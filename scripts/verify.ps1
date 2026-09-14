#Requires -Version 7.2
param(
    [switch] $IncludeGame
)

$ErrorActionPreference = "Stop"

function Invoke-CheckedCommand {
    param(
        [string] $Command,
        [string[]] $Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$npmCommand = if ($IsWindows) { "npm.cmd" } else { "npm" }

Push-Location $repositoryRoot
try {
    # Validate even when dependencies were installed with engine checks bypassed.
    Invoke-CheckedCommand node @(
        "-e",
        "if (process.versions.node.split('.')[0] !== '24') { console.error('Use Node 24.x.'); process.exit(1); }"
    )

    $uiDirectory = "src/ProgressionControls/UI"
    if (-not (Test-Path -LiteralPath "$uiDirectory/node_modules/.bin/eslint")) {
        throw "Install UI dependencies first: npm ci --prefix $uiDirectory"
    }

    Invoke-CheckedCommand $npmCommand @("--prefix", $uiDirectory, "run", "check")

    # Folder mode avoids loading the game project's deployment targets.
    $csharpFiles = @(git -c core.quotepath=false ls-files --cached --others --exclude-standard -- "*.cs")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not enumerate C# source files."
    }
    $csharpFiles = @($csharpFiles | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
    if ($csharpFiles.Count -eq 0) {
        throw "No C# source files were found for the formatting check."
    }
    Invoke-CheckedCommand dotnet (@(
        "format", "whitespace", ".", "--folder", "--verify-no-changes",
        "--verbosity", "normal", "--include"
    ) + $csharpFiles)

    Invoke-CheckedCommand dotnet @(
        "test", "tests/ProgressionControls.Core.Tests/ProgressionControls.Core.Tests.csproj",
        "--configuration", "Release"
    )

    if ($IncludeGame) {
        # The official Update gate skips post-processing and deployment.
        Invoke-CheckedCommand dotnet @(
            "build", "src/ProgressionControls/ProgressionControls.csproj",
            "--configuration", "Release", "-p:ModPublisherCommand=Update"
        )
    }

    Invoke-CheckedCommand git @("diff", "--check")
}
finally {
    Pop-Location
}
