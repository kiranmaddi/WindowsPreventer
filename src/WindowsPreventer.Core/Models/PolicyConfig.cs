namespace WindowsPreventer.Core.Models;

public sealed class PolicyConfig
{
    public string[] TargetUsers { get; set; } = [];

    public bool ExemptAdministrators { get; set; } = true;

    public PolicyControls Controls { get; set; } = new();
}