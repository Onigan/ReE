# PROMPT_BASE — Codex / Claude Code（固定ルール）

あなたはこのリポジトリで作業するAIです。以下のルールを必ず守ってください。

---

## 0) 最優先ルール（SSOT）
- 正本（SSOT）は `Docs/SSOT/00_SSOT/` 配下。
- SSOTと実装が矛盾している場合は **停止**し、矛盾点を列挙して質問する（勝手に推測で改変しない）。
- `SSOT_PATH.txt` は repo root の基準ポインタ。

---

## 1) ガードレール（Touch List）
- 変更してよい範囲は `AI_DEV/TOUCH_LIST.md` の Allowed のみ。
- `Assets/**` を含む Denied 範囲は一切変更しない。
- Touch List 外に変更が必要なら、理由と代替案を提示して停止する。

---

## 2) 1タスク＝1Packet
- 1回の作業は必ず **1タスク**に限定。
- 出力に必ず含める：
  - Intent（目的）
  - Supersede（上書き対象ファイル）
  - Acceptance Criteria（受け入れ条件）
  - Test Steps（テスト手順）

---

## 3) 出力形式（重要）
- 出力は **unified diff（パッチ）だけ**。
- 説明文は最小限（diffの前後に長文を書かない）。
- diffに含めるファイルは Touch List の Allowed のみ。

---

## 4) 検証（必須）
- 提案の最後に、必ずこのコマンドを実行した前提で合否を示す：
  - `pwsh -File AI_DEV/VERIFY.ps1`
- FAIL になる可能性があるなら、先に理由を示して停止する。

---

## 5) 不明点の扱い
- 不明点は推測で埋めない。必ず **「不明」** と書き、必要な情報を質問する。