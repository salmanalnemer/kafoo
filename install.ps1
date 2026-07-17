param(
    [string]$ProjectRoot = "D:\KafoDotNet\Kafo.Web"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$PatchRoot = $PSScriptRoot
$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$ProjectFile = Join-Path $ProjectRoot "Kafo.Web.csproj"

if (-not (Test-Path $ProjectFile)) {
    throw "لم يتم العثور على Kafo.Web.csproj داخل: $ProjectRoot"
}

$backupRoot = Join-Path $ProjectRoot ("security-backup-progressive-login-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

$files = @(
    "Program.cs",
    "Areas\Admin\Controllers\AdminAuthController.cs",
    "Areas\Portal\Controllers\PortalAuthController.cs",
    "Controllers\AccountSecurityController.cs",
    "Security\LoginSecurity.cs",
    "Configuration\SecurityOptions.cs",
    "Services\Implementations\LoginOtpService.cs",
    "wwwroot\css\rate-limit.css",
    "wwwroot\js\rate-limit-page.js"
)

foreach ($relativePath in $files) {
    $source = Join-Path $PatchRoot $relativePath
    $destination = Join-Path $ProjectRoot $relativePath

    if (-not (Test-Path $source)) {
        throw "ملف التحديث غير موجود: $source"
    }

    if (Test-Path $destination) {
        $backupDestination = Join-Path $backupRoot $relativePath
        New-Item -ItemType Directory -Path (Split-Path $backupDestination -Parent) -Force | Out-Null
        Copy-Item $destination $backupDestination -Force
    }

    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item $source $destination -Force
    Write-Host "تم تحديث: $relativePath" -ForegroundColor Green
}

Write-Host ""
Write-Host "تم حفظ نسخة احتياطية من الملفات المستبدلة في:" -ForegroundColor Cyan
Write-Host $backupRoot -ForegroundColor Yellow
Write-Host ""
Write-Host "لم يتم تعديل قاعدة البيانات أو appsettings أو ملفات المستخدمين." -ForegroundColor Cyan
Write-Host "نفّذ الآن: dotnet clean ثم dotnet build" -ForegroundColor Cyan
