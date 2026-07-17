#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

python3 scripts/security-static-check.py

dotnet --info
dotnet restore
dotnet build Kafo.Web.csproj -c Release --no-restore

dotnet list Kafo.Web.csproj package --vulnerable --include-transitive
dotnet list Kafo.Web.csproj package --deprecated --include-transitive
dotnet list Kafo.Web.csproj package --outdated --include-transitive

echo "Security/build checks completed successfully."
