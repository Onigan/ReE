# AI Dev Runbook (Phase C)

## Purpose
Standardize the flow for safe AI-driven development (Codex / Claude Code).

## Fixed Flow
1. Open `README.md` and confirm task intent.
2. Confirm SSOT roots in `SSOT_PATH.txt`.
3. Read SSOT entry files in order:
   - `Docs/SSOT/00_SSOT/SSOT_INDEX.md`
   - `Docs/SSOT/00_SSOT/README_AI.md`
   - `Docs/SSOT/00_SSOT/PROJECT_RULES.md`
4. Create one Packet from `AI_DEV/PACKET_TEMPLATE.md`.
5. Keep changes inside `AI_DEV/TOUCH_LIST.md` allowlist only.
6. Never change `Assets/**` for AI Dev environment tasks.
7. Implement minimal diff for this Packet only.
8. Run `pwsh -File AI_DEV/VERIFY.ps1`.
9. Record verify command, result, and exit code in the Packet.
10. Submit with unified diff and list residual unknowns as `不明`.

## Required Evidence
- `git diff --name-only`
- verify command output
- `echo $LASTEXITCODE`

## Failure Handling
- If verify fails, do not proceed.
- Fix only in-scope files and rerun verify.
- If SSOT conflicts with code, stop and ask.
