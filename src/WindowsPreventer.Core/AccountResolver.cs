using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using WindowsPreventer.Core.Models;

namespace WindowsPreventer.Core;

internal sealed class AccountResolver
{
    private static readonly Regex UserSidPattern = new("^S-1-5-21-.+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ResolvedAccount Resolve(string userName)
    {
        var account = new NTAccount(userName);
        var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

        return new ResolvedAccount(userName, sid.Value);
    }

    public IReadOnlyList<ResolvedAccount> GetLoadedUserAccounts()
    {
        var accounts = new List<ResolvedAccount>();

        foreach (var subKeyName in Registry.Users.GetSubKeyNames())
        {
            if (!UserSidPattern.IsMatch(subKeyName) || subKeyName.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var sid = new SecurityIdentifier(subKeyName);
                var account = (NTAccount)sid.Translate(typeof(NTAccount));
                accounts.Add(new ResolvedAccount(account.Value, sid.Value));
            }
            catch (IdentityNotMappedException)
            {
                Console.Error.WriteLine($"Warning: skipping loaded profile {subKeyName} because it could not be resolved to an account.");
            }
        }

        return accounts;
    }
}