# SSOT_INDEX — ReE_Alpha01（SSOT 本文索引）

このファイルは **SSOT（正本）本文へ入るための索引**です。  
迷ったらここに戻ります。

---

## 0. この索引の使い方（最初の30秒）
1) `PROJECT_RULES.md` を読む（運用の憲法）  
2) `README_AI.md` を読む（AI/エージェントの迷子防止）  
3) **SSOT_PATH を確認**（実体パスの参照先）  
   - repo root：`../../../SSOT_PATH.txt`  
4) 以降、この索引から各SSOT本文へ進む

---

## 1. 正本（SSOT）の場所（確定）
- SSOT Root（正本）：`Docs/SSOT/00_SSOT/`
- 本索引：`Docs/SSOT/00_SSOT/SSOT_INDEX.md`
- 運用ルール：`Docs/SSOT/00_SSOT/PROJECT_RULES.md`
- AIガイド：`Docs/SSOT/00_SSOT/README_AI.md`

---

## 2. SSOT_PATH の意味（重要）
- `../../../SSOT_PATH.txt` は **このリポジトリの SSOT / DRAFT / Specs / Logs の実体パス**を指します。
- SSOT本文は Git 管理下の `Docs/SSOT/00_SSOT/` が正本です。
- SSOT_PATH の内容が古い/矛盾する場合は、まず SSOT_PATH を修正してから運用します。

---

## 3. 優先順位（衝突時の裁定）
1) SSOT（この `Docs/SSOT/00_SSOT/` 配下）  
2) 実装コード（Unity）＝「現状の挙動」  
3) Logs / Specs / Design（補助。正本と矛盾したら正本優先）  
※不明点は推測で埋めない（不明として停止）。

---

## 4. 主要セクション索引

### 4.1 運用・ルール（必読）
- [PROJECT_RULES.md](./PROJECT_RULES.md)
- [README_AI.md](./README_AI.md)

### 4.2 世界設定（WORLD）
> ※WORLD 配下のファイルは今後増える前提。増えたらこの索引に追記します。

- [WORLD/00_WORLD_INDEX_SSOT_v1.2.md](./WORLD/00_WORLD_INDEX_SSOT_v1.2.md)
- [WORLD/MANIFEST_00_WORLD_INDEX_OVERWRITE_v1.2.md](./WORLD/MANIFEST_00_WORLD_INDEX_OVERWRITE_v1.2.md)
- [WORLD/WORLD_SSOT_MD_OVERWRITE_README_2026-01-25.md](./WORLD/WORLD_SSOT_MD_OVERWRITE_README_2026-01-25.md)

---

## 5. 追加予定（保留フラグ付き）
以下は “SSOTとして今後追加・整備する可能性が高い” ものです。  
現時点でファイルが無い場合は **保留**として扱います。

- 【保留】戦闘仕様 SSOT（TimeCore / UI / 状態遷移）
- 【保留】スキル体系 SSOT
- 【保留】アイテム / 装備 SSOT
- 【保留】ダンジョン探索 SSOT
- 【保留】AI連携（World Writer / GM / Unity I/F）SSOT

---

## 6. Legacy（参照禁止の旧導線）
- `Docs/SSOT/00_SSOT/00_SSOT/` のような二重パスは legacy（参照しない）
- `/00_SSOT` `/Logs_SSOT` のような **リポジトリ直下前提の記述は誤り**  
  → 必ず `Docs/SSOT/00_SSOT/` / `Docs/Logs_SSOT/` を使う

---