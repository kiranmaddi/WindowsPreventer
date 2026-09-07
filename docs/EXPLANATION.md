# Windows Preventer Explanation

This document explains the project in simple terms, especially from the perspective of a Java developer who is new to .NET and Windows desktop applications.

## What We Are Building

Windows Preventer is a Windows desktop administration application.

The goal is to let a system administrator apply selected Windows restrictions to standard user profiles while keeping system administrators exempt.

Examples of restrictions:

- Disable Explorer right-click context menus.
- Hide restart and shutdown commands.
- Disable clipboard history and cloud clipboard sync.
- Later, add provision-based access so admins can allow or block features per user requirement.

The project is intended for authorized administration of owned or managed Windows machines. It should be visible, auditable, reversible, and safe to test.

## Why We Chose .NET Desktop

The final product should be an `.exe` file, so a proper .NET desktop application is the right long-term direction.

PowerShell was useful at the beginning because it let us quickly test Windows policy behavior. But PowerShell should stay as prototype/reference tooling, not the main product.

The main application is now a .NET WPF desktop app.

## Java To .NET Comparison

If you know Java, these comparisons help:

| Java world                               | .NET / C# world             |
| ---------------------------------------- | --------------------------- |
| JDK                                      | .NET SDK                    |
| JVM                                      | .NET runtime / CLR          |
| `.java` file                             | `.cs` file                  |
| Maven/Gradle project                     | `.csproj` project file      |
| IntelliJ/Eclipse workspace               | `.sln` solution file        |
| JAR/library                              | DLL/library                 |
| Runnable JAR/native launcher             | EXE                         |
| `public static void main(String[] args)` | `Program.cs` or app startup |
| JavaFX/Swing                             | WPF / WinUI / WinForms      |
| FXML                                     | XAML                        |
| Controller class                         | XAML code-behind class      |

## What .NET SDK Does

The .NET SDK is like the JDK.

It lets us:

- Compile C# code.
- Restore dependencies.
- Build the solution.
- Publish a Windows `.exe`.

Common commands:

```powershell
dotnet restore
dotnet build .\WindowsPreventer.sln
dotnet run --project .\src\WindowsPreventer.App
```

To publish the desktop app:

```powershell
.\scripts\publish-desktop.ps1
```

The published desktop executable is created at:

```text
artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe
```

## What WPF Is

WPF means Windows Presentation Foundation.

It is a Microsoft framework for building Windows desktop applications.

In this project:

- `MainWindow.xaml` defines the visual layout.
- `MainWindow.xaml.cs` contains the button-click behavior.

This is similar to JavaFX:

| JavaFX           | WPF                    |
| ---------------- | ---------------------- |
| FXML file        | XAML file              |
| Controller class | `.xaml.cs` code-behind |
| Button           | Button                 |
| TextField        | TextBox                |
| CheckBox         | CheckBox               |
| Scene/window     | Window                 |

## Current Project Structure

```text
WindowsPreventer/
  WindowsPreventer.sln
  config/
    license.sample.json
    policies.sample.json
  docs/
    BLUEPRINT.md
    EXPLANATION.md
    DEVELOPMENT_PLAN.md
  scripts/
    publish-desktop.ps1
    publish-exe.ps1
    windows-preventer.ps1
  src/
    WindowsPreventer.App/
    WindowsPreventer.Core/
    WindowsPreventer.Cli/
    WindowsPreventer.Licensing/
    WindowsPreventer/
```

## Main Projects

### WindowsPreventer.App

This is the desktop application.

It contains the administrator UI.

The UI currently supports:

- Selecting a policy JSON file.
- Selecting a license JSON file.
- Showing license status.
- Showing detected Windows users on startup.
- Entering selected target users.
- Copying selected standard users into the target list.
- Copying all standard users into the target list.
- Choosing whether to exempt administrators.
- Toggling restriction options.
- Previewing changes.
- Applying changes.
- Removing changes.
- Viewing output inside the app.

Apply is disabled when the selected license is missing, invalid, or expired. Preview and Remove stay available.

Important files:

```text
src\WindowsPreventer.App\MainWindow.xaml
src\WindowsPreventer.App\MainWindow.xaml.cs
src\WindowsPreventer.App\WindowsPreventer.App.csproj
```

### WindowsPreventer.Core

This is the shared business logic library.

It is similar to a Java service/module JAR.

It does not care whether it is called from the desktop app or the command-line app.

It handles:

- Loading policy options into model classes.
- Resolving Windows user accounts to SIDs.
- Checking whether a user is a local administrator.
- Checking whether the app is running elevated.
- Previewing policy changes.
- Applying or removing Windows registry policy values.

Important files:

```text
src\WindowsPreventer.Core\PolicyEngine.cs
src\WindowsPreventer.Core\AccountResolver.cs
src\WindowsPreventer.Core\AdministratorChecker.cs
src\WindowsPreventer.Core\Elevation.cs
src\WindowsPreventer.Core\Models\
```

### WindowsPreventer.Cli

This is a command-line executable.

It is useful for automation and testing.

Example:

```powershell
.\artifacts\publish\win-x64\windows-preventer.exe --config .\config\policies.sample.json --license .\config\license.sample.json --what-if
```

Important files:

```text
src\WindowsPreventer.Cli\Program.cs
src\WindowsPreventer.Cli\WindowsPreventer.Cli.csproj
```

### WindowsPreventer.Licensing

This is the shared license validation library.

It handles:

- Loading a local license JSON file.
- Checking required fields.
- Checking expiration date.
- Returning a license status such as Missing, Invalid, Expired, or Valid.
- Helping the UI and CLI decide whether Apply should be allowed.

Important files:

```text
src\WindowsPreventer.Licensing\LicenseInfo.cs
src\WindowsPreventer.Licensing\LicenseStatus.cs
src\WindowsPreventer.Licensing\LicenseValidationResult.cs
src\WindowsPreventer.Licensing\LicenseService.cs
```

### PowerShell Prototype

The PowerShell module was the first prototype.

It remains useful as reference/admin tooling, but it is not the main product path.

Important files:

```text
src\WindowsPreventer\WindowsPreventer.psm1
scripts\windows-preventer.ps1
```

## How The Pieces Work Together

The desktop app calls the core engine.

```text
WindowsPreventer.App
  -> WindowsPreventer.Licensing
  -> WindowsPreventer.Core
    -> Windows registry policy values
```

The CLI also calls the same core engine.

```text
WindowsPreventer.Cli
  -> WindowsPreventer.Licensing
  -> WindowsPreventer.Core
    -> Windows registry policy values
```

This means we do not duplicate business logic.

If we add a new restriction, it should usually be added to `WindowsPreventer.Core` first. Then the desktop UI and CLI can expose it.

## Policy Config File

The sample policy config is:

```text
config\policies.sample.json
```

Example:

```json
{
  "targetUsers": [],
  "exemptAdministrators": true,
  "controls": {
    "disableRightClick": true,
    "hideRestartAndShutdown": true,
    "disableClipboardHistory": true,
    "disableCopyPaste": false
  }
}
```

Meaning:

- `targetUsers` contains the users to restrict.
- Empty `targetUsers` means no user-specific target is selected.
- `exemptAdministrators` prevents local admins from being restricted.
- `controls` contains the restriction switches.

The desktop app shows detected Windows users separately. Admins can use `Use Selected` or `Use All Standard` to copy users into `targetUsers`.

Some controls are user-specific and some are machine-wide:

| Control                                            | Scope          |
| -------------------------------------------------- | -------------- |
| Disable Explorer right-click                       | Selected users |
| Hide restart and shutdown commands                 | Selected users |
| Disable clipboard history and cloud clipboard sync | Machine-wide   |

## License Config File

The sample license config is:

```text
config\license.sample.json
```

Example:

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

Current license behavior:

| License state | Preview | Apply   | Remove  |
| ------------- | ------- | ------- | ------- |
| Missing       | Allowed | Blocked | Allowed |
| Invalid       | Allowed | Blocked | Allowed |
| Expired       | Allowed | Blocked | Allowed |
| Valid         | Allowed | Allowed | Allowed |

This first version validates local license structure and expiration. It does not yet validate cryptographic signatures.

## What Has Been Built So Far

Built and validated:

- .NET 8 SDK installed on the development machine.
- C# solution created.
- Shared Core library created.
- CLI executable created and published.
- WPF desktop app created.
- Desktop app wired to the Core policy engine.
- Licensing project created.
- Desktop app shows license status and blocks Apply without a valid license.
- CLI supports `--license` and blocks Apply without a valid license.
- Desktop app published as a Windows `.exe`.
- Dry-run behavior validated so preview mode does not apply policy changes.

Current desktop executable:

```text
artifacts\publish\desktop-win-x64\WindowsPreventer.App.exe
```

## Safety Behavior

The app has a preview mode.

Preview mode shows what would happen but does not change Windows.

Real apply/remove operations require administrator elevation.

This is intentional because Windows registry policy changes should not happen silently or accidentally.

## Important Limitation

Full basic copy/paste blocking is not implemented yet.

Reason: Windows does not provide a clean universal per-user policy switch for all copy/paste behavior across every application.

Possible future approaches:

- Kiosk mode.
- Assigned Access.
- AppLocker or Windows Defender Application Control.
- Application-specific restrictions.
- A signed endpoint agent for controlled environments.

We should not implement fragile keyboard interception as the main approach.
