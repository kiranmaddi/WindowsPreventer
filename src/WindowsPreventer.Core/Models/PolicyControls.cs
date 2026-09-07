namespace WindowsPreventer.Core.Models;

public sealed class PolicyControls
{
    public bool DisableRightClick { get; set; }

    public bool HideRestartAndShutdown { get; set; }

    public bool DisableClipboardHistory { get; set; }

    public bool DisableCopyPaste { get; set; }
}