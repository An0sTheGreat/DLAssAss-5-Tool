param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$managerRoot = Split-Path -Parent $PSScriptRoot
$sourceAddon = Join-Path $managerRoot "Payload\renodx-dlss5-super-anus.addon64"
$expectedAddonHash = "8F5C9D481AEF47EBB81EA0DD88884BB0CAA2A83B1002B4D8C177DB04611DCEF8"
$expectedDllHashes = @{
    "nvngx_dlss.dll" = "3975567B8943C53ACCE397F2B72380092F84F162D00B0D2C7D08A1025C563983"
    "nvngx_dlssg.dll" = "FF6E90EB78B827927DFF5B4ECC6B1C870C2E9BCA29ED9F48C7D348CC9E170B82"
    "nvngx_dlssnr.dll" = "E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A"
}
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $managerRoot "..\..\.."))
$releaseDirectory = Join-Path $workspaceRoot "artifacts\manager-v1.1.1"
$publishDirectory = Join-Path $releaseDirectory "publish"
$archive = Join-Path $releaseDirectory "DLAssAss-5-Tool-v1.1.1-$Runtime.zip"
$fullArtifactsRoot = [IO.Path]::GetFullPath((Join-Path $workspaceRoot "artifacts")) + [IO.Path]::DirectorySeparatorChar
if (-not ([IO.Path]::GetFullPath($publishDirectory).StartsWith($fullArtifactsRoot, [StringComparison]::OrdinalIgnoreCase))) {
    throw "Unsafe publish path: $publishDirectory"
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
$archiveHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
"$archiveHash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath (Join-Path $releaseDirectory "SHA256SUMS-v1.1.1.txt") -Encoding utf8
Write-Host "Published: $publishDirectory"
Write-Host "Archive:   $archive"
