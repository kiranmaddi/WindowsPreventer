# Windows Preventer Development Plan

This document is the working roadmap for future development.

It should be updated as features are implemented, changed, or removed.

## Product Direction

Windows Preventer should be developed as a proper .NET Windows desktop application.

Primary deliverable:

```text
WindowsPreventer.App.exe
```

Supporting deliverables:

```text
windows-preventer.exe     Command-line tool for automation
PowerShell scripts        Prototype/reference/admin helpers
```

## Current Architecture

```text
WindowsPreventer.App
  Desktop administrator UI

WindowsPreventer.Core
  Shared policy and Windows integration logic

WindowsPreventer.Cli
  Command-line wrapper around Core

WindowsPreventer.Licensing
  Shared offline license validation

PowerShell prototype
  Reference implementation and helper scripts
```

## Development Principles

- Keep policy logic in `WindowsPreventer.Core`.
- Keep UI behavior in `WindowsPreventer.App`.
- Keep command-line parsing in `WindowsPreventer.Cli`.
- Keep license validation in `WindowsPreventer.Licensing`.
- Do not duplicate registry or account logic in multiple apps.
- Prefer documented Windows policy surfaces.
- Always support preview/dry-run before applying changes.
- Always keep remove/rollback paths available.
- Do not hide the app or bypass Windows security prompts.
- Do not apply real restrictions without administrator elevation.

## Phase 1: Desktop Foundation

Status: started.

Goals:

- Create WPF desktop app.
- Reference shared Core project.
- Load policy JSON.
- Edit target users and controls.
- Preview changes.
- Apply changes with elevation.
- Remove changes.
- Show run output inside the UI.

Implemented so far:

- WPF app scaffold.
- Basic admin UI.
- Policy config picker.
- Target users textbox.
- Detected Windows users grid.
- Service/generated profile filtering.
- Restriction checkboxes.
- Preview/apply/remove buttons.
- Run as Administrator button.
- Apply/Remove confirmation dialog.
- Output panel.
- Shared Core integration.
- Desktop `.exe` publish script.

Next improvements:

- Add safer status messages for success, warnings, and errors.
- Add app icon and product metadata.
- Add better validation for target user names.
- Add a user/profile discovery screen.

## Phase 2: Licensing

Status: started.

Goal:

Control app usage based on a license.

Recommended first approach:

- Use an offline `license.json` file.
- Validate required fields.
- Validate expiration date.
- Show license status in the desktop app.
- Allow preview without a valid license.
- Require a valid license for Apply.
- Allow Remove even without a valid license, so restrictions can always be rolled back.

Implemented so far:

- Added `config\license.sample.json`.
- Added `WindowsPreventer.Licensing` project.
- Added `LicenseInfo`, `LicenseStatus`, `LicenseValidationResult`, and `LicenseService`.
- Desktop app can select a license file.
- Desktop app shows license status.
- Desktop app disables Apply when the license is missing, invalid, or expired.
- CLI accepts `--license` / `-l`.
- CLI blocks Apply when the license is missing, invalid, or expired.
- Preview and Remove remain available without a valid license.

Suggested first license file:

```json
{
  "customerName": "Development Customer",
  "licenseId": "WP-DEV-0001",
  "edition": "Professional",
  "expiresOn": "2027-09-05",
  "maxDevices": 5,
  "features": [
    "DisableRightClick",
    "HideRestartAndShutdown",
    "DisableClipboardHistory"
  ]
}
```

Suggested licensing project:

```text
src\WindowsPreventer.Licensing\
  LicenseInfo.cs
  LicenseStatus.cs
  LicenseValidationResult.cs
  LicenseService.cs
```

Validation behavior:

| License state | Preview | Apply   | Remove  |
| ------------- | ------- | ------- | ------- |
| Missing       | Allowed | Blocked | Allowed |
| Invalid       | Allowed | Blocked | Allowed |
| Expired       | Allowed | Blocked | Allowed |
| Valid         | Allowed | Allowed | Allowed |

Later licensing hardening:

- Add RSA or ECDSA signature verification.
- Keep private signing key outside the app.
- Embed only the public verification key inside the app.
- Add license generator tool.
- Add edition-based feature gates.
- Optionally add device binding.

Next licensing improvements:

- Add unit tests for missing, invalid, expired, and valid licenses.
- Add feature gates so a license can allow only selected controls.
- Add signed license verification.
- Add a license generator tool for development/testing.
- Add a clearer license details screen in the desktop app.

Important rule:

Remove should remain available even if the license is expired or invalid.

## Phase 3: Provisioning Model

Status: planned.

Goal:

Allow system administrators to grant or revoke provisions based on user requirements.

Examples:

- User A cannot right-click.
- User B can right-click but cannot access restart/shutdown.
- User C gets temporary access until a specific date.
- Training-room users get one shared policy profile.

Suggested model:

```text
ProvisionProfile
  Name
  Controls
  AssignedUsers
  AssignedGroups
  StartsOn
  ExpiresOn
```

Example profiles:

- `StandardLockedDown`
- `TrainingRoom`
- `TemporaryAccess`
- `ClipboardRestricted`

Needed features:

- Profile create/edit/delete UI.
- Assign users to profiles.
- Expiration dates.
- Audit log entries.
- Export/import profiles.

## Phase 4: Audit And Reporting

Status: planned.

Goals:

- Track who applied a policy.
- Track when a policy was applied.
- Track which machine was changed.
- Track which users were targeted.
- Track apply/remove results.

Suggested audit fields:

```text
Timestamp
MachineName
WindowsUser
Action
TargetUsers
Controls
Result
ErrorMessage
```

Storage options:

- Local JSON log file for early version.
- Windows Event Log for production version.
- Central server/database later if remote management is added.

## Phase 5: Windows Service

Status: planned.

Goal:

Add background enforcement so policies can be reconciled automatically.

Why a service may be needed:

- Apply policies when target users sign in.
- Reapply policies if registry values drift.
- Run with controlled LocalSystem privileges.
- Support future remote management.

Suggested project:

```text
src\WindowsPreventer.Service\
```

The desktop app should manage configuration.

The service should enforce configuration.

## Phase 6: Advanced Controls

Status: planned.

Possible additions:

- Assigned Access / kiosk mode.
- AppLocker rules.
- Windows Defender Application Control rules.
- RDP clipboard redirection restrictions.
- Application-specific clipboard restrictions.

Important note:

Full universal copy/paste blocking should not be implemented with fragile keyboard blocking as the primary solution. Use Windows-supported control surfaces where possible.

## Build And Publish Commands

Build all projects:

```powershell
dotnet build .\WindowsPreventer.sln
```

Run desktop app during development:

```powershell
dotnet run --project .\src\WindowsPreventer.App
```

Publish desktop app:

```powershell
.\scripts\publish-desktop.ps1
```

Publish CLI app:

```powershell
.\scripts\publish-exe.ps1
```

Desktop executable output:

```text
artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe
```

CLI executable output:

```text
artifacts\publish\win-x64\windows-preventer.exe
```

## Testing Plan

Use a VM or spare test machine first.

Recommended test accounts:

```text
Local administrator account
Standard user account
```

Basic tests:

1. Run desktop app without elevation.
2. Confirm preview works.
3. Confirm apply is blocked or clearly asks for elevation.
4. Run desktop app as administrator.
5. Apply policy to standard user.
6. Confirm administrator remains unrestricted.
7. Sign in as standard user and confirm expected restrictions.
8. Remove policy.
9. Confirm standard user returns to normal behavior.

Licensing tests to add later:

1. Missing license blocks Apply.
2. Expired license blocks Apply.
3. Invalid license blocks Apply.
4. Valid license allows Apply.
5. Remove works even without valid license.

## Immediate Next Step

Continue Phase 2 licensing hardening:

1. Add tests for `WindowsPreventer.Licensing`.
2. Add edition and feature gating checks.
3. Add signed license verification.
4. Add a development license generator tool.
5. Build and publish again.
