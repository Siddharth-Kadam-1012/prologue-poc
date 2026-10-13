#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
  Validates the traceability matrix for a feature.
  Delegates to scripts/sdlc/sdlc.cs.

.PARAMETER SpecDir  Feature spec directory.
#>
[CmdletBinding()]
param([string]$SpecDir)

if (-not $SpecDir -and (Test-Path '.specify/feature.json')) {
    $SpecDir = (Get-Content '.specify/feature.json' | ConvertFrom-Json).feature_directory
}
if (-not $SpecDir) { Write-Error "Pass -SpecDir."; exit 2 }

dotnet run (Join-Path $PSScriptRoot "sdlc/sdlc.cs") -- traceability "$SpecDir"
exit $LASTEXITCODE
