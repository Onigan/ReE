# SSOT Update PR Instruction Template（AI用）
（SSOT更新PRをAIが作るための指示書テンプレ）

## 0) Packet情報（必須）
- Packet名: Packet_<NNN>: <短いタイトル>
- 対象ブランチ:
  - 作業ブランチ: dev から作る
  - PRのbase: main（安定版）
- 1タスク=1Packet（このPacketに無関係な変更は混ぜない）

## 1) SSOTの正（Source of Truth）
- SSOTルートは SSOT_PATH.txt の参照先。
- 優先順位は **SSOT > コード > メモ**。
- 不明点や矛盾がある場合は「推測で確定」せず質問して止める。

## 2) 目的（What / Why）
- 何をSSOTに反映するのか（1〜3行で）
- 何が嬉しいのか（参照性/手戻り防止/運用安定など）

## 3) 入力（今回AIが参照する素材）
- 参照するフォルダ/ファイル（相対パスで列挙）
  例:
  - 00_SSOT/...
  - 01_DRAFT/...
  - Specs/...
  - README.md / SSOT_INDEX.md / SSOT_PATH.txt

## 4) 変更対象（Deliverables）
### 4.1 変更するファイル一覧（必須）
- [ADD]    00_SSOT/<path>/<file>.md
- [MODIFY] 00_SSOT/<path>/<file>.md
- [MOVE]   01_DRAFT/<...> -> 00_SSOT/<...>（必要な場合のみ）

### 4.2 各ファイルの変更要約（必須）
- <file>: 何をどう変えるか（箇条書きで簡潔に）

## 5) 禁止事項（絶対）
- git clean -fd を単独で実行しない
- 履歴改変（rebase / reset --hard / force push）をしない
- 無関係な整形・リネーム・大改造を混ぜない
- SSOTの裁定事項を推測で上書きしない（矛盾は質問）

## 6) 受入条件（Acceptance Criteria）
- [ ] SSOT_INDEX.md（または目次相当）から新規/更新資料に到達できる
- [ ] リンク切れがない（相対パスの存在確認）
- [ ] 章番号・用語が統一されている（勝手に新語を増やさない）
- [ ] 差分が「このPacketの範囲だけ」になっている
- [ ] GitHub Actions（Large File Guard等）が緑

## 7) 手順（AIが必ず実施して報告する）
### 7.1 作業ブランチ作成
- `git checkout dev`
- `git pull`
- `git checkout -b packet/<NNN>-<short-title>`

### 7.2 変更作業
- 指定ファイルを追加/更新
- 必要ならSSOT_INDEX.mdへ導線追加

### 7.3 ローカル検証
- `git status` が意図通り（余計な変更がない）
- 変更ファイル一覧を `git diff --name-only` で確認
- リンク先ファイルの存在確認（最低限）

### 7.4 コミット
- コミットメッセージ: `Packet_<NNN>: <title>`
- `git commit -m "Packet_<NNN>: <title>"`

### 7.5 push
- `git push -u origin packet/<NNN>-<short-title>`

### 7.6 PR作成（必須）
- GitHubでPRを作る
  - base: main
  - compare: packet/<NNN>-<short-title>
- PR本文に以下を貼る（最低限）
  - 目的
  - 変更ファイル一覧
  - 変更要約
  - 受入条件
  - 検証結果（Actionsが緑か）

## 8) AIの最終報告フォーマット（必須）
- ブランチ名:
- コミットID:
- 変更ファイル一覧:
- 主要差分の要約:
- 検証結果:
  - Actions: 成功/失敗（URL）
- 残タスク/質問（あれば）:
