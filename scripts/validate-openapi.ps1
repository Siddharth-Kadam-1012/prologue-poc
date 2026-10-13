#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
  Validates an OpenAPI contract YAML file.
  Delegates to scripts/sdlc/sdlc.cs.

.PARAMETER ContractPath
  Path to the OpenAPI YAML file.

.PARAMETER SpecDir
  Feature spec directory — will look for contracts/openapi.yaml inside it.
#>
[CmdletBinding()]
param([string]$ContractPath, [string]$SpecDir)

if (-not $ContractPath -and $SpecDir) {
    $ContractPath = Join-Path $SpecDir "contracts/openapi.yaml"
}

if (-not $ContractPath) {
    if (Test-Path '.specify/feature.json') {
        $SpecDir = (Get-Content '.specify/feature.json' | ConvertFrom-Json).feature_directory
        $ContractPath = Join-Path $SpecDir "contracts/openapi.yaml"
    }
}

if (-not $ContractPath) { Write-Error "Pass -ContractPath or -SpecDir."; exit 2 }

dotnet run (Join-Path $PSScriptRoot "sdlc/sdlc.cs") -- openapi "$ContractPath"
exit $LASTEXITCODE
