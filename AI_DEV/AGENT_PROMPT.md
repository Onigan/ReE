# AGENT_PROMPT — ReE_Alpha01（Codex / Claude Code / ルシェル 共通）

このプロンプトは「AIがReE_Alpha01で作業する時の固定ルール」です。  
毎回、**このAGENT_PROMPT** と **最新のPacket（MakePacketで生成）** をセットで渡してください。

---

## 0) 何を渡されるか（入力）
- ① この `AI_DEV/AGENT_PROMPT.md`
- ② 最新 Packet（例：`AI_DEV/Packets/Packet_YYYYmmdd_HHMMSS_<Title>.md`）

Packetに書かれた Intent / Change Request / Acceptance Criteria / Test Steps を唯一のタスク定義として扱う。

---

## 1) 正本（SSOT）ルール（最優先）
- 正本（SSOT）は `Docs/SSOT/00_SSOT/`。
- SSOTと実装が矛盾している場合は **停止**し、矛盾点を列挙して質問する（勝手に推測で改変しない）。
- `SSOT_PATH.txt` は repo root の基準ポインタ。

---

## 2) 変更範囲（Touch List）
- 変更してよい範囲は `AI_DEV/TOUCH_LIST.md` の Allowed のみ。
- `Assets/**` を含む Denied 範囲は一切変更しない。
- Touch List外に変更が必要なら、理由・代替案を提示して停止する。

---

## 3) 1タスク＝1Packet
- 1回の作業は必ず **1タスク**に限定する。
- 複数タスクに見える場合は、まず「どれを優先するか」を質問して停止。

---

## 4) 出力形式（絶対）
- 出力は **unified diff（パッチ）だけ**。
- 説明文は最小限（diffの前後に短く）。
- diffに含めるファイルは Touch List の Allowed のみ。

---

## 5) 必須の記載（出力に含める）
- Touch List（今回変更したファイル一覧）
- Supersede（上書き対象ファイル）
- Acceptance Criteria を満たした理由
- Test Steps（実行手順）
- 不明点 / リスク（あれば “不明” と明記）

---

## 6) Verify（必須）
- 提案の最後に、必ずこのコマンドを実行した前提で合否を示す：
  - `pwsh -File AI_DEV/VERIFY.ps1`
- FAILになる可能性がある場合は、先に理由を示して停止する。

---

## 7) 不明点の扱い
- 不明点は推測で埋めない。必ず **「不明」** と書き、必要な情報を質問する。

---

## 8) 最低限の作業手順（AIの内部手順）
1) Packetを読む（Intent/AC/Test/Supersedeを抽出）
2) Touch Listを確認
3) 変更案を作る（最小差分）
4) Verifyを通す（PASS前提）
5) unified diffのみを出力