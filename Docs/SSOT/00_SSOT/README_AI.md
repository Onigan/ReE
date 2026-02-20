# README_AI — SSOT（正本）読み方ガイド（AI/エージェント用）

## 0. 目的
このファイルは、AI/エージェントが ReE_Alpha01 の正本（SSOT）を誤参照せず、
**1タスク＝1Packet** で安全に作業するための最短導線です。

---

## 1. 正本（SSOT）の場所（最優先）
**Canonical SSOT root（正本ルート）**
- `Docs/SSOT/00_SSOT/`

**正本の入口（索引）**
- `Docs/SSOT/00_SSOT/SSOT_INDEX.md`

**運用ルール（最優先で遵守）**
- `Docs/SSOT/00_SSOT/PROJECT_RULES.md`

迷ったら：
`README.md` → `Docs/SSOT/00_SSOT/SSOT_INDEX.md` の順に戻る。

---

## 2. SSOT優先順位（衝突時の裁定）
1) **SSOT（正本）**：`Docs/SSOT/00_SSOT/` が常に正  
2) **Code（Unity実装）**：現状挙動。設計と矛盾したら差分として記録する  
3) **Specs / Design（補助）**：参考。正本と矛盾したら正本を優先  
4) **Docs/DRAFT/**：草案。原則 Git に入れない（混入防止）

---

## 3. 作業開始チェック（最初の30秒）
1) `git status -sb` が clean か確認  
2) `Docs/SSOT/00_SSOT/SSOT_INDEX.md` を開く  
3) repo root の `SSOT_PATH.txt` を確認（SSOT / DRAFT / Specs / Logs の基準ルート）
4) **1タスク＝1Packet** に分割して着手（複数タスク同時禁止）

---

## 4. AIに依頼する場合（Packet導線）
- AI実務入口：`AI_DEV/README.md`
- AI向けSSOT入口（補助）：`AI_DEV/SSOT_INDEX.md`

依頼文には必ず入れる：
- 目的 / 変更点 / 受け入れ条件 / テスト手順 / 影響範囲（Supersede）

---

## 5. Legacy（参照禁止の旧導線）
- 旧 “mirror” パス：`Docs/SSOT/00_SSOT/00_SSOT/` は **legacy**。参照しない。
- ルート直下 `/00_SSOT` `/Logs_SSOT` を前提にした記述が残っていたら、必ず `Docs/...` 配下へ修正する。

---