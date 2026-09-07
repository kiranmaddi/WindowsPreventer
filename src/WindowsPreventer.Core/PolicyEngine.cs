using Microsoft.Win32;
using WindowsPreventer.Core.Models;

namespace WindowsPreventer.Core;

public sealed class PolicyEngine
{
    private readonly AccountResolver accountResolver = new();
    private readonly AdministratorChecker administratorChecker = new();
    private readonly TextWriter output;
    private readonly TextWriter error;

    public PolicyEngine()
        : this(Console.Out, Console.Error)
    {
    }

    public PolicyEngine(TextWriter output, TextWriter error)
    {
        this.output = output;
        this.error = error;
    }

    public void Execute(PolicyConfig config, PolicyMode mode, bool whatIf)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!whatIf && !Elevation.IsProcessElevated())
        {
            throw new InvalidOperationException("Apply and remove operations must be run from an elevated terminal. Re-run with --what-if to preview without elevation.");
        }

        var policyValues = GetPolicyValues(config.Controls);
        var machinePolicyValues = policyValues.Where(value => value.Scope == PolicyRegistryScope.Machine).ToArray();
        var userPolicyValues = policyValues.Where(value => value.Scope == PolicyRegistryScope.User).ToArray();
        var targetAccounts = ResolveTargetAccounts(config);

        foreach (var policyValue in machinePolicyValues)
        {
            ExecuteMachinePolicy(policyValue, mode, whatIf);
        }

        if (targetAccounts.Count == 0 && userPolicyValues.Length > 0)
        {
            error.WriteLine("No selected target users. User-specific restrictions were skipped.");
            return;
        }

        var appliedAnyUserPolicy = false;

        foreach (var account in targetAccounts)
        {
            if (config.ExemptAdministrators && administratorChecker.IsLocalAdministrator(account.Sid))
            {
                output.WriteLine($"Skipping administrator account: {account.UserName}");
                continue;
            }

            if (!IsUserHiveLoaded(account.Sid))
            {
                error.WriteLine($"Warning: skipping {account.UserName} because HKEY_USERS\\{account.Sid} is not loaded. Sign in as that user or load NTUSER.DAT before applying per-user policies.");
                continue;
            }

            foreach (var policyValue in userPolicyValues)
            {
                ExecuteUserPolicy(account, policyValue, mode, whatIf);
            }

            appliedAnyUserPolicy = true;
        }

        if (appliedAnyUserPolicy && !whatIf && userPolicyValues.Length > 0)
        {
            ExplorerRefresh.NotifyPolicyChanged();
            output.WriteLine("Notified running Explorer processes of the policy change. If a restriction (e.g. right-click menu) does not appear immediately for a signed-in user, restart explorer.exe for that user or have them sign out and back in.");
        }
    }

    private IReadOnlyList<ResolvedAccount> ResolveTargetAccounts(PolicyConfig config)
    {
        if (config.TargetUsers.Length == 0)
        {
            return [];
        }

        return config.TargetUsers.Select(accountResolver.Resolve).ToArray();
    }

    private static bool IsUserHiveLoaded(string sid)
    {
        return Registry.Users.GetSubKeyNames().Any(subKeyName => string.Equals(subKeyName, sid, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<PolicyRegistryValue> GetPolicyValues(PolicyControls controls)
    {
        if (controls.DisableCopyPaste)
        {
            throw new NotSupportedException("disableCopyPaste is not supported in this phase. Use kiosk mode, app-level controls, or a managed endpoint agent design.");
        }

        var values = new List<PolicyRegistryValue>();

        if (controls.DisableRightClick)
        {
            values.Add(new PolicyRegistryValue(PolicyRegistryScope.User, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoViewContextMenu", 1));
        }

        if (controls.HideRestartAndShutdown)
        {
            values.Add(new PolicyRegistryValue(PolicyRegistryScope.User, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoClose", 1));
        }

        if (controls.DisableClipboardHistory)
        {
            values.Add(new PolicyRegistryValue(PolicyRegistryScope.Machine, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", 0));
            values.Add(new PolicyRegistryValue(PolicyRegistryScope.Machine, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0));
        }

        return values;
    }

    private void ExecuteMachinePolicy(PolicyRegistryValue policyValue, PolicyMode mode, bool whatIf)
    {
        ExecuteRegistryPolicy(Registry.LocalMachine, "HKEY_LOCAL_MACHINE", policyValue, mode, whatIf, output);
    }

    private void ExecuteUserPolicy(ResolvedAccount account, PolicyRegistryValue policyValue, PolicyMode mode, bool whatIf)
    {
        if (whatIf)
        {
            var fullPath = $@"HKEY_USERS\{account.Sid}\{policyValue.Path}\{policyValue.Name}";
            var action = mode == PolicyMode.Apply
                ? $"set DWORD {fullPath} to {policyValue.Value}"
                : $"remove {fullPath}";

            output.WriteLine($"What if: {action}");
            return;
        }

        using var userHive = Registry.Users.OpenSubKey(account.Sid, writable: true);

        if (userHive is null)
        {
            error.WriteLine($"Warning: skipping {account.UserName} because HKEY_USERS\\{account.Sid} is not loaded.");
            return;
        }

        ExecuteRegistryPolicy(userHive, $"HKEY_USERS\\{account.Sid}", policyValue, mode, whatIf, output);
    }

    private static void ExecuteRegistryPolicy(RegistryKey baseKey, string basePath, PolicyRegistryValue policyValue, PolicyMode mode, bool whatIf, TextWriter output)
    {
        var fullPath = $@"{basePath}\{policyValue.Path}\{policyValue.Name}";

        if (mode == PolicyMode.Apply)
        {
            if (whatIf)
            {
                output.WriteLine($"What if: set DWORD {fullPath} to {policyValue.Value}");
                return;
            }

            using var key = baseKey.CreateSubKey(policyValue.Path, writable: true);
            key.SetValue(policyValue.Name, policyValue.Value, RegistryValueKind.DWord);
            output.WriteLine($"Set DWORD {fullPath} to {policyValue.Value}");
            return;
        }

        if (whatIf)
        {
            output.WriteLine($"What if: remove {fullPath}");
            return;
        }

        using var existingKey = baseKey.OpenSubKey(policyValue.Path, writable: true);
        existingKey?.DeleteValue(policyValue.Name, throwOnMissingValue: false);
        output.WriteLine($"Removed {fullPath}");
    }
}