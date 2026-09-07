namespace WindowsPreventer.Core.Models;

public sealed class UserProfileInfo
{
    public string UserName { get; init; } = string.Empty;

    public string Sid { get; init; } = string.Empty;

    public string ProfilePath { get; init; } = string.Empty;

    public bool IsLoaded { get; init; }

    public bool IsTargetable { get; init; }

    public bool IsAdministrator { get; init; }

    public string ProfileType => IsAdministrator ? "Administrator" : "Standard";

    public string LoadStatus => IsLoaded ? "Loaded" : "Not loaded";

    public string TargetStatus => IsTargetable ? "Targetable" : "Hidden";
}