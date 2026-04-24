#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$SourceDir,
    [Parameter(Mandatory = $true)][string]$BuildDir,
    [Parameter(Mandatory = $true)][string]$StageDir,
    [string]$Configuration = 'Release',
    [string]$Arch = 'x64'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Find-VsInstall {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found at $vswhere. Install Visual Studio Build Tools." }
    $path = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $path) { throw 'No Visual Studio installation with MSVC x86/x64 tools found.' }
    return $path.Trim()
}

function Import-VcVars {
    param([string]$VsPath, [string]$Arch)
    $vcvars = Join-Path $VsPath 'VC\Auxiliary\Build\vcvarsall.bat'
    if (-not (Test-Path $vcvars)) { throw "vcvarsall.bat not found at $vcvars." }
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        $vsInstaller = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
        & cmd /c "set `"PATH=%PATH%;$vsInstaller`" && `"$vcvars`" $Arch >nul 2>&1 && set > `"$tmp`""
        if ($LASTEXITCODE -ne 0) { throw "vcvarsall.bat failed with exit code $LASTEXITCODE." }
        foreach ($line in Get-Content $tmp) {
            if ($line -match '^([^=]+)=(.*)$') {
                Set-Item -Path "env:$($Matches[1])" -Value $Matches[2]
            }
        }
    }
    finally {
        Remove-Item $tmp -ErrorAction SilentlyContinue
    }
}

function Find-BundledNinja {
    param([string]$VsPath)
    $ninja = Join-Path $VsPath 'Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe'
    if (-not (Test-Path $ninja)) {
        $found = Get-Command ninja -ErrorAction SilentlyContinue
        if ($found) { return $found.Source }
        throw "Ninja not found. Install the 'C++ CMake tools for Windows' VS component, or put ninja on PATH."
    }
    return $ninja
}

$SourceDir = [System.IO.Path]::GetFullPath($SourceDir)
$BuildDir  = [System.IO.Path]::GetFullPath($BuildDir)
$StageDir  = [System.IO.Path]::GetFullPath($StageDir)

if (-not (Test-Path (Join-Path $SourceDir 'CMakeLists.txt'))) {
    throw "libjxl source not found at $SourceDir. Did you run 'git submodule update --init --recursive'?"
}
if (-not (Test-Path (Join-Path $SourceDir 'third_party\highway\CMakeLists.txt'))) {
    throw "libjxl third-party submodules missing (highway). Run 'git submodule update --init --recursive' inside $SourceDir."
}

$vs = Find-VsInstall
Write-Host "[build-native] VS install: $vs"
Import-VcVars -VsPath $vs -Arch $Arch
$ninja = Find-BundledNinja -VsPath $vs
Write-Host "[build-native] Ninja: $ninja"
Write-Host "[build-native] cl.exe: $((Get-Command cl.exe).Source)"

New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null
New-Item -ItemType Directory -Force -Path $StageDir | Out-Null

$cacheFile = Join-Path $BuildDir 'CMakeCache.txt'
if (-not (Test-Path $cacheFile)) {
    Write-Host "[build-native] Configuring CMake ($Configuration)"
    & cmake `
        -S $SourceDir `
        -B $BuildDir `
        -G Ninja `
        "-DCMAKE_MAKE_PROGRAM=$ninja" `
        "-DCMAKE_BUILD_TYPE=$Configuration" `
        -DBUILD_SHARED_LIBS=ON `
        -DBUILD_TESTING=OFF `
        -DJPEGXL_ENABLE_TOOLS=OFF `
        -DJPEGXL_ENABLE_MANPAGES=OFF `
        -DJPEGXL_ENABLE_JPEGLI=OFF `
        -DJPEGXL_ENABLE_JNI=OFF `
        -DJPEGXL_ENABLE_SJPEG=OFF `
        -DJPEGXL_ENABLE_BENCHMARK=OFF `
        -DJPEGXL_ENABLE_EXAMPLES=OFF `
        -DJPEGXL_ENABLE_DOXYGEN=OFF `
        -DJPEGXL_ENABLE_PLUGINS=OFF `
        -DJPEGXL_ENABLE_DEVTOOLS=OFF `
        -DJPEGXL_ENABLE_COVERAGE=OFF `
        -DJPEGXL_WARNINGS_AS_ERRORS=OFF
    if ($LASTEXITCODE -ne 0) { throw "cmake configure failed ($LASTEXITCODE)." }
}

Write-Host "[build-native] Building jxl + jxl_threads"
& cmake --build $BuildDir --target jxl jxl_threads
if ($LASTEXITCODE -ne 0) { throw "cmake build failed ($LASTEXITCODE)." }

$required = @(
    'lib\jxl.dll',
    'lib\jxl_threads.dll',
    'lib\jxl_cms.dll',
    'third_party\brotli\brotlicommon.dll',
    'third_party\brotli\brotlidec.dll',
    'third_party\brotli\brotlienc.dll'
)
$resolved = foreach ($rel in $required) {
    $full = Join-Path $BuildDir $rel
    if (-not (Test-Path $full)) { throw "Expected native output not produced: $full" }
    $full
}

Write-Host "[build-native] Staging DLLs to $StageDir"
foreach ($dll in $resolved) {
    Copy-Item -Path $dll -Destination $StageDir -Force
}

Write-Host "[build-native] Done."
