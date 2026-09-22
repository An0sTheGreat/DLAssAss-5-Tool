param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$managerRoot = Split-Path -Parent $PSScriptRoot
$sourceAddon = Join-Path $managerRoot "Payload\renodx-dlss5-super-anus.addon64"
$expectedAddonHash = "7393AB462F9CDA74DA9E1AEEED9DB832D8FCBC65C6CCDF2136455729D680E455"
$expectedDllHashes = @{
    "nvngx_dlss.dll" = "3975567B8943C53ACCE397F2B72380092F84F162D00B0D2C7D08A1025C563983"
    "nvngx_dlssg.dll" = "FF6E90EB78B827927DFF5B4ECC6B1C870C2E9BCA29ED9F48C7D348CC9E170B82"
    "nvngx_dlssnr.dll" = "E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A"
}
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $managerRoot "..\..\.."))
$releaseDirectory = Join-Path $workspaceRoot "artifacts\v1.1.2"
$publishDirectory = Join-Path $workspaceRoot "artifacts\.manager-v1.1.2-publish"
$archive = Join-Path $releaseDirectory "DLAssAss-5-Tool-v1.1.2-$Runtime.zip"
$manualArchive = Join-Path $releaseDirectory "DLSS-5-Super-Anus-Manually-v1.1.2.zip"
$looseAddon = Join-Path $releaseDirectory ([IO.Path]::GetFileName($sourceAddon))
$fullArtifactsRoot = [IO.Path]::GetFullPath((Join-Path $workspaceRoot "artifacts")) + [IO.Path]::DirectorySeparatorChar
foreach ($path in @($releaseDirectory, $publishDirectory)) {
    if (-not ([IO.Path]::GetFullPath($path).StartsWith($fullArtifactsRoot, [StringComparison]::OrdinalIgnoreCase))) {
        throw "Unsafe publish path: $path"
    }
}
$localDotnet = Join-Path $managerRoot ".tools\dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { "dotnet" }
$sdkVersion = & $dotnet --version
if ($LASTEXITCODE -ne 0 -or [int]($sdkVersion.Split('.')[0]) -lt 8) { throw ".NET 8 SDK or newer is required." }

foreach ($entry in $expectedDllHashes.GetEnumerator()) {
    $path = Join-Path $managerRoot "DLSS Files\$($entry.Key)"
    if (-not (Test-Path -LiteralPath $path)) { throw "Required bundled runtime not found: $path" }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($hash -ne $entry.Value) { throw "Unexpected $($entry.Key) hash: $hash" }
}
if (-not (Test-Path -LiteralPath $sourceAddon)) { throw "Add-on payload not found: $sourceAddon" }
$addonHash = (Get-FileHash -LiteralPath $sourceAddon -Algorithm SHA256).Hash
if ($addonHash -ne $expectedAddonHash) { throw "Unexpected add-on payload hash: $addonHash" }

if ((Test-Path -LiteralPath $publishDirectory) -or (Test-Path -LiteralPath $releaseDirectory)) {
    throw "Release output already exists; preserve it and choose a fresh output path."
}
New-Item -ItemType Directory -Path $releaseDirectory | Out-Null
$project = Join-Path $managerRoot "DLAssAss5Tool.csproj"
& $dotnet restore $project -r $Runtime --configfile (Join-Path $managerRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
& $dotnet publish $project -c $Configuration -r $Runtime --self-contained true --no-restore -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$payloadDirectory = Join-Path $publishDirectory "Payload"
$dlssDirectory = Join-Path $publishDirectory "DLSS Files"
New-Item -ItemType Directory -Path $payloadDirectory, $dlssDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourceAddon -Destination (Join-Path $payloadDirectory ([IO.Path]::GetFileName($sourceAddon))) -Force
Copy-Item -LiteralPath (Join-Path $managerRoot "Payload\DLSS5_Feed.fx") -Destination $payloadDirectory -Force
Copy-Item -LiteralPath (Join-Path $managerRoot "DLSS Files\README.md") -Destination $dlssDirectory -Force
foreach ($name in $expectedDllHashes.Keys) {
    Copy-Item -LiteralPath (Join-Path $managerRoot "DLSS Files\$name") -Destination $dlssDirectory -Force
}
Copy-Item -LiteralPath (Join-Path $managerRoot "README.md") -Destination $publishDirectory -Force
foreach ($document in @("CHANGELOG.md", "VERSION", "THIRD_PARTY_NOTICES.md")) {
    Copy-Item -LiteralPath (Join-Path $managerRoot $document) -Destination $publishDirectory
}
Copy-Item -LiteralPath (Join-Path $managerRoot "licenses") -Destination $publishDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $managerRoot "docs") -Destination $publishDirectory -Recurse
New-Item -ItemType Directory -Path (Join-Path $publishDirectory "assets") -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $managerRoot "assets\compatibility-experimental.svg") -Destination (Join-Path $publishDirectory "assets")
$checksums = @(
    Get-FileHash -LiteralPath (Join-Path $publishDirectory "DLAssAss 5 Tool.exe") -Algorithm SHA256
    Get-FileHash -LiteralPath (Join-Path $payloadDirectory ([IO.Path]::GetFileName($sourceAddon))) -Algorithm SHA256
    Get-FileHash -LiteralPath (Join-Path $payloadDirectory "DLSS5_Feed.fx") -Algorithm SHA256
    $expectedDllHashes.Keys | ForEach-Object { Get-FileHash -LiteralPath (Join-Path $dlssDirectory $_) -Algorithm SHA256 }
) | ForEach-Object { "$($_.Hash)  $($_.Path.Substring($publishDirectory.Length + 1).Replace('\', '/'))" }
$checksums | Set-Content -LiteralPath (Join-Path $publishDirectory "SHA256SUMS.txt") -Encoding utf8

& tar.exe -a -c -f $archive -C $publishDirectory .
if ($LASTEXITCODE -ne 0) { throw "archive creation failed." }
Copy-Item -LiteralPath $sourceAddon -Destination $looseAddon
& tar.exe -a -c -f $manualArchive -C $releaseDirectory ([IO.Path]::GetFileName($looseAddon))
if ($LASTEXITCODE -ne 0) { throw "manual archive creation failed." }
@($archive, $manualArchive, $looseAddon) | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    "$($hash.Hash)  $([IO.Path]::GetFileName($_))"
} | Set-Content -LiteralPath (Join-Path $releaseDirectory "SHA256SUMS.txt") -Encoding utf8
Remove-Item -LiteralPath $publishDirectory -Recurse -Force
Write-Host "Published: $releaseDirectory"
Write-Host "Archive:   $archive"
