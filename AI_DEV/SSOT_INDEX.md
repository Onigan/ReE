# SSOT_INDEX (AI/Agent Entry)

このファイルは **AI/エージェントが迷子にならないための最短入口**です。  
設計本文（SSOT）は `Docs/SSOT/00_SSOT/` にあります。

---

## 0. 最初の30秒（必須）
1) `git status -sb` が clean か確認  
2) **SSOT_PATH** を確認（repo root）  
   - ここから参照：`../SSOT_PATH.txt`  
3) SSOT本文入口へ移動：`../Docs/SSOT/00_SSOT/SSOT_INDEX.md`

---

## 1. 正本（SSOT）の場所（確定）
- SSOT Root（正本）：`../Docs/SSOT/00_SSOT/`
- SSOT 本文入口（索引）：`../Docs/SSOT/00_SSOT/SSOT_INDEX.md`
- 運用ルール：`../Docs/SSOT/00_SSOT/PROJECT_RULES.md`
- AIガイド：`../Docs/SSOT/00_SSOT/README_AI.md`

---

## 2. SSOT_PATH の意味（重要）
- `../SSOT_PATH.txt` は **このリポジトリの SSOT / DRAFT / Specs / Logs の「実体パス」**を指します。
- SSOT は Git 管理下にある `Docs/` 配下が正本です。  
  SSOT_PATH の内容が古い/矛盾している場合は **先に SSOT_PATH を修正**してから作業します。

---

## 3. 優先順位（衝突時）
1) SSOT（`Docs/SSOT/00_SSOT/`）  
2) 実装コード（Unity）＝「現状の挙動」  
3) Logs / Specs / Design（補助情報）  
※不明点は推測で埋めず「不明」として停止し、必要情報の提示を求める。

---

## 4. 作業ルール（最小）
- ブランチ：`dev`
- **1タスク = 1Packet**
- 依頼テンプレ：`./PACKET_TEMPLATE.md`
- 依頼文には必ず：
  - Acceptance Criteria（受け入れ条件）
  - Test Steps（テスト手順）
  - 変更対象ファイル（Supersede Mark）

---

## 5. 禁止・注意
- `Docs/DRAFT/` は草案（gitignore）。SSOTとして扱わない。
- `/00_SSOT` `/Logs_SSOT` のような **ルート直下前提の記述は誤り**（必ず `Docs/...` を使う）。
- `git clean -fd` 単独実行は禁止（必要なら `git clean -nd` を先にレビュー）。

---