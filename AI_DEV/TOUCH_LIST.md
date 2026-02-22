# AI_DEV Touch List（AIが変更してよい範囲）

このファイルは **Codex / Claude Code / ルシェル** が変更してよい範囲を固定し、
事故（勝手な改変・関係ないファイル変更）を防ぐためのガードレールです。

---

## Allowed（変更OK）
- AI_DEV/**
- Docs/SSOT/00_SSOT/**
- README.md
- README_DEV.md
- AGENTS.md
- SSOT_PATH.txt

---

## Denied（変更NG）
- Assets/**
- Packages/**
- ProjectSettings/**
- Library/**
- Logs/**
- Temp/**
- UserSettings/**
- Build/**
- .vs/**
- .vscode/**
- **/*.unity
- **/*.prefab
- **/*.asset

---

## Gate（停止条件）
- Allowed 以外に変更が1つでもある → **FAIL**
- Denied に該当する変更が1つでもある → **FAIL**
- SSOT（Docs/SSOT/00_SSOT/**）と実装が矛盾している → **STOP（質問）**
- 不明点は推測で埋めない → **「不明」** と明記して停止

---

## Notes（運用）
- 原則「1タスク＝1Packet」。
- Touch List を変更する場合は、必ず **Packetで理由・受け入れ条件・テスト手順**を明記してから行う。