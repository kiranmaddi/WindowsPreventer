[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Runtime = 'win-x64',

    [string] $Configuration = 'Release'
)

$projectPath = Join-Path -Path $PSScriptRoot -ChildPath '..\src\WindowsPreventer.Cli\WindowsPreventer.Cli.csproj'
$publishPath = Join-Path -Path $PSScriptRoot -ChildPath "..\artifacts\publish\$Runtime"

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --output $publishPath

Write-Host "Published executable to $publishPath"