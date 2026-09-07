# Windows Preventer Test Package

This is a portable test package for Windows Preventer.

## Contents

- WindowsPreventer.App.exe - desktop application
- config\policies.sample.json - sample policy settings
- config\license.sample.json - sample development license

## Safe First Test

1. Extract the zip file.
2. Double-click WindowsPreventer.App.exe.
3. Confirm the app shows the sample license as valid.
4. Click Preview Changes first. Preview does not change Windows.

## Applying Real Changes

Only test real Apply/Remove on a VM or spare machine.

1. Create or use a standard Windows user account.
2. Sign into that standard account once so its profile is loaded.
3. Sign back into an administrator account.
4. Run WindowsPreventer.App.exe as administrator.
5. Click Preview Changes.
6. Click Apply only after confirming the target user and controls.
7. Sign out and sign in as the standard user to check restrictions.
8. Run Remove from the app to roll back the restrictions.

## Current Restrictions

- Disable Explorer right-click context menus.
- Hide restart and shutdown commands.
- Disable clipboard history and cloud clipboard sync.

Full universal copy/paste blocking is not included in this test package.

## License Behavior

- Preview works even if the license is missing.
- Apply requires a valid license.
- Remove remains available so restrictions can be rolled back.

