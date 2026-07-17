#!/usr/bin/env python3
"""Dependency-free security regression checks for the Kafo source tree."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ERRORS: list[str] = []


def error(message: str) -> None:
    ERRORS.append(message)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


# Files that must never ship or be committed.
for path in ROOT.rglob("*"):
    if not path.is_file():
        continue
    rp = rel(path)
    lower = path.name.lower()
    if rp.startswith(("bin/", "obj/", ".git/")):
        error(f"Build/repository artifact present: {rp}")
    if lower.endswith((".db", ".sqlite", ".sqlite3", ".pfx", ".p12", ".key", ".pem")):
        error(f"Sensitive runtime file present: {rp}")
    if ".backup" in lower or ".bak" in lower or "backup_before_" in lower:
        error(f"Backup file present: {rp}")
    if rp.startswith("App_Data/") and rp != "App_Data/.gitkeep":
        error(f"Runtime App_Data content present: {rp}")
    if rp.startswith("wwwroot/uploads/") and rp != "wwwroot/uploads/.gitkeep":
        error(f"Uploaded deployment content present: {rp}")

# Parse configuration/project files.
for path in [*ROOT.glob("appsettings*.json"), ROOT / "global.json", ROOT / "Properties" / "launchSettings.json"]:
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except Exception as exc:
        error(f"Invalid JSON {rel(path)}: {exc}")
        continue
    if path.name.startswith("appsettings"):
        password = (((data.get("Email") or {}).get("Smtp") or {}).get("Password"))
        if isinstance(password, str) and password.strip():
            error(f"SMTP password must not be stored in {rel(path)}")
        if data.get("AllowedHosts") == "*":
            error(f"AllowedHosts wildcard found in {rel(path)}")

for path in [*ROOT.rglob("*.csproj"), *ROOT.rglob("*.props"), *ROOT.rglob("*.targets")]:
    try:
        ET.parse(path)
    except Exception as exc:
        error(f"Invalid XML {rel(path)}: {exc}")

source_extensions = {".cs", ".cshtml", ".js", ".json"}
patterns = {
    r"Admin@12345": "Default administrator password",
    r"@Html\.Raw\s*\(": "Uncontrolled Html.Raw output",
    r"\binnerHTML\s*=": "innerHTML assignment",
    r"\bon(?:click|change|submit|input|load|error|keydown|keyup)\s*=": "Inline JavaScript event handler",
    r"\bRequest\.Host\b": "Host-header-derived absolute URL",
    r"CookieSecurePolicy\.None": "Insecure cookie policy",
    r"SameSiteMode\.None": "SameSite=None cookie",
    r"SendPortalAccountPasswordAsync": "Plaintext portal-password email flow",
    r"TemporaryPasswordGenerator": "Temporary plaintext password generator",
}

for path in ROOT.rglob("*"):
    if not path.is_file() or path.suffix.lower() not in source_extensions:
        continue
    rp = rel(path)
    if rp.startswith(("wwwroot/lib/", "bin/", "obj/")) or path.name.endswith((".min.js", ".min.css")):
        continue
    try:
        text = path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        continue
    for pattern, label in patterns.items():
        if re.search(pattern, text, re.IGNORECASE):
            error(f"{label}: {rp}")

# Every target=_blank anchor must be protected.
anchor_pattern = re.compile(r"<a\b[^>]*\btarget=[\"']_blank[\"'][^>]*>", re.I | re.S)
for path in [*ROOT.joinpath("Areas").rglob("*.cshtml"), *ROOT.joinpath("Views").rglob("*.cshtml")]:
    text = path.read_text(encoding="utf-8-sig")
    for tag in anchor_pattern.findall(text):
        if not re.search(r"\brel=[\"'][^\"']*\bnoopener\b", tag, re.I):
            error(f"target=_blank without rel=noopener: {rel(path)}")


# Catch gross C# syntax corruption without requiring the .NET SDK.
def check_csharp_delimiters(path: Path) -> None:
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    stack: list[tuple[str, int]] = []
    pairs = {")": "(", "]": "[", "}": "{"}
    state = "code"
    verbatim = False
    raw_quotes = 0
    line = 1
    index = 0

    while index < len(text):
        char = text[index]
        following = text[index + 1] if index + 1 < len(text) else ""
        if char == "\n":
            line += 1

        if state == "line-comment":
            if char == "\n":
                state = "code"
            index += 1
            continue
        if state == "block-comment":
            if char == "*" and following == "/":
                state = "code"
                index += 2
            else:
                index += 1
            continue
        if state == "string":
            if verbatim:
                if char == '"' and following == '"':
                    index += 2
                    continue
                if char == '"':
                    state = "code"
                    verbatim = False
            else:
                if char == "\\":
                    index += 2
                    continue
                if char == '"':
                    state = "code"
            index += 1
            continue
        if state == "character":
            if char == "\\":
                index += 2
                continue
            if char == "'":
                state = "code"
            index += 1
            continue
        if state == "raw-string":
            if text.startswith('"' * raw_quotes, index):
                state = "code"
                index += raw_quotes
                raw_quotes = 0
            else:
                index += 1
            continue

        if char == "/" and following == "/":
            state = "line-comment"
            index += 2
            continue
        if char == "/" and following == "*":
            state = "block-comment"
            index += 2
            continue
        if char == "@" and following == '"':
            state = "string"
            verbatim = True
            index += 2
            continue
        if char == "$" and following == "@" and index + 2 < len(text) and text[index + 2] == '"':
            state = "string"
            verbatim = True
            index += 3
            continue
        if char == "@" and following == "$" and index + 2 < len(text) and text[index + 2] == '"':
            state = "string"
            verbatim = True
            index += 3
            continue
        if char == "$" and following == '"':
            state = "string"
            index += 2
            continue
        if char == '"':
            quote_count = 1
            while index + quote_count < len(text) and text[index + quote_count] == '"':
                quote_count += 1
            if quote_count >= 3:
                state = "raw-string"
                raw_quotes = quote_count
                index += quote_count
                continue
            state = "string"
            index += 1
            continue
        if char == "'":
            state = "character"
            index += 1
            continue

        if char in "([{":
            stack.append((char, line))
        elif char in ")]}":
            if not stack or stack[-1][0] != pairs[char]:
                error(f"Unmatched {char} in {rel(path)}:{line}")
                return
            stack.pop()
        index += 1

    if state in {"block-comment", "string", "character", "raw-string"}:
        error(f"Unterminated {state} in {rel(path)}")
    if stack:
        delimiter, delimiter_line = stack[-1]
        error(f"Unmatched {delimiter} in {rel(path)}:{delimiter_line}")


for path in ROOT.rglob("*.cs"):
    check_csharp_delimiters(path)

# Required hardening controls must remain wired.
required_fragments = {
    "Program.cs": [
        "UseHttpsRedirection()",
        "UseHsts()",
        "AddRateLimiter",
        "AddAntiforgery",
        "PersistKeysToFileSystem",
        "KafoCookieAuthenticationEvents",
    ],
    "Middleware/SecurityHeadersMiddleware.cs": [
        "Content-Security-Policy",
        "script-src-attr 'none'",
        "X-Content-Type-Options",
        "Referrer-Policy",
    ],
    "Services/Implementations/FileUploadService.cs": [
        "ValidateSignatureAsync",
        "_malwareScanner.ScanAsync",
        "/secure-files/",
    ],
    "Services/Implementations/SmtpEmailSender.cs": ["Uri.UriSchemeHttps"],
}
for filename, fragments in required_fragments.items():
    text = (ROOT / filename).read_text(encoding="utf-8-sig")
    for fragment in fragments:
        if fragment not in text:
            error(f"Required security control missing from {filename}: {fragment}")

if ERRORS:
    print("SECURITY STATIC CHECK: FAILED", file=sys.stderr)
    for item in ERRORS:
        print(f" - {item}", file=sys.stderr)
    raise SystemExit(1)

print("SECURITY STATIC CHECK: PASSED")
