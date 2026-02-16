\# AI Dev Rules (ReE\_Alpha01)



目的:

\- 実装は AI（Codex / Claude）に依頼し、人間は「要求→依頼文→取り込み→テスト→差分報告」を回す。

\- GitHub はバックアップ/履歴。作業は dev ブランチで行い、main は安定版のみ。



基本ルール:

1\. 変更は必ず dev ブランチで行う（main 直コミット禁止）

2\. 1タスク=1Packet（小さく・検証可能に）

3\. 変更対象ファイルを明示（Supersede Mark）

4\. 受け入れ条件（テスト手順/成功条件）を必ず書く

5\. 不明点は推測で埋めず「不明」として質問する



作業フロー:

\- Human: Packet を作成 → AI に渡す

\- AI: Plan-First → 実装 → 変更点説明 → テスト手順提示

\- Human: Unityでテスト → 結果ログ/スクショ共有 → 修正依頼（必要なら）



ブランチ運用:

\- 日常作業: dev

\- 安定版: main（dev で動作確認できたものだけを反映）



---

## Packet_001: 100MB事故防止ルール（必須・固定）

### 目的
Unity開発で起きがちな「巨大ファイル混入」による GitHub 側ブロック（単一オブジェクト 100MB 制限）を、
**ローカル段階で確実に止める**。  
GitHubは単一オブジェクトが 100MB を超えると push をブロックする（警告は 50MiB 付近で出る）。  
大容量が必要な場合は Git LFS 等の別手段を検討する。  
※制限の根拠：GitHub公式ドキュメント参照。

---

### ルール（絶対）
1) **100MB以上のファイルは “Gitに入れない”**（例外は別途合意した場合のみ）  
2) **commit 前に staged を必ず確認**：`git diff --cached`  
3) **開発は dev ブランチで 1タスク=1Packet**（関係ない変更を混ぜない）  
4) **事故対応（履歴改変・強制push等）は単独判断しない**（必ず合意してから）

---

### 1回だけ：新PCセットアップ（各PC必須）
`.githooks/pre-commit` を置くだけでは動かない。clone先PCごとに設定が必要。

```bash
git config core.hooksPath .githooks
git config --get core.hooksPath
```

- ⚠ Unityプロジェクトでは `git clean -fd` を単独実行しない（未追跡のAssets等を大量削除する危険がある）。実行する場合は必ず `git clean -nd` の出力を共有して合意してから。

## SSOT Entry
- SSOTの入口: `AI_DEV/SSOT_INDEX.md`

## 日常フロー（作業者向け）
1. Packetを決める
2. AIに依頼
3. ローカルテスト
4. commit
5. push（Actions確認）