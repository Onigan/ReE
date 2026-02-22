param(
  [string]$Title = "AI_Task",
  [string]$OutDir = "AI_DEV/Packets",
  [switch]$SkipVerify
)

# --- helpers ---
function Run-Git {
  param([string[]]$Args)
  try {
    $out = & git @Args 2>$null
    if ($null -eq $out) { return @() }
    if ($out -is [string]) { return @($out) }
    return $out
  } catch {
    return @()
  }
}

# --- paths ---
$ts = Get-Date -Format "yyyyMMdd_HHmmss"
$safeTitle = ($Title -replace '[\\/:*?"<>| ]','_')
New-Item -ItemType Directory -Force $OutDir | Out-Null
$outPath = Join-Path $OutDir ("Packet_{0}_{1}.md" -f $ts, $safeTitle)

# --- snapshots (git) ---
$branchLines     = Run-Git @("rev-parse","--abbrev-ref","HEAD")
$statusLines     = Run-Git @("status","-sb")
$lastCommitLines = Run-Git @("log","-1","--oneline")
$changedUnstaged = Run-Git @("diff","--name-only")
$changedStaged   = Run-Git @("diff","--cached","--name-only")

$branch      = ($branchLines | Select-Object -First 1)
$statusText  = ($statusLines -join "`r`n")
$lastCommit  = ($lastCommitLines | Select-Object -First 1)
$unstagedTxt = ($changedUnstaged -join "`r`n")
$stagedTxt   = ($changedStaged -join "`r`n")

# --- SSOT pointers (existence check) ---
$paths = @(
  "README.md",
  "SSOT_PATH.txt",
  "AGENTS.md",
  "AI_DEV/README.md",
  "AI_DEV/SSOT_INDEX.md",
  "Docs/SSOT/00_SSOT/SSOT_INDEX.md",
  "Docs/SSOT/00_SSOT/PROJECT_RULES.md",
  "Docs/SSOT/00_SSOT/README_AI.md",
  "AI_DEV/TOUCH_LIST.md",
  "AI_DEV/PROMPT_BASE.md",
  "AI_DEV/VERIFY.ps1"
)

$existsLines = $paths | ForEach-Object { "{0} : {1}" -f $_, (Test-Path $_) }
$existsText = ($existsLines -join "`r`n")

# --- VERIFY (run and capture) ---
$verifyOutText = "(skipped)"
$verifyExit = "N/A"
if (-not $SkipVerify) {
  if (Test-Path "AI_DEV/VERIFY.ps1") {
    $verifyLines = & pwsh -NoProfile -File "AI_DEV/VERIFY.ps1" 2>&1
    $verifyExit = $LASTEXITCODE
    $verifyOutText = ($verifyLines -join "`r`n")
  } else {
    $verifyOutText = "AI_DEV/VERIFY.ps1 not found"
    $verifyExit = "N/A"
  }
}

# --- content (HERE-STRING: safe) ---
$content = @"
# Packet_${ts}: $Title

## 0) Intent（何をしたいか）
- 

## 1) Current State（現状）
- Branch: $branch
- Last commit: $lastCommit

### git status -sb
\`\`\`
$statusText
\`\`\`

### Changed files (staged)
\`\`\`
$stagedTxt
\`\`\`

### Changed files (unstaged)
\`\`\`
$unstagedTxt
\`\`\`

## 2) Source of Truth（正本）
- SSOT root: Docs/SSOT/00_SSOT/
- SSOT entry: Docs/SSOT/00_SSOT/SSOT_INDEX.md
- Rules: Docs/SSOT/00_SSOT/PROJECT_RULES.md
- AI guide: Docs/SSOT/00_SSOT/README_AI.md
- SSOT_PATH: ./SSOT_PATH.txt (repo root)

### Pointer existence check
\`\`\`
$existsText
\`\`\`

## 3) Change Request（変更要求）
- What:
- Why:
- Scope (Supersede):
  - Files to change:
  - Files NOT to change:

## 4) Acceptance Criteria（受け入れ条件）
- [ ] 

## 5) Test Steps（テスト手順）
1) 

## 6) Verify（自動）
- VERIFY EXIT CODE: $verifyExit

\`\`\`
$verifyOutText
\`\`\`

## 7) Evidence（証跡）
- Console logs:
- Screenshots:
"@

$content | Set-Content -Encoding utf8 $outPath
Write-Host "Created: $outPath"as