param(
  [string]$Title = "AI_Task",
  [string]$OutDir = "AI_DEV/Packets"
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

# --- snapshots ---
$branchLines    = Run-Git @("rev-parse","--abbrev-ref","HEAD")
$statusLines    = Run-Git @("status","-sb")
$lastCommitLines= Run-Git @("log","-1","--oneline")
$changedLines   = Run-Git @("diff","--name-only")

$branch     = ($branchLines | Select-Object -First 1)
$statusText = ($statusLines -join "`r`n")
$lastCommit = ($lastCommitLines | Select-Object -First 1)
$changedText= ($changedLines -join "`r`n")

# --- SSOT pointers (existence check) ---
$paths = @(
  "README.md",
  "SSOT_PATH.txt",
  "AGENTS.md",
  "AI_DEV/README.md",
  "AI_DEV/SSOT_INDEX.md",
  "Docs/SSOT/00_SSOT/SSOT_INDEX.md",
  "Docs/SSOT/00_SSOT/PROJECT_RULES.md",
  "Docs/SSOT/00_SSOT/README_AI.md"
)

$existsLines = $paths | ForEach-Object {
  "{0} : {1}" -f $_, (Test-Path $_)
}
$existsText = ($existsLines -join "`r`n")

# --- content (HERE-STRING: safe) ---
$content = @"
# Packet_${ts}: $Title

## 0) Intent・井ｽ輔ｒ縺励◆縺・°・・
- 

## 1) Current State・育樟迥ｶ・・
- Branch: $branch
- Last commit: $lastCommit

### git status -sb
\`\`\`
$statusText
\`\`\`

### Changed files (git diff --name-only)
\`\`\`
$changedText
\`\`\`

## 2) Source of Truth・域ｭ｣譛ｬ・・
- SSOT root: Docs/SSOT/00_SSOT/
- SSOT entry: Docs/SSOT/00_SSOT/SSOT_INDEX.md
- Rules: Docs/SSOT/00_SSOT/PROJECT_RULES.md
- AI guide: Docs/SSOT/00_SSOT/README_AI.md
- SSOT_PATH: ./SSOT_PATH.txt (repo root)

### Pointer existence check
\`\`\`
$existsText
\`\`\`

## 3) Change Request・亥､画峩隕∵ｱゑｼ・
- What:
- Why:
- Scope (Supersede):
  - Files to change:
  - Files NOT to change:

## 4) Acceptance Criteria・亥女縺大・繧梧擅莉ｶ・・
- [ ] 

## 5) Test Steps・医ユ繧ｹ繝域焔鬆・ｼ・
1) 

## 6) Evidence・郁ｨｼ霍｡・・
- Console logs:
- Screenshots:
"@

# Write UTF-8
$content | Set-Content -Encoding utf8 $outPath
Write-Host "Created: $outPath"
