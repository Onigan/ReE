# ReE_Alpha01（Unity） — 入口 / SSOT（正本）導線

このリポジトリは **ReE_Alpha01（Unityプロジェクト）** の開発用リポジトリです。  
設計・世界観・運用ルールは、リポジトリ内の **SSOT（Single Source of Truth / 正本）** を根拠にします。

---

## 最初の5分（初心者チェック）
1) 作業ツリー確認  
- `git status -sb` → **clean** であること（汚れていたら作業開始前に整理）

2) 正本（SSOT）を開く  
- **SSOT入口**：`Docs/SSOT/00_SSOT/SSOT_INDEX.md`

3) 1タスクを決める（小さく安全に）  
- 原則 **1タスク＝1Packet**（内容・受け入れ条件・テスト手順を明文化）

---

## 最重要：SSOT優先順位（迷子防止）
**確定（正）**
- `Docs/SSOT/00_SSOT/` 配下（ここが正本）

**参考（補助）**
- `Docs/SSOT/Design/`（設計の補助。正本と矛盾したら正本を優先）
- `Docs/Specs/`（仕様の置き場。SSOTに統合するまでは仕様メモ扱い）

**禁止（混入防止）**
- `Docs/DRAFT/` は **gitignore 対象**（草案は原則Gitに入れない）

---

## 入口はここ（README.md）だけにする方針
README/INDEX が複数あっても削除・統合を急ぎません。  
今は「入口（このREADME）を確定」→「リンク導線を統一」→「不要になったら整理」が安全です。

---

## 主要リンク（ここから迷わず辿れるようにする）
### SSOT（正本）
- SSOT索引（本文入口）：`Docs/SSOT/00_SSOT/SSOT_INDEX.md`
- SSOT運用ルール：`Docs/SSOT/00_SSOT/PROJECT_RULES.md`
- AI向けSSOT読み方：`Docs/SSOT/00_SSOT/README_AI.md`

### AIへ依頼する（Packet運用）
- AI実務入口：`AI_DEV/README.md`
- AI向けSSOT入口（補助）：`AI_DEV/SSOT_INDEX.md`

### 仕様・ログ置き場
- 仕様メモ：`Docs/Specs/`
- SSOTログ：`Docs/Logs_SSOT/`

### 開発環境（Unity起動・実行）
- 開発手順：`README_DEV.md`

### エージェント向け最小規約（Codex/Claude等）
- `AGENTS.md`

---

## 開発ルール（短縮版）
- 作業ブランチは **dev**（必要に応じてPR/レビュー）
- **1タスク＝1Packet**（目的 / 変更点 / 受け入れ条件 / テスト手順）
- 不明点は推測で埋めない（SSOTにないなら「未定義」として扱う）
- 大きなファイル混入（例：100MB級）に注意（事故防止）

---

## 困ったとき（貼ると早い情報）
最低限これを貼ると原因特定が速いです：
- `git status -sb`
- 変更したファイルの一覧（コミット前なら `git diff --name-only`）
- Unity Console の該当ログ（エラーは全文、警告は関連箇所）

---

## 役割分担（README/INDEXが複数ある理由）
- `README.md`：**唯一の入口**（このページ）
- `Docs/SSOT/00_SSOT/SSOT_INDEX.md`：**正本の索引（本文入口）**
- `Docs/SSOT/00_SSOT/README_AI.md`：**AIが迷子にならない読み方**
- `AI_DEV/README.md`：**AIに投げる実務（Packet作成・依頼方法）**
- `AGENTS.md`：**外部エージェント向け超短い規約**
- `README_DEV.md`：**Unity実行・開発環境**

削除・統合は「導線が安定してから」行います。
