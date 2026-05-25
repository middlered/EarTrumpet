param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('x86')]
    [string]$Platform = 'x86',

    [string]$ArtifactsBase = (Join-Path $PSScriptRoot '..\..\artifacts'),
    [string]$WixBin
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }
    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Find-MSBuild {
    $candidates = @()
    $vswhereCandidates = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    )

    foreach ($vswhere in $vswhereCandidates) {
        if (Test-Path -LiteralPath $vswhere) {
            $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\amd64\MSBuild.exe'
            if ($found) {
                $candidates += $found
            }
            $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\MSBuild.exe'
            if ($found) {
                $candidates += $found
            }
        }
    }

    $candidates += @(
        'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
    )

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $cmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    throw 'MSBuild.exe was not found. Install Visual Studio with MSBuild, or pass a Developer PowerShell where MSBuild is on PATH.'
}

function Find-Wix([string]$ExplicitRoot) {
    $candidates = @()
    if ($ExplicitRoot) {
        if (Test-Path -LiteralPath $ExplicitRoot -PathType Leaf) {
            $candidates += $ExplicitRoot
        }
        else {
            $candidates += (Join-Path $ExplicitRoot 'wix.exe')
        }
    }

    $cmd = Get-Command wix.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $candidates += @(
        "${env:ProgramFiles}\WiX Toolset v7\bin\wix.exe",
        "${env:ProgramFiles}\WiX Toolset v6\bin\wix.exe",
        "${env:ProgramFiles}\WiX Toolset v5\bin\wix.exe",
        "${env:ProgramFiles}\WiX Toolset v4\bin\wix.exe",
        "${env:ProgramFiles(x86)}\WiX Toolset v7\bin\wix.exe",
        "${env:ProgramFiles(x86)}\WiX Toolset v6\bin\wix.exe",
        "${env:ProgramFiles(x86)}\WiX Toolset v5\bin\wix.exe",
        "${env:ProgramFiles(x86)}\WiX Toolset v4\bin\wix.exe"
    )

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'wix.exe was not found. Install WiX Toolset (v4+), or pass -WixBin to the wix.exe folder.'
}

function Invoke-LoggedCommand([string]$FilePath, [string[]]$Arguments) {
    Write-Host "> $FilePath $($Arguments -join ' ')"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath"
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $repoRoot

$msbuild = Find-MSBuild
$wix = Find-Wix -ExplicitRoot $WixBin

$buildOutput = Resolve-FullPath (Join-Path $repoRoot "Build\$Configuration")
$artifactsRoot = Resolve-FullPath (Join-Path $ArtifactsBase 'msi')
$objDir = Join-Path $artifactsRoot 'obj'

New-Item -ItemType Directory -Path $artifactsRoot, $objDir -Force | Out-Null

Invoke-LoggedCommand $msbuild @(
    'EarTrumpet\EarTrumpet.csproj',
    "/p:Configuration=$Configuration",
    "/p:Platform=$Platform",
    '/v:minimal',
    '-maxcpucount'
)

$exePath = Join-Path $buildOutput 'EarTrumpet.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Build completed but EarTrumpet.exe was not found at '$exePath'."
}

$version = (Get-Item -LiteralPath $exePath).VersionInfo.FileVersion
if (-not $version) {
    $version = '1.0.0.0'
}

$productWxs = Join-Path $PSScriptRoot 'Product.wxs'

$msiPath = Join-Path $artifactsRoot "EarTrumpet_$version.msi"
Invoke-LoggedCommand $wix @(
    'build',
    '-arch', $Platform,
    '-o', $msiPath,
    '-intermediatefolder', $objDir,
    '-ext', 'WixToolset.UI.wixext',
    '-d', "BuildOutputDir=$buildOutput",
    '-d', "AppVersion=$version",
    '-d', "SourceDir=$repoRoot",
    $productWxs
)

Write-Host ''
Write-Host "MSI build completed: $msiPath"
