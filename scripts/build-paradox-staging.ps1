param(
    [string] $Version = "0.1.0",
    [string] $ContentSource
)

$ErrorActionPreference = "Stop"

function Assert-ChildPath {
    param(
        [string] $Parent,
        [string] $Child
    )

    $parentPath = [IO.Path]::GetFullPath($Parent).TrimEnd(
        [char[]] @(
            [IO.Path]::DirectorySeparatorChar,
            [IO.Path]::AltDirectorySeparatorChar))
    $childPath = [IO.Path]::GetFullPath($Child)
    if (-not $childPath.StartsWith(
            $parentPath + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path '$childPath' is outside '$parentPath'."
    }

    return $childPath
}

$repositoryRoot = [IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot ".."))
$projectRoot = Join-Path $repositoryRoot "src\ProgressionControls"
$configurationPath = Join-Path `
    $projectRoot `
    "Properties\PublishConfiguration.xml"
$thumbnailPath = Join-Path `
    $projectRoot `
    "Properties\Thumbnail.png"
$screenshotRelativePaths = @(
    "Properties/Screenshot-Settings.png"
)
$screenshotPaths = @(
    $screenshotRelativePaths |
        ForEach-Object {
            Join-Path $projectRoot $_
        }
)
$localModsPath = [Environment]::GetEnvironmentVariable(
    "CSII_LOCALMODSPATH",
    "User")

if ([string]::IsNullOrWhiteSpace($ContentSource)) {
    if ([string]::IsNullOrWhiteSpace($localModsPath)) {
        throw "CSII_LOCALMODSPATH is not configured."
    }

    $ContentSource = Join-Path `
        $localModsPath `
        "Kobbyist.ProgressionControls"
}

$contentSourcePath = [IO.Path]::GetFullPath($ContentSource)
$approvedSourceFiles = @(
    "Kobbyist.ProgressionControls.Core.dll",
    "Kobbyist.ProgressionControls.Core.pdb",
    "Kobbyist.ProgressionControls.dll",
    "Kobbyist.ProgressionControls.pdb",
    "Kobbyist.ProgressionControls_linux_x86_64.so",
    "Kobbyist.ProgressionControls_mac_x86_64.bundle",
    "Kobbyist.ProgressionControls_win_x86_64.dll",
    "Kobbyist.ProgressionControls_win_x86_64.pdb",
    "LICENSE"
)

$requiredInputPaths = @(
    $configurationPath,
    $thumbnailPath,
    $contentSourcePath
)
$requiredInputPaths += $screenshotPaths
foreach ($requiredPath in $requiredInputPaths) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required packaging input is missing: $requiredPath"
    }
}

foreach ($relativePath in $approvedSourceFiles) {
    $requiredPath = Join-Path $contentSourcePath $relativePath
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required deployed file is missing: $requiredPath"
    }
}

[xml] $configuration = Get-Content -Raw -LiteralPath $configurationPath

function Get-PublishValue {
    param(
        $Node
    )

    if ($null -eq $Node) {
        return $null
    }
    if ($Node -is [System.Xml.XmlElement]) {
        if ($Node.HasAttribute("Value")) {
            return $Node.GetAttribute("Value")
        }
        return $Node.InnerText
    }
    return [string] $Node
}

$requiredMetadata = @{
    ModId = "154015"
    DisplayName = "Progression Controls"
    ModVersion = $Version
    GameVersion = "1.6.*"
    Thumbnail = "Properties/Thumbnail.png"
    Tag = "Code Mod"
    AccessLevel = "Private"
}
foreach ($entry in $requiredMetadata.GetEnumerator()) {
    $node = $configuration.Publish.($entry.Key)
    if ((Get-PublishValue $node) -ne $entry.Value) {
        throw ("Metadata {0} must equal '{1}'." -f
            $entry.Key,
            $entry.Value)
    }
}
$configuredScreenshots = @(
    $configuration.Publish.Screenshot |
        ForEach-Object {
            Get-PublishValue $_
        }
)
if (($configuredScreenshots -join "`n") -cne
        ($screenshotRelativePaths -join "`n")) {
    throw ("Configured screenshots must equal: {0}." -f
        ($screenshotRelativePaths -join ", "))
}

foreach ($field in @(
        "ShortDescription",
        "LongDescription",
        "ChangeLog")) {
    $node = $configuration.Publish.($field)
    if ([string]::IsNullOrWhiteSpace((Get-PublishValue $node))) {
        throw "Metadata $field must not be empty."
    }
}

Add-Type -AssemblyName System.Drawing
$thumbnail = [Drawing.Image]::FromFile($thumbnailPath)
try {
    if ($thumbnail.Width -ne 950 -or $thumbnail.Height -ne 500) {
        throw "Thumbnail must be exactly 950x500 pixels."
    }
}
finally {
    $thumbnail.Dispose()
}

$nestedDirectories = @(
    Get-ChildItem -LiteralPath $contentSourcePath -Directory)
if ($nestedDirectories.Count -gt 0) {
    $names = ($nestedDirectories.FullName -join ", ")
    throw "Unexpected nested content directories found: $names"
}

$unexpectedFiles = @(
    Get-ChildItem -LiteralPath $contentSourcePath -File |
        Where-Object {
            $approvedSourceFiles -cnotcontains $_.Name
        }
)
if ($unexpectedFiles.Count -gt 0) {
    $names = ($unexpectedFiles.FullName -join ", ")
    throw "Unexpected deployed files found: $names"
}

$artifactRoot = Join-Path $repositoryRoot "artifacts\paradox"
$stageName = "ProgressionControls-$Version-private"
$stageRoot = Assert-ChildPath `
    $artifactRoot `
    (Join-Path $artifactRoot $stageName)
$archivePath = Assert-ChildPath `
    $artifactRoot `
    (Join-Path $artifactRoot "$stageName.zip")

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$propertiesStage = Join-Path $stageRoot "Properties"
$contentStage = Join-Path $stageRoot "Content"
[void] (New-Item -ItemType Directory -Path $propertiesStage -Force)
[void] (New-Item -ItemType Directory -Path $contentStage -Force)

Copy-Item -LiteralPath $configurationPath -Destination (
    Join-Path $propertiesStage "PublishConfiguration.xml")
Copy-Item -LiteralPath $thumbnailPath -Destination (
    Join-Path $propertiesStage "Thumbnail.png")
foreach ($screenshotPath in $screenshotPaths) {
    Copy-Item `
        -LiteralPath $screenshotPath `
        -Destination (Join-Path $propertiesStage (
            Split-Path -Leaf $screenshotPath))
}

foreach ($relativePath in $approvedSourceFiles) {
    Copy-Item `
        -LiteralPath (Join-Path $contentSourcePath $relativePath) `
        -Destination (Join-Path $contentStage $relativePath)
}

$hashLines = Get-ChildItem -LiteralPath $stageRoot -Recurse -File |
    Sort-Object FullName |
    ForEach-Object {
        $relativePath = $_.FullName.Substring(
            $stageRoot.Length + 1).Replace("\", "/")
        $hash = ((Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash).ToLowerInvariant()
        "$hash  $relativePath"
    }
[IO.File]::WriteAllLines(
    (Join-Path $stageRoot "SHA256SUMS.txt"),
    $hashLines,
    [Text.UTF8Encoding]::new($false))

Compress-Archive `
    -Path (Join-Path $stageRoot "*") `
    -DestinationPath $archivePath `
    -CompressionLevel Optimal

$archiveHash = ((Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash).ToLowerInvariant()

[pscustomobject] @{
    Stage = $stageRoot
    Archive = $archivePath
    ArchiveSha256 = $archiveHash
    ContentFiles = @(
        Get-ChildItem -LiteralPath $contentStage -Recurse -File
    ).Count
}
