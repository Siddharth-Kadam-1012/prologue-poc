#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
  Validates the spec.md for a feature.
  Delegates to scripts/sdlc/sdlc.cs.

.PARAMETER SpecDir          Feature spec directory.
.PARAMETER StrictQuestions  Fail if any [QUESTION] items remain unresolved.
#>
[CmdletBinding()]
param([string]$SpecDir, [switch]$StrictQuestions)

if (-not $SpecDir -and (Test-Path '.specify/feature.json')) {
    $SpecDir = (Get-Content '.specify/feature.json' | ConvertFrom-Json).feature_directory
}
if (-not $SpecDir) { Write-Error "Pass -SpecDir."; exit 2 }

$extra = if ($StrictQuestions) { @("--strict-questions") } else { @() }
dotnet run (Join-Path $PSScriptRoot "sdlc/sdlc.cs") -- spec "$SpecDir" @extra
exit $LASTEXITCODE
