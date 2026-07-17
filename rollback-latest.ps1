param(
    [string]$ProjectRoot = "D:\KafoDotNet\Kafo.Web"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$backup = Get-ChildItem -Path $ProjectRoot -Directory -Filter "security-backup-progressive-login-*" |
    Sort-Object Name -Descending |
    Select-Object -First 1

if ($null -eq $backup) {
    throw "لم يتم العثور على نسخة احتياطية للتحديث داخل: $ProjectRoot"
}

Get-ChildItem -Path $backup.FullName -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring($backup.FullName.Length).TrimStart('\', '/')
    $destination = Join-Path $ProjectRoot $relativePath
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item $_.FullName $destination -Force
    Write-Host "تمت الاستعادة: $relativePath" -ForegroundColor Green
}

Write-Host "تمت استعادة آخر نسخة احتياطية من: $($backup.FullName)" -ForegroundColor Cyan
