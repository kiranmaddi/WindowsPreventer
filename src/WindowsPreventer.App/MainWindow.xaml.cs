using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using WindowsPreventer.Core;
using WindowsPreventer.Core.Models;
using WindowsPreventer.Licensing;

namespace WindowsPreventer.App;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MainWindow()
    {
        InitializeComponent();
        SetElevationStatus();
        ConfigPathTextBox.Text = GetDefaultConfigPath();
        LicensePathTextBox.Text = GetDefaultLicensePath();
        LoadConfigFromPath(ConfigPathTextBox.Text);
        RefreshLicenseStatus();
        LoadDetectedProfiles();
    }

    private void BrowseConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select policy config",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = ConfigPathTextBox.Text
        };

        if (dialog.ShowDialog(this) == true)
        {
            ConfigPathTextBox.Text = dialog.FileName;
            LoadConfigFromPath(dialog.FileName);
        }
    }

    private void BrowseLicense_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select license file",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = LicensePathTextBox.Text
        };

        if (dialog.ShowDialog(this) == true)
        {
            LicensePathTextBox.Text = dialog.FileName;
            RefreshLicenseStatus();
        }
    }

    private void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        SaveConfigToPath(ConfigPathTextBox.Text);
    }

    private void PreviewChanges_Click(object sender, RoutedEventArgs e)
    {
        ExecutePolicy(PolicyMode.Apply, whatIf: true);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var licenseResult = RefreshLicenseStatus();

        if (!licenseResult.CanApply)
        {
            AppendOutput(licenseResult.Message + Environment.NewLine);
            return;
        }

        if (!Elevation.IsProcessElevated())
        {
            AppendOutput($"Apply requires administrator elevation. Use Run as Administrator first.{Environment.NewLine}");
            return;
        }

        if (!ConfirmPolicyChange("Apply"))
        {
            AppendOutput($"Apply was cancelled.{Environment.NewLine}");
            return;
        }

        ExecutePolicy(PolicyMode.Apply, whatIf: false);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (!Elevation.IsProcessElevated())
        {
            AppendOutput($"Remove requires administrator elevation. Use Run as Administrator first.{Environment.NewLine}");
            return;
        }

        if (!ConfirmPolicyChange("Remove"))
        {
            AppendOutput($"Remove was cancelled.{Environment.NewLine}");
            return;
        }

        ExecutePolicy(PolicyMode.Remove, whatIf: false);
    }

    private void ClearOutput_Click(object sender, RoutedEventArgs e)
    {
        OutputTextBox.Clear();
    }

    private void RefreshProfiles_Click(object sender, RoutedEventArgs e)
    {
        LoadDetectedProfiles();
    }

    private void RunAsAdministrator_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var executablePath = Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                AppendOutput($"Could not find the current executable path.{Environment.NewLine}");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                Verb = "runas"
            });

            Close();
        }
        catch (Exception exception)
        {
            AppendOutput($"Could not relaunch as administrator: {exception.Message}{Environment.NewLine}");
        }
    }

    private void UseSelectedProfiles_Click(object sender, RoutedEventArgs e)
    {
        var selectedProfiles = DetectedProfilesDataGrid.SelectedItems
            .OfType<UserProfileInfo>()
            .Where(profile => profile.IsTargetable)
            .ToArray();

        if (selectedProfiles.Length == 0)
        {
            AppendOutput($"No detected Windows users were selected.{Environment.NewLine}");
            return;
        }

        var targetProfiles = selectedProfiles
            .Where(profile => !IsAdminExemptionEnabled() || !profile.IsAdministrator)
            .Select(profile => profile.UserName)
            .ToArray();

        if (targetProfiles.Length == 0)
        {
            AppendOutput($"Only administrator users were selected. They were not added because local administrator exemption is enabled.{Environment.NewLine}");
            return;
        }

        SetTargetUsers(targetProfiles);
        AppendOutput($"Loaded {targetProfiles.Length} selected user(s) into Selected Target Users.{Environment.NewLine}");
    }

    private void UseAllStandardProfiles_Click(object sender, RoutedEventArgs e)
    {
        var standardProfiles = DetectedProfilesDataGrid.Items
            .OfType<UserProfileInfo>()
            .Where(profile => profile.IsTargetable && !profile.IsAdministrator)
            .Select(profile => profile.UserName)
            .ToArray();

        if (standardProfiles.Length == 0)
        {
            AppendOutput($"No standard users were detected.{Environment.NewLine}");
            return;
        }

        SetTargetUsers(standardProfiles);
        AppendOutput($"Loaded all {standardProfiles.Length} standard user(s) into Selected Target Users.{Environment.NewLine}");
    }

    private bool IsAdminExemptionEnabled()
    {
        return ExemptAdministratorsCheckBox.IsChecked == true;
    }

    private void SetElevationStatus()
    {
        var isElevated = Elevation.IsProcessElevated();
        ElevationStatusText.Text = isElevated ? "Elevated" : "Preview only";
        RunAsAdministratorButton.Visibility = isElevated ? Visibility.Collapsed : Visibility.Visible;
    }

    private bool ConfirmPolicyChange(string action)
    {
        var targetUsers = GetTargetUsers();
        var targetSummary = targetUsers.Length == 0
            ? "No selected user-specific targets"
            : string.Join(Environment.NewLine, targetUsers.Select(user => $"- {user}"));

        var message = $"{action} selected Windows policy restrictions?{Environment.NewLine}{Environment.NewLine}" +
            $"Selected users:{Environment.NewLine}{targetSummary}{Environment.NewLine}{Environment.NewLine}" +
            "Machine-wide controls may affect the whole computer.";

        return MessageBox.Show(this, message, $"Confirm {action}", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void AddManualTargetUser_Click(object sender, RoutedEventArgs e)
    {
        var userName = ManualTargetUserTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            AppendOutput($"Enter a user name before adding a target.{Environment.NewLine}");
            return;
        }

        AddTargetUser(userName);
        ManualTargetUserTextBox.Clear();
        AppendOutput($"Added target user: {userName}{Environment.NewLine}");
    }

    private void RemoveSelectedTargetUsers_Click(object sender, RoutedEventArgs e)
    {
        var selectedUsers = SelectedTargetUsersListBox.SelectedItems
            .OfType<string>()
            .ToArray();

        if (selectedUsers.Length == 0)
        {
            AppendOutput($"No target users were selected for removal.{Environment.NewLine}");
            return;
        }

        foreach (var user in selectedUsers)
        {
            SelectedTargetUsersListBox.Items.Remove(user);
        }

        AppendOutput($"Removed {selectedUsers.Length} target user(s).{Environment.NewLine}");
    }

    private void ClearTargetUsers_Click(object sender, RoutedEventArgs e)
    {
        SelectedTargetUsersListBox.Items.Clear();
        AppendOutput($"Cleared all selected target users.{Environment.NewLine}");
    }

    private void ExecutePolicy(PolicyMode mode, bool whatIf)
    {
        try
        {
            var config = BuildConfigFromUi();
            var output = new StringWriter();
            var error = new StringWriter();
            var engine = new PolicyEngine(output, error);

            engine.Execute(config, mode, whatIf);
            AppendOutput(output.ToString());
            AppendOutput(error.ToString());

            if (!whatIf && config.TargetUsers.Length > 0 && IsCurrentUserTargeted(config.TargetUsers))
            {
                OfferExplorerRestart();
            }
        }
        catch (Exception exception)
        {
            AppendOutput(exception.Message + Environment.NewLine);
        }
    }

    private static bool IsCurrentUserTargeted(IEnumerable<string> targetUsers)
    {
        using var currentIdentity = WindowsIdentity.GetCurrent();
        var currentSid = currentIdentity.User?.Value;

        if (string.IsNullOrEmpty(currentSid))
        {
            return false;
        }

        foreach (var userName in targetUsers)
        {
            try
            {
                var account = new NTAccount(userName);
                var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

                if (string.Equals(sid.Value, currentSid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (IdentityNotMappedException)
            {
                // Ignore accounts that cannot be resolved; PolicyEngine already reports this.
            }
        }

        return false;
    }

    private void OfferExplorerRestart()
    {
        var result = MessageBox.Show(
            this,
            "Explorer caches shell restrictions (like the right-click context menu) and may not reflect this change until it restarts. Restart Windows Explorer for the current user now?",
            "Restart Explorer?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                process.Kill();
            }

            Process.Start("explorer.exe");
            AppendOutput($"Restarted Windows Explorer.{Environment.NewLine}");
        }
        catch (Exception exception)
        {
            AppendOutput($"Failed to restart Windows Explorer: {exception.Message}{Environment.NewLine}");
        }
    }

    private LicenseValidationResult RefreshLicenseStatus()
    {
        var result = new LicenseService().Validate(LicensePathTextBox.Text);
        LicenseStatusText.Text = result.Message;
        ApplyButton.IsEnabled = result.CanApply;

        return result;
    }

    private void LoadDetectedProfiles()
    {
        try
        {
            var profiles = new UserProfileService().GetKnownProfiles();
            DetectedProfilesDataGrid.ItemsSource = profiles;
            AppendOutput($"Detected {profiles.Count} Windows profile(s).{Environment.NewLine}");
        }
        catch (Exception exception)
        {
            AppendOutput($"Failed to detect profiles: {exception.Message}{Environment.NewLine}");
        }
    }

    private void LoadConfigFromPath(string path)
    {
        if (!File.Exists(path))
        {
            AppendOutput($"Config file not found: {path}{Environment.NewLine}");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<PolicyConfig>(json, JsonOptions) ?? new PolicyConfig();
            ApplyConfigToUi(config);
            AppendOutput($"Loaded config: {path}{Environment.NewLine}");
        }
        catch (Exception exception)
        {
            AppendOutput($"Failed to load config: {exception.Message}{Environment.NewLine}");
        }
    }

    private void SaveConfigToPath(string path)
    {
        try
        {
            var config = BuildConfigFromUi();
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
            AppendOutput($"Saved config: {path}{Environment.NewLine}");
        }
        catch (Exception exception)
        {
            AppendOutput($"Failed to save config: {exception.Message}{Environment.NewLine}");
        }
    }

    private PolicyConfig BuildConfigFromUi()
    {
        return new PolicyConfig
        {
            TargetUsers = GetTargetUsers(),
            ExemptAdministrators = IsAdminExemptionEnabled(),
            Controls = new PolicyControls
            {
                DisableRightClick = DisableRightClickCheckBox.IsChecked == true,
                HideRestartAndShutdown = HideRestartShutdownCheckBox.IsChecked == true,
                DisableClipboardHistory = DisableClipboardHistoryCheckBox.IsChecked == true,
                DisableCopyPaste = false
            }
        };
    }

    private void ApplyConfigToUi(PolicyConfig config)
    {
        SetTargetUsers(config.TargetUsers);
        ExemptAdministratorsCheckBox.IsChecked = config.ExemptAdministrators;
        DisableRightClickCheckBox.IsChecked = config.Controls.DisableRightClick;
        HideRestartShutdownCheckBox.IsChecked = config.Controls.HideRestartAndShutdown;
        DisableClipboardHistoryCheckBox.IsChecked = config.Controls.DisableClipboardHistory;
        DisableCopyPasteCheckBox.IsChecked = config.Controls.DisableCopyPaste;
    }

    private string[] GetTargetUsers()
    {
        return SelectedTargetUsersListBox.Items
            .OfType<string>()
            .Where(userName => !string.IsNullOrWhiteSpace(userName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void SetTargetUsers(IEnumerable<string> users)
    {
        SelectedTargetUsersListBox.Items.Clear();

        foreach (var user in users.Where(userName => !string.IsNullOrWhiteSpace(userName)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            SelectedTargetUsersListBox.Items.Add(user);
        }
    }

    private void AddTargetUser(string userName)
    {
        if (SelectedTargetUsersListBox.Items.OfType<string>().Any(existing => string.Equals(existing, userName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedTargetUsersListBox.Items.Add(userName);
    }

    private void AppendOutput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        OutputTextBox.AppendText(text);
        OutputTextBox.ScrollToEnd();
    }

    private static string GetDefaultConfigPath()
    {
        var appConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "policies.sample.json");

        if (File.Exists(appConfigPath))
        {
            return appConfigPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "config", "policies.sample.json"));
    }

    private static string GetDefaultLicensePath()
    {
        var appLicensePath = Path.Combine(AppContext.BaseDirectory, "config", "license.sample.json");

        if (File.Exists(appLicensePath))
        {
            return appLicensePath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "config", "license.sample.json"));
    }
}