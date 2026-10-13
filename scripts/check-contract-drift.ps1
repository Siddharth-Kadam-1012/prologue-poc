#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
  Detects breaking changes between OpenAPI contracts (exit 2 = breaking).
  Delegates to scripts/sdlc/sdlc.cs.

.PARAMETER SpecDir      Feature spec directory (looks for contracts/openapi.yaml).
.PARAMETER NewContract  Path to the new/current contract.
.PARAMETER OldContract  Path to the previously approved contract (e.g. `git show <ref>:<path> > old.yaml`).
                        Without it the tool only warns; that is NOT evidence of "no drift".
#>
[CmdletBinding()]
param([string]$SpecDir, [string]$NewContract, [string]$OldContract)

if (-not $NewContract -and $SpecDir) { $NewContract = Join-Path $SpecDir "contracts/openapi.yaml" }
if (-not $NewContract -and (Test-Path '.specify/feature.json')) {
    $d = (Get-Content '.specify/feature.json' | ConvertFrom-Json).feature_directory
    $NewContract = Join-Path $d "contracts/openapi.yaml"
}
if (-not $NewContract) { Write-Error "Pass -NewContract or -SpecDir."; exit 2 }

$extraArgs = @()
if ($OldContract) { $extraArgs += "--old=$OldContract" }

dotnet run (Join-Path $PSScriptRoot "sdlc/sdlc.cs") -- drift "$NewContract" @extraArgs
exit $LASTEXITCODE
