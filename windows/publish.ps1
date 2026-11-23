param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',
    [string]$OutputDirectory,
    [switch]$AppOnly
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'TinyHand.Windows/TinyHand.Windows.csproj'
$output = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $PSScriptRoot "../dist/windows/$Runtime" }
$pluginArguments = @()
if (-not $AppOnly) {
    Push-Location (Join-Path $PSScriptRoot '../stream-deck')
    try {
        npm ci --ignore-scripts
        if ($LASTEXITCODE -ne 0) { throw 'Plugin dependency installation failed.' }
        npm run package
        if ($LASTEXITCODE -ne 0) { throw 'Plugin packaging failed.' }
    } finally { Pop-Location }
    $package = (Resolve-Path (Join-Path $PSScriptRoot '../stream-deck/out/io.github.zebrajaeger.tinyhand.streamDeckPlugin')).Path
    $metadata = (Resolve-Path (Join-Path $PSScriptRoot '../stream-deck/out/plugin-metadata.json')).Path
    $pluginArguments = @("-p:RequireStreamDeckPackage=true", "-p:StreamDeckPackagePath=$package", "-p:StreamDeckMetadataPath=$metadata")
}
dotnet publish $project -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true @pluginArguments --source https://api.nuget.org/v3/index.json -o $output
if ($LASTEXITCODE -ne 0) { throw 'TinyHand publish failed.' }
$check = Start-Process (Join-Path $output 'TinyHand.Windows.exe') -ArgumentList '--check-audio-resources' -Wait -PassThru
if ($check.ExitCode -ne 0) { throw 'Published audio resource validation failed.' }
if (-not $AppOnly) {
    $pluginCheck = Start-Process (Join-Path $output 'TinyHand.Windows.exe') -ArgumentList '--check-stream-deck-plugin-resources' -Wait -PassThru
    if ($pluginCheck.ExitCode -ne 0) { throw 'Published plugin resource validation failed.' }
}
Write-Host "Portable TinyHand: $output/TinyHand.Windows.exe"
