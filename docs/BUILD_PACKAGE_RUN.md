# Build, Package, And Run Reference

This file is a quick reference for building, packaging, and running Windows Preventer.

Run these commands from the project root:

```text
C:\DreamProjects\WindowsPreventer
```

## One-Time Requirement

Install the .NET 8 SDK on the development machine.

Check whether .NET is available:

```powershell
dotnet --info
```

If the current terminal does not recognize `dotnet` after installation, restart PowerShell or temporarily run:

```powershell
$env:PATH = 'C:\Program Files\dotnet;' + $env:PATH
```

## Build All Projects

Use this to compile the desktop app, CLI app, core library, and licensing library:

```powershell
dotnet build .\WindowsPreventer.sln
```

Expected result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Run Desktop App During Development

Use this when developing or testing from source:

```powershell
dotnet run --project .\src\WindowsPreventer.App
```

This opens the WPF desktop app.

In the app:

- `Preview Changes` does not change Windows.
- `Apply` requires a valid license and administrator elevation.
- `Remove` is kept available for rollback, but real registry changes still require administrator elevation.

## Publish Desktop EXE

Use this to create the desktop executable package:

```powershell
.\scripts\publish-desktop.ps1
```

Output folder:

```text
artifacts\publish\desktop-win-x64
```

Main executable:

```text
artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe
```

## Run Published Desktop EXE

Run the published app:

```powershell
.\artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe
```

For real Apply or Remove testing, run it as administrator.

For safe first testing, open the app normally and use only `Preview Changes`.

## Create Sendable Test Deliverable

Use this to create the zip file you can send to another user or tester:

```powershell
.\scripts\package-test-deliverable.ps1
```

Deliverable folder:

```text
Deliverables\WindowsPreventer-Test-0.1.7-win-x64
```

Deliverable zip:

```text
Deliverables\WindowsPreventer-Test-0.1.7-win-x64.zip
```

This zip contains:

- `WindowsPreventer.App.exe`
- `config\policies.sample.json`
- `config\license.sample.json`
- `TESTING_README.md`

## How A Tester Should Use The Zip

1. Extract `Deliverables\WindowsPreventer-Test-0.1.7-win-x64.zip`.
2. Open the extracted folder.
3. Double-click `WindowsPreventer.App.exe`.
4. Confirm the app loads the sample license.
5. Click `Preview Changes` first.
6. Only test `Apply` on a VM or spare machine.
7. Run the app as administrator before testing real Apply or Remove.

## Publish CLI EXE

The CLI is useful for automation and command-line testing.

Publish it with:

```powershell
.\scripts\publish-exe.ps1
```

CLI output folder:

```text
artifacts\publish\win-x64
```

CLI executable:

```text
artifacts\publish\win-x64\windows-preventer.exe
```

## Run CLI In Preview Mode

Preview does not change Windows:

```powershell
.\artifacts\publish\win-x64\windows-preventer.exe --config .\config\policies.sample.json --license .\config\license.sample.json --what-if
```

## Run CLI Apply

Apply requires:

- Valid license file.
- Elevated administrator terminal.
- Correct target user configuration.

Command:

```powershell
.\artifacts\publish\win-x64\windows-preventer.exe --config .\config\policies.sample.json --license .\config\license.sample.json --mode Apply
```

## Run CLI Remove

Remove is used to roll back policy values.

Command:

```powershell
.\artifacts\publish\win-x64\windows-preventer.exe --config .\config\policies.sample.json --mode Remove
```

Real Remove still requires administrator elevation because it changes registry policy values.

## Common Workflow

For normal development:

```powershell
dotnet build .\WindowsPreventer.sln
dotnet run --project .\src\WindowsPreventer.App
```

For creating a tester package:

```powershell
.\scripts\package-test-deliverable.ps1
```

For sending to a tester:

```text
Send this file:
Deliverables\WindowsPreventer-Test-0.1.7-win-x64.zip
```

## Safety Notes

- Start with `Preview Changes`.
- Use a VM or spare test machine for real Apply testing.
- Do not test restrictions on your main admin account.
- Keep administrator exemption enabled while testing.
- Use Remove to roll back applied restrictions.
