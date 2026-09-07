using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using Microsoft.Win32;
using WindowsPreventer.Core.Models;

namespace WindowsPreventer.Core;

public sealed class UserProfileService
{
    private const string ProfileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
    private static readonly string[] IgnoredProfileNames = ["Default", "Default User", "Public", "All Users", "DefaultAppPool"];
    private readonly AdministratorChecker administratorChecker = new();

    public IReadOnlyList<UserProfileInfo> GetLoadedProfiles()
    {
        return GetKnownProfiles().Where(profile => profile.IsLoaded).ToArray();
    }

    public IReadOnlyList<UserProfileInfo> GetKnownProfiles()
    {
        var profiles = new List<UserProfileInfo>();
        var loadedSids = Registry.Users.GetSubKeyNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var profileListKey = Registry.LocalMachine.OpenSubKey(ProfileListPath, writable: false);

        if (profileListKey is null)
        {
            return [];
        }

        foreach (var sid in profileListKey.GetSubKeyNames())
        {
            using var profileKey = profileListKey.OpenSubKey(sid, writable: false);
            var profilePath = profileKey?.GetValue("ProfileImagePath")?.ToString() ?? string.Empty;

            if (!IsUserProfilePath(profilePath))
            {
                continue;
            }

            var userName = ResolveUserName(sid, profilePath);

            if (!IsTargetableUserProfile(userName, profilePath))
            {
                continue;
            }

            profiles.Add(new UserProfileInfo
            {
                UserName = userName,
                Sid = sid,
                ProfilePath = profilePath,
                IsLoaded = loadedSids.Contains(sid),
                IsTargetable = true,
                IsAdministrator = IsAdministrator(sid)
            });
        }

        return profiles
            .OrderBy(profile => profile.IsAdministrator)
            .ThenByDescending(profile => profile.IsLoaded)
            .ThenBy(profile => profile.UserName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsUserProfilePath(string profilePath)
    {
        if (string.IsNullOrWhiteSpace(profilePath))
        {
            return false;
        }

        var fullPath = Environment.ExpandEnvironmentVariables(profilePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var usersRoot = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Users"));
        var profileName = Path.GetFileName(fullPath);

        return fullPath.StartsWith(usersRoot, StringComparison.OrdinalIgnoreCase) &&
            !IgnoredProfileNames.Contains(profileName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsTargetableUserProfile(string userName, string profilePath)
    {
        var profileName = Path.GetFileName(profilePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        if (profileName.EndsWith("_$", StringComparison.OrdinalIgnoreCase) || userName.EndsWith("_$", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (userName.StartsWith("IIS APPPOOL\\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private bool IsAdministrator(string sid)
    {
        try
        {
            return administratorChecker.IsLocalAdministrator(sid);
        }
        catch (Exception exception) when (exception is PrincipalOperationException or InvalidOperationException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string ResolveUserName(string sid, string profilePath)
    {
        try
        {
            var securityIdentifier = new SecurityIdentifier(sid);
            var account = (NTAccount)securityIdentifier.Translate(typeof(NTAccount));

            return account.Value;
        }
        catch (Exception exception) when (exception is IdentityNotMappedException or ArgumentException or SystemException)
        {
            var profileFolderName = Path.GetFileName(profilePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            return string.IsNullOrWhiteSpace(profileFolderName) ? sid : profileFolderName;
        }
    }
}