# Packet Template

## Packet Name
Packet_<PROJECT>_<TOPIC>_<YYYY-MM-DD>

## Goal
- 何をできるようにするか（1つだけ）

## Scope
- 変更するもの:
  - file1
  - file2
- 変更しないもの:
  - Assets/**
  - fileX（触らない宣言）

## Supersede Mark（上書き宣言）
- このPacketは <対象> を上書きする:
  - <file path>

## Constraints
- SSOT root: Docs/SSOT/00_SSOT/
- 1 task = 1 Packet
- Output: unified diff only
- Touch List: AI_DEV/TOUCH_LIST.md を遵守
- 例外や未知は「不明」として質問

## Implementation Plan (AI writes)
1.
2.
3.

## Acceptance Criteria（受け入れ条件）
- [ ] 条件1
- [ ] 条件2

## Test Steps（テスト手順）
1.
2.
3.

## Verify（必須）
- Command: `pwsh -File AI_DEV/VERIFY.ps1`
- Result: PASS / FAIL
- Exit code: <0 or 1>

## Notes / Risks
- 想定リスクと回避策
