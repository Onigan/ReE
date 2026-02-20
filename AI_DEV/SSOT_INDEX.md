# SSOT Index (AI Entry) — ReE_Alpha01

AI/エージェントが **SSOTを誤参照しない** ための最短入口です。  
**本文索引（正本）は** `Docs/SSOT/00_SSOT/SSOT_INDEX.md` を参照してください。

---

## 0) Preflight（必須）
1) `git status -sb` が clean か確認  
2) repo root の `SSOT_PATH.txt` を読む（SSOT / DRAFT / Specs / Logs のルート確認）  
3) **1タスク＝1Packet**（混線防止）

---

## 1) 正本(SSOT)の場所
- 正本は **repo root の `SSOT_PATH.txt`** を参照する  
- repo内運用のデフォルト想定：
  - SSOT：`Docs/SSOT/00_SSOT/`
  - Logs：`Docs/Logs_SSOT/`
  - Specs：`Docs/Specs/`
  - Draft：`Docs/DRAFT/`（原則Gitに入れない）

---

## 2) SSOTの入口（読む順）
1) `README.md`（唯一の入口）  
2) SSOT索引：`Docs/SSOT/00_SSOT/SSOT_INDEX.md`（本文入口）  
3) AI読み方ガイド：`Docs/SSOT/00_SSOT/README_AI.md`  
4) 運用ルール：`Docs/SSOT/00_SSOT/PROJECT_RULES.md`

---

## 3) 優先参照順（衝突時）
1) SSOT（正本）
2) Code（現状の挙動）※矛盾は差分として記録して裁定
3) Specs / Design（補助）
4) Draft（草案・原則Git管理外）

---

## 4) AIに依頼する時（Packet）
- `AI_DEV/PACKET_TEMPLATE.md` に従う  
- 依頼文に必ず入れる：
  - 目的 / 変更点
  - 受け入れ条件（Acceptance Criteria）
  - テスト手順（Test Steps）
  - 影響範囲（Supersede Mark：どのファイルを上書きするか）

---

## 5) Legacy（参照禁止）
- 旧 “mirror” パス：`Docs/SSOT/00_SSOT/00_SSOT/` は **legacy**。参照しない。
- `/00_SSOT` `/Logs_SSOT` を repo直下に置く前提の記述は **Docs配下へ修正**。
- `git clean -fd` は単独実行禁止（必ず `git clean -nd` で事前レビュー）