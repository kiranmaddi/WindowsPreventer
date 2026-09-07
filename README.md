# Windows Preventer

Windows Preventer is a Windows administration tool for applying documented policy restrictions to standard user profiles while leaving local administrators exempt.

The primary deliverable is a Windows desktop `.exe` built with C#/.NET WPF. The command-line `.exe` remains available for automation, and the PowerShell implementation remains as prototype/reference tooling.

## Current Scope

- Apply or remove user policy registry values for non-admin profiles.
- Support dry-run previews before changing the machine.
- Load and validate an offline license file.
- Block Apply when the license is missing, invalid, or expired.
- Keep Preview and Remove available without a valid license.
- Keep administrators exempt by checking local Administrators group membership.
- Show detected Windows users on startup.
- Filter service/generated profiles from the selectable user list.
- Apply user-specific restrictions only to selected target users; empty `targetUsers` means no user-specific target.
- Start with safe documented controls:
  - Disable Explorer context menus.
  - Hide shutdown/restart commands from Explorer and Start menu.
  - Disable clipboard history and cloud clipboard sync.

Basic local copy/paste across all applications is not implemented in this first slice because Windows does not provide a clean per-user policy switch for that. That should be handled later through assigned access, kiosk mode, application-level restrictions, or a signed endpoint agent with explicit admin deployment.

## Quick Start

Run the desktop app during development:

```powershell
dotnet run --project .\src\WindowsPreventer.App
```

Build the desktop `.exe` package:

```powershell
.\scripts\publish-desktop.ps1
```

Create a sendable test package zip:

```powershell
.\scripts\package-test-deliverable.ps1
```

The test package zip will be created under:

```text
Deliverables\WindowsPreventer-Test-0.1.7-win-x64.zip
```

The published desktop executable will be created under:

```text
artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe
```

Preview the sample policy with the CLI executable without changing Windows:

```powershell
dotnet run --project .\src\WindowsPreventer.Cli -- --config .\config\policies.sample.json --license .\config\license.sample.json --what-if
```

Build the CLI `.exe` package:

```powershell
.\scripts\publish-exe.ps1
```

The published CLI executable will be created under:

```text
artifacts\publish\win-x64\windows-preventer.exe
```

Run the published executable in dry-run mode:

```powershell
.\artifacts\publish\win-x64\windows-preventer.exe --config .\config\policies.sample.json --license .\config\license.sample.json --what-if
```

The PowerShell prototype can still be run directly:

```powershell
.\scripts\windows-preventer.ps1 -ConfigPath .\config\policies.sample.json -WhatIf
```

Apply the sample policy as an elevated administrator:

```powershell
.\scripts\windows-preventer.ps1 -ConfigPath .\config\policies.sample.json
```

Apply and remove operations must run from an elevated PowerShell session. Dry runs do not require elevation.

Remove the sample policy values:

```powershell
.\scripts\windows-preventer.ps1 -ConfigPath .\config\policies.sample.json -Mode Remove
```

## Project Layout

```text
config/                  Sample policy configuration
docs/                    Architecture and rollout notes
scripts/                 Publish scripts and PowerShell prototype entry point
src/WindowsPreventer.App/     WPF desktop application
src/WindowsPreventer.Cli/     C# command-line executable
src/WindowsPreventer.Core/    Shared C# policy engine
src/WindowsPreventer.Licensing/ Shared offline license validation
src/WindowsPreventer/         PowerShell prototype module
```

## Targeting Model

The desktop app shows detected Windows users and lets an admin copy either selected standard users or all standard users into the selected target list.

Empty `targetUsers` means no user-specific restrictions are selected. Machine-wide controls, such as clipboard history policy, can still preview/apply at machine scope.

Real Apply and Remove actions require administrator elevation. The desktop app provides a Run as Administrator button and asks for confirmation before making real changes.

## Safety Model

This tool is intended for authorized administration of owned or managed Windows machines. It does not hide itself, bypass permissions, intercept credentials, or persist without administrator consent.
