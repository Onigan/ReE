# SSOT Index (AI Entry)

## 正本(SSOT)の場所
- 正本は `SSOT_PATH.txt` を参照する。
- 作業開始時に `SSOT_PATH.txt` を確認し、記載された SSOT / DRAFT / Specs / Logs のルートを基準にする。

## 優先参照順
1. SSOT（正本）
2. 実装コード（現在のリポジトリ）
3. メモ / ログ（補助情報）

## 更新ルール
- 1タスク = 1Packet。
- 変更は `dev` ブランチへ。
- `main` は安定版のみ反映する。

## 危険操作
- `git clean -fd` は単独実行禁止。
- 実行する場合は必ず `git clean -nd` の結果をレビューしてから実行する。

## AIに渡す手順
- `AI_DEV/PACKET_TEMPLATE.md` に従って Packet を作成する。
- 依頼文には受け入れ条件（Acceptance Criteria）とテスト手順（Test Steps）を必ず含める。
