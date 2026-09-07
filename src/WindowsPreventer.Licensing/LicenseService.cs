using System.Text.Json;

namespace WindowsPreventer.Licensing;

public sealed class LicenseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LicenseValidationResult Validate(string licensePath, DateOnly? currentDate = null)
    {
        if (string.IsNullOrWhiteSpace(licensePath) || !File.Exists(licensePath))
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Missing,
                Message = "License file was not found. Preview and Remove are available, but Apply is blocked."
            };
        }

        LicenseInfo? license;

        try
        {
            var json = File.ReadAllText(licensePath);
            license = JsonSerializer.Deserialize<LicenseInfo>(json, JsonOptions);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Invalid,
                Message = $"License file could not be read: {exception.Message}"
            };
        }

        if (license is null)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Invalid,
                Message = "License file is empty or invalid."
            };
        }

        var requiredFieldError = ValidateRequiredFields(license);

        if (requiredFieldError is not null)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Invalid,
                Message = requiredFieldError,
                License = license
            };
        }

        var today = currentDate ?? DateOnly.FromDateTime(DateTime.Today);

        if (license.ExpiresOn < today)
        {
            return new LicenseValidationResult
            {
                Status = LicenseStatus.Expired,
                Message = $"License {license.LicenseId} expired on {license.ExpiresOn:yyyy-MM-dd}. Preview and Remove are available, but Apply is blocked.",
                License = license
            };
        }

        return new LicenseValidationResult
        {
            Status = LicenseStatus.Valid,
            Message = $"License {license.LicenseId} is valid for {license.CustomerName} until {license.ExpiresOn:yyyy-MM-dd}.",
            License = license
        };
    }

    public bool AllowsFeature(LicenseValidationResult result, string featureName)
    {
        return result.Status == LicenseStatus.Valid &&
            result.License?.Features.Any(feature => string.Equals(feature, featureName, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static string? ValidateRequiredFields(LicenseInfo license)
    {
        if (string.IsNullOrWhiteSpace(license.CustomerName))
        {
            return "License customerName is required.";
        }

        if (string.IsNullOrWhiteSpace(license.LicenseId))
        {
            return "License licenseId is required.";
        }

        if (string.IsNullOrWhiteSpace(license.Edition))
        {
            return "License edition is required.";
        }

        if (license.ExpiresOn == default)
        {
            return "License expiresOn is required.";
        }

        if (license.MaxDevices <= 0)
        {
            return "License maxDevices must be greater than zero.";
        }

        return null;
    }
}