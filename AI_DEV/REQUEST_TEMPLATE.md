# AI Request Template (Codex / Claude Code)

## 0) Purpose
- What to achieve in one sentence.

## 1) Scope
- Files to change:
  - <path>
- Files not to change:
  - Assets/**
  - <other blocked paths if needed>

## 2) Constraints
- SSOT first: `Docs/SSOT/00_SSOT/`
- 1 task = 1 Packet
- Unknowns must be marked as `不明`
- Output must be unified diff only
- Changes must stay inside `AI_DEV/TOUCH_LIST.md`

## 3) Acceptance Criteria
- [ ] <criterion 1>
- [ ] <criterion 2>

## 4) Test Steps
1. <step 1>
2. <step 2>
3. Run: `pwsh -File AI_DEV/VERIFY.ps1`

## 5) Supersede
- <target file path>

## 6) Required Output Format
1. Touch List used
2. Unified diff patch
3. Verify result (PASS/FAIL + exit code)
4. Residual risks / unknowns (`不明`)
