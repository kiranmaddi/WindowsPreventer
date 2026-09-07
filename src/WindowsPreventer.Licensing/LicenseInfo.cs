namespace WindowsPreventer.Licensing;

public sealed class LicenseInfo
{
    public string CustomerName { get; set; } = string.Empty;

    public string LicenseId { get; set; } = string.Empty;

    public string Edition { get; set; } = string.Empty;

    public DateOnly ExpiresOn { get; set; }

    public int MaxDevices { get; set; }

    public string[] Features { get; set; } = [];
}