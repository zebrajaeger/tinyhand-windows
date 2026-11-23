param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'TinyHand.Windows/TinyHand.Windows.csproj'
$output = Join-Path $PSScriptRoot "../dist/windows/$Runtime"
dotnet publish $project -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --source https://api.nuget.org/v3/index.json -o $output
if ($LASTEXITCODE -ne 0) { throw 'TinyHand publish failed.' }
$check = Start-Process (Join-Path $output 'TinyHand.Windows.exe') -ArgumentList '--check-audio-resources' -Wait -PassThru
if ($check.ExitCode -ne 0) { throw 'Published audio resource validation failed.' }
Write-Host "Portable TinyHand: $output/TinyHand.Windows.exe"
