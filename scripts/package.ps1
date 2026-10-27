# Packages a built QuickPane for release: QuickPane-<version>-x64.zip holding the program and its
# license, SHA256SUMS.txt for the zip and each file in it, and notes.md for the release page.
# CI runs it on every build, so packaging is proven before anyone pushes a tag; the release
# workflow runs it on the tagged commit. Run scripts/build.ps1 first.
#
# Usage:  pwsh -File scripts/package.ps1 [-Version 3.10.0.2] [-Configuration Release] [-OutDir dist] [-NoNotes]
# Output: <OutDir>\QuickPane-<version>-x64.zip, SHA256SUMS.txt, and notes.md unless -NoNotes
param(
    [string]$Version,
    [ValidateSet('Release', 'Debug')][string]$Configuration = 'Release',
    [string]$OutDir = 'dist',
    [switch]$NoNotes
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $repo "QuickPane\bin\x64\$Configuration"
$exe = Join-Path $bin 'QuickPane.exe'
$config = Join-Path $bin 'QuickPane.exe.config'
$license = Join-Path $repo 'LICENSE'
foreach ($f in $exe, $config) {
    if (-not (Test-Path $f)) { throw "Missing $f. Run scripts/build.ps1 -Configuration $Configuration first." }
}

# The exe's own file version is the source of truth. A tag that disagrees with it means
# AssemblyInfo.cs was not bumped, and the zip would carry a program that misreports its version
# to the in-app update check.
$built = (Get-Item $exe).VersionInfo.FileVersion
if (-not $Version) { $Version = $built }
if ($built -ne $Version) { throw "Built version is $built, expected $Version. Bump QuickPane\Properties\AssemblyInfo.cs." }

if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path $repo $OutDir }
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force $OutDir | Out-Null

# LICENSE ships inside the zip because MIT requires the copyright notice to travel with copies.
$zip = Join-Path $OutDir "QuickPane-$Version-x64.zip"
Compress-Archive -Path $exe, $config, $license -DestinationPath $zip
$sums = Join-Path $OutDir 'SHA256SUMS.txt'
@(
    foreach ($f in $exe, $config, $license, $zip) {
        '{0}  {1}' -f (Get-FileHash $f -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $f -Leaf)
    }
) | Set-Content $sums -Encoding ascii

if (-not $NoNotes) {
    # A full release document in docs/releases/<version>.md is used as is, minus its "# ..." title,
    # which would repeat the release title. Otherwise the notes are this version's changelog section
    # plus the generic install steps below.
    $notes = Join-Path $OutDir 'notes.md'
    $doc = Join-Path $repo "docs\releases\$Version.md"
    if (Test-Path $doc) {
        (Get-Content $doc | Select-Object -Skip 1 | Out-String).Trim() | Set-Content $notes -Encoding utf8
    } else {
        $cl = Get-Content (Join-Path $repo 'CHANGELOG.md') -Raw
        $m = [regex]::Match($cl, "(?ms)^## \[$([regex]::Escape($Version))\][^\n]*\n(.*?)(?=^## \[)")
        if (-not $m.Success) { throw "No docs\releases\$Version.md and no CHANGELOG.md section for $Version." }
        $install = @'
## Install

1. Close QuickPane, and wait for it to finish: `Start-Process "$env:LOCALAPPDATA\QuickPane\QuickPane.exe" -ArgumentList '--exit' -Wait`
2. Keep a copy of the current `QuickPane.exe` in `%LOCALAPPDATA%\QuickPane`, so you can go back.
3. Unzip the download, and copy `QuickPane.exe` and `QuickPane.exe.config` into `%LOCALAPPDATA%\QuickPane`, replacing the files there.
4. Start `%LOCALAPPDATA%\QuickPane\QuickPane.exe` again.

`SHA256SUMS.txt` lists the SHA-256 hash of the zip and of each file inside it. The files are not code-signed. This release also carries a GitHub build provenance attestation, which `gh attestation verify <file> -R prisant-labs/windows-explorer-quickpane` checks.
'@
        ($m.Groups[1].Value.Trim() + "`n`n" + $install) | Set-Content $notes -Encoding utf8
    }
    # Release notes are posted publicly, so stop on anything that looks like a local path.
    $hits = Select-String -Path $notes -Pattern '(?i)\b[a-z]:\\(users|projects)\\|onedrive'
    if ($hits) { $hits; throw 'Possible personal data in the release notes.' }
}

Get-Content $sums
"Packaged $Version into $OutDir"
