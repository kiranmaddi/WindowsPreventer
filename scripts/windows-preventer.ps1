[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string] $ConfigPath,

    [ValidateSet('Apply', 'Remove')]
    [string] $Mode = 'Apply'
)

$modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\src\WindowsPreventer\WindowsPreventer.psd1'
Import-Module $modulePath -Force

Invoke-WindowsPreventerPolicy -ConfigPath $ConfigPath -Mode $Mode -WhatIf:$WhatIfPreference