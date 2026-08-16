# Check for hardcoded Arabic literals in MessageBox.Show calls (.cs)
# and in Header=/Content= attributes (.xaml).
# Intended for CI: exits with code 1 and prints matches when found.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts/check-arabic-literals.ps1

[CmdletBinding()]
param(
    [string]$Root = ""
)

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

$arabic = "[\u0600-\u06FF]"
$issues = [System.Collections.Generic.List[object]]::new()

$exclude = @("\\obj\\", "\\bin\\", "\\.git\\", "\\Backups\\", "\\Certificates_Output\\")

# Legacy files grandfathered from the check (see arabic-legacy-files.txt).
# Keep this list small and only for pre-existing violations.
$legacyFile = Join-Path $PSScriptRoot "arabic-legacy-files.txt"
$legacy = @()
if (Test-Path $legacyFile) {
    $legacy = Get-Content $legacyFile |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -ne "" -and -not $_.StartsWith("#") }
}

function Test-Excluded([string]$path) {
    foreach ($p in $exclude) {
        if ($path -match $p) { return $true }
    }
    $rel = $path
    if ($path.StartsWith($Root + "\")) { $rel = $path.Substring($Root.Length + 1) }
    $norm = $rel -replace "/", "\"
    foreach ($l in $legacy) {
        if ($norm -ieq $l) { return $true }
    }
    return $false
}

Get-ChildItem -Path $Root -Recurse -Filter *.cs -File -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.FullName) } |
    ForEach-Object {
        $file = $_
        $lines = [System.IO.File]::ReadAllLines($file.FullName)
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            $trimmed = $line.TrimStart()
            if ($trimmed.StartsWith("//") -or $trimmed.StartsWith("///")) { continue }
            if ($line -match $arabic -and $line -match "MessageBox") {
                $issues.Add([pscustomobject]@{ File = $file.FullName; Line = ($i + 1); Text = $line.Trim() })
            }
        }
    }

Get-ChildItem -Path $Root -Recurse -Filter *.xaml -File -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.FullName) } |
    ForEach-Object {
        $file = $_
        $lines = [System.IO.File]::ReadAllLines($file.FullName)
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            if ($line -match "<!--") { continue }
            if (($line -match "Header\s*=" -or $line -match "Content\s*=") -and $line -match $arabic) {
                $issues.Add([pscustomobject]@{ File = $file.FullName; Line = ($i + 1); Text = $line.Trim() })
            }
        }
    }

if ($issues.Count -gt 0) {
    Write-Host ""
    Write-Host "FAIL: Found $($issues.Count) hardcoded Arabic literal(s) in MessageBox/Header/Content." -ForegroundColor Red
    Write-Host "Use the translation system (Translations.Get) and TranslationViewModel.Instance instead." -ForegroundColor Red
    Write-Host ""
    $issues | ForEach-Object { Write-Host ("  {0}({1}): {2}" -f $_.File, $_.Line, $_.Text) }
    exit 1
}

Write-Host "OK: No hardcoded Arabic literals found in MessageBox/Header/Content." -ForegroundColor Green
exit 0
