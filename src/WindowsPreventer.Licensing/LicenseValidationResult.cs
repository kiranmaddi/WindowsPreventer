namespace WindowsPreventer.Licensing;

public sealed class LicenseValidationResult
{
    public LicenseStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public LicenseInfo? License { get; init; }

    public bool CanApply => Status == LicenseStatus.Valid;
}