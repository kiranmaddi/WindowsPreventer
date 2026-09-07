[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Runtime = 'win-x64',

    [string] $Configuration = 'Release',

    [string] $Version = '0.1.7'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path -Path $PSScriptRoot -ChildPath '..')
$publishScript = Join-Path -Path $repoRoot -ChildPath 'scripts\publish-desktop.ps1'
$publishPath = Join-Path -Path $repoRoot -ChildPath "artifacts\publish\desktop-$Runtime"
$deliverablesRoot = Join-Path -Path $repoRoot -ChildPath 'Deliverables'
$packageName = "WindowsPreventer-Test-$Version-$Runtime"
$packageFolder = Join-Path -Path $deliverablesRoot -ChildPath $packageName
$zipPath = Join-Path -Path $deliverablesRoot -ChildPath "$packageName.zip"

& $publishScript -Runtime $Runtime -Configuration $Configuration

if (Test-Path -Path $packageFolder) {
    Remove-Item -Path $packageFolder -Recurse -Force
}

if (Test-Path -Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

New-Item -Path $packageFolder -ItemType Directory -Force | Out-Null
Copy-Item -Path (Join-Path -Path $publishPath -ChildPath '*') -Destination $packageFolder -Recurse -Force

$readmePath = Join-Path -Path $packageFolder -ChildPath 'TESTING_README.md'
$readme = @"
# Windows Preventer Test Package

This is a portable test package for Windows Preventer.

## Contents

- `WindowsPreventer.App.exe` - desktop application
- `config\policies.sample.json` - sample policy settings
- `config\license.sample.json` - sample development license

## Safe First Test

1. Extract the zip file.
2. Double-click `WindowsPreventer.App.exe`.
3. Confirm the app shows the sample license as valid.
4. Click `Preview Changes` first. Preview does not change Windows.

## Applying Real Changes

Only test real Apply/Remove on a VM or spare machine.

1. Create or use a standard Windows user account.
2. Sign into that standard account once so its profile is loaded.
3. Sign back into an administrator account.
4. Run `WindowsPreventer.App.exe` as administrator.
5. Click `Preview Changes`.
6. Click `Apply` only after confirming the target user and controls.
7. Sign out and sign in as the standard user to check restrictions.
8. Run `Remove` from the app to roll back the restrictions.

## Current Restrictions

- Disable Explorer right-click context menus.
- Hide restart and shutdown commands.
- Disable clipboard history and cloud clipboard sync.

Full universal copy/paste blocking is not included in this test package.

## License Behavior

- Preview works even if the license is missing.
- Apply requires a valid license.
- Remove remains available so restrictions can be rolled back.

"@

Set-Content -Path $readmePath -Value $readme -Encoding UTF8
Compress-Archive -Path (Join-Path -Path $packageFolder -ChildPath '*') -DestinationPath $zipPath -Force

Write-Host "Created test package folder: $packageFolder"
Write-Host "Created test package zip: $zipPath"