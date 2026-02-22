param(
  [string]$ExpectedBranch = "dev"
)

$ErrorActionPreference = "Stop"
$fail = $false

function OK([string]$m) { Write-Host "[OK] $m" }
function NG([string]$m) { Write-Host "[NG] $m"; $script:fail = $true }

# --- branch check ---
try {
  $branch = (git rev-parse --abbrev-ref HEAD).Trim()
  if ($branch -eq $ExpectedBranch) { OK "branch is $ExpectedBranch" }
  else { NG "branch is not $ExpectedBranch (current: $branch)" }
} catch {
  NG "git is not available or not a git repo here"
}

# --- required pointers (existence check) ---
$required = @(
  "AI_DEV/TOUCH_LIST.md",
  "AI_DEV/PROMPT_BASE.md",
  "SSOT_PATH.txt",
  "Docs/SSOT/00_SSOT/SSOT_INDEX.md",
  "Docs/SSOT/00_SSOT/PROJECT_RULES.md",
  "Docs/SSOT/00_SSOT/README_AI.md"
)

foreach ($f in $required) {
  if (Test-Path $f) { OK "$f exists" } else { NG "$f missing" }
}

# --- collect changed files (staged + unstaged) ---
$changed = @()
try { $changed += git diff --name-only } catch {}
try { $changed += git diff --cached --name-only } catch {}

$changed = $changed |
  Where-Object { $_ -and $_.Trim() -ne "" } |
  ForEach-Object { $_.Trim().Replace("\","/") } |
  Sort-Object -Unique

if ($changed.Count -eq 0) {
  OK "no changed files (clean)"
} else {
  OK ("changed files: {0}" -f $changed.Count)
}

# --- touch list rules ---
$allowedExact = @(
  "README.md",
  "README_DEV.md",
  "AGENTS.md",
  "SSOT_PATH.txt"
)

$allowedPrefixes = @(
  "AI_DEV/",
  "Docs/SSOT/00_SSOT/"
)

$deniedPrefixes = @(
  "Assets/",
  "Packages/",
  "ProjectSettings/",
  "Library/",
  "Logs/",
  "Temp/",
  "UserSettings/",
  "Build/",
  ".vs/",
  ".vscode/"
)

# extra deny patterns (Unity assets)
function Is-DeniedByPattern([string]$p) {
  return ($p.EndsWith(".unity") -or $p.EndsWith(".prefab") -or $p.EndsWith(".asset"))
}

foreach ($p in $changed) {
  # denied prefix
  foreach ($d in $deniedPrefixes) {
    if ($p.StartsWith($d)) { NG "DENIED path changed: $p"; continue }
  }
  if (Is-DeniedByPattern $p) {
    NG "DENIED file type changed: $p"
    continue
  }

  # allowed exact
  if ($allowedExact -contains $p) { continue }

  # allowed prefix
  $isAllowed = $false
  foreach ($a in $allowedPrefixes) {
    if ($p.StartsWith($a)) { $isAllowed = $true; break }
  }
  if (-not $isAllowed) {
    NG "OUTSIDE Touch List: $p"
  }
}

if ($fail) {
  Write-Host "VERIFY RESULT: FAIL"
  exit 1
}

Write-Host "VERIFY RESULT: PASS"
exit 0