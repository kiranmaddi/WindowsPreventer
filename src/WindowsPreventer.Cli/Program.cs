using System.Text.Json;
using WindowsPreventer.Core;
using WindowsPreventer.Core.Models;
using WindowsPreventer.Licensing;

var options = CommandLineOptions.Parse(args);

if (options.ShowHelp)
{
    CommandLineOptions.WriteHelp();
    return 0;
}

if (options.ListProfiles)
{
    foreach (var profile in new UserProfileService().GetKnownProfiles())
    {
        Console.WriteLine($"{profile.UserName}\t{profile.ProfileType}\t{profile.LoadStatus}\t{profile.ProfilePath}");
    }

    return 0;
}

try
{
    var json = File.ReadAllText(options.ConfigPath);
    var config = JsonSerializer.Deserialize<PolicyConfig>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? throw new InvalidOperationException("The policy config file is empty or invalid.");

    var engine = new PolicyEngine();

    if (options.Mode == PolicyMode.Apply && !options.WhatIf)
    {
        var licenseResult = new LicenseService().Validate(options.LicensePath);

        if (!licenseResult.CanApply)
        {
            throw new InvalidOperationException(licenseResult.Message);
        }

        Console.WriteLine(licenseResult.Message);
    }

    engine.Execute(config, options.Mode, options.WhatIf);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

internal sealed class CommandLineOptions
{
    public string ConfigPath { get; init; } = @"config\policies.sample.json";

    public PolicyMode Mode { get; init; } = PolicyMode.Apply;

    public string LicensePath { get; init; } = @"config\license.sample.json";

    public bool WhatIf { get; init; }

    public bool ListProfiles { get; init; }

    public bool ShowHelp { get; init; }

    public static CommandLineOptions Parse(string[] args)
    {
        var configPath = @"config\policies.sample.json";
        var licensePath = @"config\license.sample.json";
        var mode = PolicyMode.Apply;
        var whatIf = false;
        var listProfiles = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            switch (arg.ToLowerInvariant())
            {
                case "--help":
                case "-h":
                    return new CommandLineOptions { ShowHelp = true };
                case "--config":
                case "-c":
                    configPath = RequireValue(args, ref index, arg);
                    break;
                case "--mode":
                case "-m":
                    var modeValue = RequireValue(args, ref index, arg);
                    if (!Enum.TryParse(modeValue, ignoreCase: true, out mode))
                    {
                        throw new ArgumentException($"Invalid mode '{modeValue}'. Expected Apply or Remove.");
                    }
                    break;
                case "--license":
                case "-l":
                    licensePath = RequireValue(args, ref index, arg);
                    break;
                case "--what-if":
                    whatIf = true;
                    break;
                case "--list-profiles":
                    listProfiles = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        return new CommandLineOptions
        {
            ConfigPath = configPath,
            LicensePath = licensePath,
            Mode = mode,
            WhatIf = whatIf,
            ListProfiles = listProfiles
        };
    }

    public static void WriteHelp()
    {
        Console.WriteLine("Windows Preventer CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  windows-preventer --config config\\policies.sample.json --license config\\license.sample.json --what-if");
        Console.WriteLine("  windows-preventer --config config\\policies.sample.json --license config\\license.sample.json --mode Apply");
        Console.WriteLine("  windows-preventer --config config\\policies.sample.json --mode Remove");
        Console.WriteLine("  windows-preventer --list-profiles");
    }

    private static string RequireValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}.");
        }

        index++;
        return args[index];
    }
}