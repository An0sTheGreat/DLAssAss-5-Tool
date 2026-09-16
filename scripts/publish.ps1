param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$managerRoot = Split-Path -Parent $PSScriptRoot
$sourceAddon = Join-Path $managerRoot "Payload\renodx-dlss5-super-anus.addon64"
$expectedAddonHash = "6A1A9F37FB8F861C02BFDFFF001C5285F202649C694E10356BFBB5F1F9C3EA12"
$releaseDirectory = Join-Path $managerRoot "artifacts\v.1.0.8"
$publishDirectory = Join-Path $releaseDirectory "publish"
$archive = Join-Path $releaseDirectory "DLAssAss-5-Tool-$Runtime.zip"
$fullManagerRoot = [IO.Path]::GetFullPath($managerRoot) + [IO.Path]::DirectorySeparatorChar
if (-not ([IO.Path]::GetFullPath($publishDirectory).StartsWith($fullManagerRoot, [StringComparison]::OrdinalIgnoreCase))) {
    throw "Unsafe publish path: $publishDirectory"
}
$localDotnet = Join-Path $managerRoot ".tools\dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { "dotnet" }
$sdkVersion = & $dotnet --version
if ($LASTEXITCODE -ne 0 -or [int]($sdkVersion.Split('.')[0]) -lt 8) { throw ".NET 8 SDK or newer is required." }

$bundledDlls = Get-ChildItem -LiteralPath (Join-Path $managerRoot "DLSS Files") -Filter "*.dll" -File
if ($bundledDlls) { throw "Remove NVIDIA DLLs from 'DLSS Files' before publishing. They must be user supplied." }
if (-not (Test-Path -LiteralPath $sourceAddon)) { throw "Add-on payload not found: $sourceAddon" }
$addonHash = (Get-FileHash -LiteralPath $sourceAddon -Algorithm SHA256).Hash
if ($addonHash -ne $expectedAddonHash) { throw "Unexpected add-on payload hash: $addonHash" }

if ((Test-Path -LiteralPath $publishDirectory) -or (Test-Path -LiteralPath $archive)) {
    throw "Release output already exists; preserve it and choose a fresh output path."
}
$project = Join-Path $managerRoot "DLAssAss5Tool.csproj"
& $dotnet restore $project -r $Runtime --configfile (Join-Path $managerRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
& $dotnet publish $project -c $Configuration -r $Runtime --self-contained true --no-restore -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$payloadDirectory = Join-Path $publishDirectory "Payload"
$dlssDirectory = Join-Path $publishDirectory "DLSS Files"
New-Item -ItemType Directory -Path $payloadDirectory, $dlssDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourceAddon -Destination (Join-Path $payloadDirectory ([IO.Path]::GetFileName($sourceAddon))) -Force
Copy-Item -LiteralPath (Join-Path $managerRoot "DLSS Files\README.md") -Destination $dlssDirectory -Force
Copy-Item -LiteralPath (Join-Path $managerRoot "README.md") -Destination $publishDirectory -Force
foreach ($document in @("CHANGELOG.md", "VERSION", "THIRD_PARTY_NOTICES.md")) {
    Copy-Item -LiteralPath (Join-Path $managerRoot $document) -Destination $publishDirectory
}
Copy-Item -LiteralPath (Join-Path $managerRoot "licenses") -Destination $publishDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $managerRoot "docs") -Destination $publishDirectory -Recurse
New-Item -ItemType Directory -Path (Join-Path $publishDirectory "assets") -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $managerRoot "assets\compatibility-experimental.svg") -Destination (Join-Path $publishDirectory "assets")
if (Get-ChildItem -LiteralPath $dlssDirectory -Filter "*.dll" -File) { throw "Publish contains an NVIDIA DLL." }

$checksums = @(
    Get-FileHash -LiteralPath (Join-Path $publishDirectory "DLAssAss 5 Tool.exe") -Algorithm SHA256
    Get-FileHash -LiteralPath (Join-Path $payloadDirectory ([IO.Path]::GetFileName($sourceAddon))) -Algorithm SHA256
) | ForEach-Object { "$($_.Hash)  $($_.Path.Substring($publishDirectory.Length + 1).Replace('\', '/'))" }
$checksums | Set-Content -LiteralPath (Join-Path $publishDirectory "SHA256SUMS.txt") -Encoding utf8

& tar.exe -a -c -f $archive -C $publishDirectory .
if ($LASTEXITCODE -ne 0) { throw "archive creation failed." }
$archiveHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
"$archiveHash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath (Join-Path $releaseDirectory "SHA256SUMS-v.1.0.8.txt") -Encoding utf8
Write-Host "Published: $publishDirectory"
Write-Host "Archive:   $archive"
