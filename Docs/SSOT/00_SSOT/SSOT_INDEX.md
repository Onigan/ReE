# SSOT_INDEX — ReE_Alpha01（正本SSOT）本文索引

このファイルは **正本（SSOT）へ入るための索引（本文入口）**です。  
迷ったら `README.md` → この `SSOT_INDEX.md` → `README_AI.md` の順に戻ってください。

---

## 0) この索引の使い方（最初の30秒）
1) `PROJECT_RULES.md` を読む（運用ルール）
2) `README_AI.md` を読む（AI/エージェント向け誤参照防止）
3) repo root の `SSOT_PATH.txt` を確認（SSOT / DRAFT / Specs / Logs の基準パス）
4) 必要な章（WORLD / SYSTEM 等）へ進む

---

## 1) 正本（SSOT）の場所（固定）
- **SSOT Root（正本）**：`Docs/SSOT/00_SSOT/`
- **本文入口（索引）**：`Docs/SSOT/00_SSOT/SSOT_INDEX.md`
- **運用ルール**：`Docs/SSOT/00_SSOT/PROJECT_RULES.md`
- **AI読み方ガイド**：`Docs/SSOT/00_SSOT/README_AI.md`

---

## 2) 優先参照順位（衝突時の裁定）
1) **SSOT（Docs/SSOT/00_SSOT/）が正**
2) Code（Unity実装）は「現状の挙動」  
   - SSOTと矛盾する場合は差分として記録し、裁定してSSOTに反映する
3) Specs / Design は補助（SSOTに反映されたらSSOT優先）
4) `Docs/DRAFT/` は草案（原則Gitに入れない／混入防止）

---

## 3) WORLD（世界観・統合）
- `WORLD/00_WORLD_INDEX_SSOT_v1.2.md`
- `WORLD/MANIFEST_00_WORLD_INDEX_OVERWRITE_v1.2.md`
- `WORLD/WORLD_SSOT_MD_OVERWRITE_README_2026-01-25.md`

※WORLD配下にSSOTが増えたら、このセクションへリンクを追加する。

---

## 4) SYSTEM / DESIGN / BATTLE（※未整備・追加予定）
- ここは「SSOTとして確定したファイル」が揃い次第、索引を追加する。
- 追加したら、必ずこの `SSOT_INDEX.md` にリンクを足す（入口を増やさない）。

---

## 5) 周辺フォルダ（SSOTが参照する）
- Specs：`Docs/Specs/`
- Dev Logs（SSOT側）：`Docs/Logs_SSOT/`
- Draft：`Docs/DRAFT/`（原則Git管理外）
- AI入口：`AI_DEV/SSOT_INDEX.md`

---

## 6) Legacy（参照禁止の旧導線）
- `Docs/SSOT/00_SSOT/00_SSOT/` のような **二重00_SSOT** は legacy（参照しない）
- `/00_SSOT` `/Logs_SSOT` を repo直下に置く前提の記述が残っていたら、**Docs配下に修正**