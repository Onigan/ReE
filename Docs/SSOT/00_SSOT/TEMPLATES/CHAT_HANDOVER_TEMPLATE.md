# ReE CHAT HANDOVER TEMPLATE v0.1.9

## 0. プロジェクト宣言（必須）
これは **ReE / Chronos Echo** プロジェクトです。  
PROJECT_RULES.md（v0.1.9 制定）を最上位ルールとして適用してください。

---

## 1. 現在のフェーズ
- フェーズ：ReE α1.0
- 対象：戦闘システム（TimeCore Unit-B）
- 状態：仕様整理完了 → 実装フェーズ開始

---

## 2. SSOT 概要（30秒要約）
- TimeCore（UT管理・2段階解決）と戦闘UI基盤は **Unity実装済み**
- 以下の設計が **公式（未実装B）として確定**
  - Effect System MVP（Damage / Heal）
  - 宣言的Effect構造（Intent / Runner / Event）
  - 多段乗算ダメージモデル
  - 3層構造ログエンジン（Facts / Interpretation / Narration）
  - 7属性体系（無属性含む）
  - 0.1m空間定義（距離・座標・射程）

---

## 3. 今回の作業目的（明示）
（例）
- Effect System MVP のクラス設計確定
- EffectRunner の責務分離確認
- 実装順序の裁定

---

## 4. 参照すべき公式ファイル
- 00_SSOT/PROJECT_RULES.md
- 00_SSOT/MANIFEST.md
- 00_SSOT/CHANGELOG.md（v0.1.9）
- 00_SSOT/HANDOVER.md（最新版）

---

## 5. 注意事項（重要）
- SSOT未記載の案はすべて Draft 扱い
- 公式化には YES/NO 裁定が必要
- 実装前に必ず整合性確認を行うこと

---

## 6. ノアへの指示
- PROJECT_RULES.md を前提として回答すること
- 仕様の先祖返りを起こさないこと
- 不明点は必ず「裁定待ち」と明示すること
