$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ProjectFile = Join-Path $ProjectRoot "Kafo.Web.csproj"

Set-Location $ProjectRoot

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Command
    )

    Write-Host "`n=== $Name ===" -ForegroundColor Cyan

    $global:LASTEXITCODE = 0
    & $Command

    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $ProjectFile)) {
    throw "Project file was not found: $ProjectFile"
}

Write-Host "Project directory: $ProjectRoot" -ForegroundColor DarkGray
Write-Host "Mode: Existing merged project" -ForegroundColor DarkGray

# ============================================================
# فحص أمني للكود فقط
# يتجاهل قواعد البيانات، ملفات المستخدمين، bin وobj والنسخ القديمة.
# ============================================================

Write-Host "`n=== Static source-code security scan ===" -ForegroundColor Cyan

$ExcludedPattern = "\\(bin|obj|Migrations|wwwroot\\uploads|wwwroot\\lib|logs|secure-storage)\\"

$SourceFiles = Get-ChildItem -Path $ProjectRoot -Recurse -File |
    Where-Object {
        $_.Extension -in @(
            ".cs",
            ".cshtml",
            ".js",
            ".json",
            ".csproj"
        ) -and
        $_.FullName -notmatch $ExcludedPattern -and
        $_.Name -notmatch "\.(bak|backup)(\.|_|$)"
    }

$SecurityErrors = [System.Collections.Generic.List[string]]::new()
$SecurityWarnings = [System.Collections.Generic.List[string]]::new()

foreach ($File in $SourceFiles) {
    $RelativePath = $File.FullName.Substring($ProjectRoot.Length).
        TrimStart("\", "/")

    $Content = Get-Content $File.FullName -Raw -ErrorAction SilentlyContinue

    if ([string]::IsNullOrWhiteSpace($Content)) {
        continue
    }

    if ($Content -match "Admin@12345") {
        $SecurityErrors.Add(
            "Hard-coded default administrator password: $RelativePath"
        )
    }

    if ($Content -match "@Html\.Raw\s*\(") {
        $SecurityErrors.Add(
            "Unsafe Html.Raw usage: $RelativePath"
        )
    }

    if ($Content -match "\.innerHTML\s*=") {
        $SecurityErrors.Add(
            "Unsafe innerHTML assignment: $RelativePath"
        )
    }

    if (
        $File.Name -like "appsettings*.json" -and
        $Content -match '"AllowedHosts"\s*:\s*"\*"'
    ) {
        $SecurityWarnings.Add(
            "AllowedHosts uses wildcard: $RelativePath"
        )
    }

    if (
        $File.Extension -eq ".cs" -and
        $Content -match 'wwwroot[\\/]+uploads[\\/]+(job-applications-cv|donor-reports|contact-attachments)'
    ) {
        $SecurityErrors.Add(
            "Private files referenced under wwwroot/uploads: $RelativePath"
        )
    }

    if (
        $Content -match "(?i)(password|secret|api[_-]?key)\s*[:=]\s*`"[^`"]{8,}`""
    ) {
        if (
            $Content -notmatch "GetEnvironmentVariable" -and
            $Content -notmatch "Configuration\[" -and
            $Content -notmatch "PasswordHash"
        ) {
            $SecurityWarnings.Add(
                "Possible hard-coded secret requires review: $RelativePath"
            )
        }
    }
}

if ($SecurityWarnings.Count -gt 0) {
    Write-Host "`nSecurity warnings:" -ForegroundColor Yellow

    foreach ($Warning in $SecurityWarnings) {
        Write-Host " - $Warning" -ForegroundColor Yellow
    }
}

if ($SecurityErrors.Count -gt 0) {
    Write-Host "`nSecurity errors:" -ForegroundColor Red

    foreach ($SecurityError in $SecurityErrors) {
        Write-Host " - $SecurityError" -ForegroundColor Red
    }

    throw "Static source-code security scan failed."
}

Write-Host "Static source-code security scan passed." `
    -ForegroundColor Green

# ============================================================
# فحص .NET
# ============================================================

Invoke-CheckedCommand ".NET SDK information" {
    dotnet --info
}

Invoke-CheckedCommand "Restore packages" {
    dotnet restore $ProjectFile --force-evaluate
}

Invoke-CheckedCommand "Release build" {
    dotnet build $ProjectFile `
        --configuration Release `
        --no-restore
}

Invoke-CheckedCommand "Vulnerable package scan" {
    dotnet list $ProjectFile package `
        --vulnerable `
        --include-transitive
}

Invoke-CheckedCommand "Deprecated package scan" {
    dotnet list $ProjectFile package `
        --deprecated `
        --include-transitive
}

Write-Host "`nSecurity and build checks completed successfully." `
    -ForegroundColor Green