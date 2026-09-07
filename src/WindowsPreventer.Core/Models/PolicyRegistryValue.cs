namespace WindowsPreventer.Core.Models;

internal sealed record PolicyRegistryValue(
    PolicyRegistryScope Scope,
    string Path,
    string Name,
    int Value);