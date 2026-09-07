using System.DirectoryServices.AccountManagement;

namespace WindowsPreventer.Core;

internal sealed class AdministratorChecker
{
    public bool IsLocalAdministrator(string sid)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, "Administrators");

        if (group is null)
        {
            throw new InvalidOperationException("The local Administrators group could not be found.");
        }

        foreach (var principal in group.GetMembers(recursive: true))
        {
            using (principal)
            {
                if (string.Equals(principal.Sid?.Value, sid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}