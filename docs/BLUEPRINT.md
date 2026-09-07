# Windows Preventer Blueprint

## Goal

Create an administrator-controlled Windows restriction manager that can apply selected controls to standard user profiles while exempting system administrators. Administrators should later be able to grant or revoke provisions per user or group.

## Principles

- Use documented Windows policy surfaces first: Group Policy-compatible registry keys, Assigned Access, AppLocker or WDAC, and MDM CSPs where applicable.
- Avoid hidden hooks or stealth behavior. Every restriction should be auditable and reversible.
- Keep the administrator exempt by default.
- Support dry-run, apply, remove, and report workflows.

## Phases

### Phase 1: Local Desktop EXE

- C#/.NET WPF desktop application.
- C#/.NET command-line executable.
- PowerShell module retained as prototype/reference tooling.
- JSON policy file.
- Dry-run mode.
- Apply and remove documented registry policies.
- Administrator exemption based on local Administrators group membership.
- Detected Windows users are visible in the desktop app.
- Empty target list means no selected user-specific target.
- Admins can explicitly choose selected standard users or all standard users.

### Phase 2: Provisioning Model

- User and group targeting.
- Named profiles such as `StandardLockedDown`, `TrainingRoom`, and `TemporaryAccess`.
- Time-bound provisions with expiration.
- Audit log for policy changes.

### Phase 3: Service and Admin UI

- Signed Windows service running as LocalSystem.
- Desktop or web-based admin console.
- Background reconciliation so local policy drift is corrected.
- Optional remote management API protected by Windows authentication.

### Phase 4: Advanced Controls

- Assigned Access or kiosk mode for controlled desktops.
- AppLocker or Windows Defender Application Control for application allow lists.
- RDP clipboard redirection restrictions for remote sessions.
- Application-specific copy/paste restrictions where the managed app supports them.

## Control Mapping

| Control                 | Phase 1 approach                            | Notes                                                                                                     |
| ----------------------- | ------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Right click             | `NoViewContextMenu` under Explorer policies | Affects Explorer context menus. Applications may have their own context menus.                            |
| Restart and shutdown UI | `NoClose` under Explorer policies           | Hides shutdown/restart commands. Does not stop physical power actions or privileged shutdown APIs.        |
| Clipboard history       | Windows System policy values                | Disables clipboard history and cross-device clipboard sync, not basic local copy/paste.                   |
| Copy/paste              | Planned                                     | Requires kiosk/app-level control or an endpoint agent. No universal clean per-user Windows policy exists. |

## Initial Data Model

```json
{
  "targetUsers": ["DOMAIN\\user1", "localuser"],
  "exemptAdministrators": true,
  "controls": {
    "disableRightClick": true,
    "hideRestartAndShutdown": true,
    "disableClipboardHistory": true,
    "disableCopyPaste": false
  }
}
```

## Operational Flow

1. Administrator edits a JSON policy file.
2. Administrator previews changes in the desktop app or runs the CLI with `--what-if`.
3. Shared core resolves target accounts to SIDs.
4. Shared core skips users who are local administrators when exemption is enabled.
5. Shared core writes user-scoped policy values only for selected target users.
6. Administrator signs out or restarts Explorer for affected users where required.

## EXE Build Flow

1. Install the .NET 8 SDK on the build machine.
2. Run `dotnet restore` from the repository root.
3. Run `dotnet build .\WindowsPreventer.sln`.
4. Run `.\scripts\publish-desktop.ps1` to create the self-contained desktop executable.
5. Optionally run `.\scripts\publish-exe.ps1` to create the command-line executable.
6. Copy `artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe` and the policy JSON file to the test machine.
7. Run the desktop executable as administrator for real apply/remove operations.

## Known Limitations

- Phase 1 can write per-user policy values only when the user hive is loaded under `HKEY_USERS`.
- Some policies require sign-out, Explorer restart, or reboot before the user experience changes.
- Full copy/paste blocking is deliberately excluded from the first implementation because keyboard interception would be brittle and intrusive.
