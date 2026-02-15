# ReE開発 正本目次（Canvas統合インデックス）

## 0. このCanvasの役割
- このCanvasは **「正本の入口」**（インデックス）です。
- 仕様や運用ルールは、ここから参照できる状態を維持します。
- **ここに載っていない情報は“正本ではない”**扱いとします。

---

## 1. 正本ルール（最重要）
1) **仕様の正本（ゲーム内容）** と **運用の正本（開発手順）** を混ぜない。
2) 変更が発生したら、該当正本Canvasに追記し、最後にこの目次へ反映する。
3) デバッグ中は仕様変更禁止。仕様変更は仕様Canvas側の変更ログ→本文更新で行う。

---

## 2. 正本一覧

### A. 仕様（ゲーム内容）
- **ReE α1.x 戦闘システム（ブレ防止の正本）**
  - 範囲：α1.0〜α1.3（戦闘コア→拡張→観測→AI）
  - 目的：戦闘仕様の確定・矛盾排除・完成条件の固定
  - 更新ルール：仕様確定事項は必ずここへ。未確定は［保留］。

### B. 運用（作業手順）
- **Antigravity運用SOP（ReE開発用・正本）**
  - 範囲：Plan→承認→Implement→テストの手順、Touch List、差分最小、1ターン=1件
  - 目的：事故（エラー連鎖・仕様ブレ）を防ぎ、前進を可視化

---

## 3. 使い方（毎回これだけ）
- 仕様の話をする：**戦闘仕様Canvas** を開いて進める
- 手順の話をする：**SOP Canvas** を開いて進める
- 新チャット開始：
  - 「どの正本Canvasを前提にするか」を最初に宣言
  - デバッグ時は再開パケット（Canvas名＋Touch List＋最上段エラー＋直前差分要旨）

---

## 4. 変更履歴
- 2026-01-06 / Noa / 新規作成：正本Canvasを2系統（仕様／運用）で統合するためのインデックスを追加



---

## 8. 戦況判断（おすすめ）内部仕様 v1.0（ActionProposal / AssistPlan）
### 概要
- 「戦況判断」は“文章助言”ではなく、**実行可能な候補（提案）をボタン化**する。
- UIで押せるのは「おすすめ①〜③／ランダム／候補試行中」だが、内部では **提案の束**を生成し、そこから1件を採用して ActionIntent に変換する。

### 用途
- 実装側が「おすすめとは何か」を迷わず、ブレなく再現できる。
- LLM（ローカル／クラウド）を使っても、出力を **提案データ**に落とすことで暴走を抑える。

### 主要カラム例
#### 8.1 ActionProposal（提案＝まだ確定していない候補）
```text
ActionProposal {
  proposalId: string,
  label: string,                 // UIに表示する短い文（例："安全策：回避"）
  rationale?: string,            // ログ用の理由（数値なし）

  // 変換先（ほぼActionIntent雛形）
  intentDraft: ActionIntent,

  // 採用条件（任意）
  requiresTargetSelect: bool,
  requiresLibrarySelect: bool,

  // 並び替え用
  scoreBand: enum { Safe, Balanced, Risky, Random, Experimental },
  rank: int                      // 同Band内の優先順位（0が最優先）
}
```

#### 8.2 AssistPlan（おすすめボタン1つが持つ“提案の束”）
```text
AssistPlan {
  assistPlanId: string,
  band: enum { Safe, Balanced, Risky, Random, Experimental },
  title: string,                 // 例："おすすめ①：安全策"
  proposals: ActionProposal[]    // 原則5件（α1.0は最低1件でも可）
}
```

### 生成ルール（確定）
- おすすめ①（安全策）：防御・回復・撤退寄り。生存優先。
- おすすめ②（バランス）：攻守の両立。標準。
- おすすめ③（リスク）：大技・詠唱・距離詰めなど。勝ち筋優先。
- ランダム：上記Bandからランダム抽出（ただし実行不能案は除外）。
- 候補試行中：Experimental（未確定の新方針・新候補）

### UI動作（α1.0安全仕様）
- 戦況判断メニューの各ボタン押下 → **AssistPlanを生成** → そのPlanの「先頭提案（rank=0）」を仮採用して Confirm へ。
  - ここで誤爆防止のため **必ずConfirm** を挟む。
- α1.2以降：AssistPlanを開いて **提案5件を一覧表示**し、選んだ提案をConfirmへ。

### ActionProposal → ActionIntent 変換（確定）
- 採用した提案の `intentDraft` を ActionIntent として扱う。
- ただし未確定が残る場合は、次の画面へ遷移：
  - requiresLibrarySelect=true → Libraryへ
  - requiresTargetSelect=true → TargetSelectへ
- 最終的に Confirm で検証（チェックリスト v1.0）→ Commit。

### 8.3 LLM出力フォーマット（JSONのみ）【追記・確定】
- LLMは自由文ではなく、**AssistPlan JSONのみ**を返す。
- JSON以外（文章、前置き、コードブロック）は **破棄**し、フォールバックへ。

#### 8.3.1 受理する最小JSON（α1.0）
```text
{
  "assistPlanId": "assist_safe_001",
  "band": "Safe",
  "title": "おすすめ①：安全策",
  "proposals": [
    {
      "proposalId": "p0",
      "label": "回避（安全）",
      "rationale": "相手の動きが読めないため、まず被弾を避ける。",
      "scoreBand": "Safe",
      "rank": 0,
      "requiresTargetSelect": false,
      "requiresLibrarySelect": false,
      "intentDraft": {
        "intentType": "Defend",
        "actionKind": "Evade",
        "targetPolicy": "Self",
        "executionModel": "Instant",
        "snapshotPolicy": "AtCommit",
        "interrupt": { "allowDuringResolving": true },
        "display": { "title": "回避" }
      }
    }
  ]
}
```

#### 8.3.2 JSON検証（必須）
- proposals が空なら破棄
- intentDraft が Confirm検証チェックを満たさない場合は破棄
- Magic の場合は必ず：executionModel=HasCast & snapshotPolicy=AtCastComplete

### 8.4 生成入力（LLMへ渡す戦況コンテキスト）【追記・確定】
- LLMへ渡すのは **短いスナップショット**に限定（暴走防止）：
  - 味方/敵の人数
  - 現在の距離（近/中/遠の3段階で可）
  - 直近ログ（最大5行）
  - 自分の状態（行動可能/不可、危険/通常 などラベル）
  - 使用可能カテゴリ（攻撃/防御/特殊/アイテム の可否）
- HP/MPなど数値は渡してもよいが、**出力には数値を出さない**。

### 8.5 フォールバック（LLM不調でも止まらない）【追記・確定】
- LLM失敗（無応答/JSON不正/検証NG）の場合：
  - Safe：回避 or ガード（行動不能なら何もしない）
  - Balanced：通常攻撃（可能なら）
  - Risky：攻撃魔法（ただしspell未選択なら通常攻撃に落とす）
  - Random：Safe/Balancedからランダム
  - Experimental：常にBalancedへ落とす

---

## 9. TargetSelect仕様 v1.0（対象選択の正本）
### 概要
- 単体/自分/地点などの対象選択を統一し、UIの迷いと実装ブレを防ぐ。

### 用途
- Skill/Magic/Item/特殊行動/Assist すべてが同じ対象選択で動く。

### 主要カラム例
- targetPolicy：None / Self / EnemySingle / AllySingle / AnySingle / Point / Area
- targetIds：単体なら1件
- targetPoint：Point/Areaで必須

### 対象候補の提示（確定）
- EnemySingle：生存している敵のみを候補
- AllySingle：生存している味方のみを候補
- AnySingle：生存している全ユニット
- Self：選択画面を省略して自分固定でも可

### キャンセル/戻る（確定）
- TargetSelectでBack → 直前のメニュー（Library or 下層メニュー）へ戻る
- ConfirmでBack → TargetSelectへ戻る

### α1.0の制限（確定）
- Areaは「Point中心」まででOK（半径/形状の表現はα1.1〜）
- Point/Areaが必要な行動は、α1.0では最小限に留める（事故防止）

### 9.1 UI表示ルール（ActionBoard 5枠運用）【追記・確定】
- TargetSelectは ActionBoard を再利用し、**最大5候補をOption0〜4**に並べる。
- 候補が6件以上ある場合はページングを行う。

#### 9.1.1 ページング（確定）
- Option4 を「次の候補（次ページ）」に割り当てる（候補が6件以上の時のみ）。
- ページングを出す場合、候補は Option0〜3 に4件表示する（Option4を次ページに確保）。
- Back は常に「キャンセル（直前へ戻る）」。

### 9.2 候補の並び順（確定）
- 近い順（距離）→ 同距離なら HP状態の良い/悪い等の並びは **α1.0では行わない**（ブレ防止）。
- 近い順が取れない場合は「生成順/ID順」で固定（決め打ちでOK）。

### 9.3 選択結果の反映（確定）
- 候補を押した瞬間に targetIds をセットし、**Confirm へ遷移**する。
- Confirm でOKなら Commit、Backなら TargetSelect に戻る。

### 9.4 Assist（戦況判断）との整合（確定）
- AssistPlan/ActionProposal が targetPolicy を指定している場合：
  - targetPolicy=None/Self：TargetSelectを省略できる。
  - それ以外：TargetSelectへ（requiresTargetSelect=true）。
- AssistPlan が「優先対象（例：最も近い敵）」を暗黙に含む場合でも、**α1.0は自動確定しない**。
  - 必ず Confirm を挟む（誤爆防止）。

---

## 10. LLM/API統合方針（戦闘システム内の役割分担）
### 概要
- 戦闘は「数値処理（World Engine）」と「提案/演出（LLM）」を分離する。
- これにより、**Unityを起動しなくても“コンテンツ生成”は進む**が、ゲーム進行（確定と保存）はEngine側が担保する。

### 用途
- 戦況判断（おすすめ）の生成
- 戦闘ログの文章化（Hardcore向け：数値を出さず雰囲気で）
- 戦闘後の要約（研究ノート向け）

### 採用ツール（確定方針）
1) **LM Studio：elyza-japanese-llama-2-7b-fast-instruct（ローカル）**
- 役割：
  - 戦況判断（AssistPlan）生成（短文・軽量）
  - 戦闘ログの文体整形（短い演出）
- 方針：
  - 出力はJSON（AssistPlan/ActionProposal）を基本
  - 失敗時はテンプレ/ルールベースにフォールバック

2) **OpenAI API（クラウド）**
- 役割：
  - 高重要度の文章生成（ボス戦の描写、章の締め、長文要約）
  - 追加コンテンツ生成（新魔法/新スキルの説明文、辞書文）※ただし戦闘中の確定処理はEngineが握る
- 方針：
  - 重要度スコアが閾値以上のみクラウド（コスト制御）
  - 出力は「構造化＋短い説明」を優先

### フェイルセーフ（必須）
- ローカルLLMが不調／応答不能でも戦闘が止まらない
  - 戦況判断：ルールベースのおすすめを返す
  - ログ：定型文で続行
- LLM出力は必ず検証し、検証NGは破棄して定型に切替

---

## 11. コンテンツ増殖（Unity起動なしで増える仕組みの整理）
### 概要
- 「アイテム/魔法/敵が増える」＝**マスターデータが増える**こと。
- Unityはマスターデータを読み込み、戦闘で使える形に変換する器。

### 用途
- Unity未起動でも、スプレッドシート/CSV/Markdownに追加しておけばコンテンツは増える。

### 運用（推奨）
- 追加は外部データ（CSV/Excel/JSON/Markdown）に記録
- Unity側は起動時（またはロード時）に読み込む
- 追加生成（LLM）→ 監査（人/AI）→ 正本へ追記 → ゲームに反映、の順で固定

---

## 12. Antigravity投下用：完全仕様書（Plan→承認 前提）
### 12.1 目的
- ReE α1.0の戦闘UIとTimeCore予約（ActionIntent）を、仕様どおりに実装する。
- 仕様の変更は行わない（変更が必要ならこのCanvasへ戻って先に仕様改定）。

### 12.2 実装範囲（α1.0）
- UI：Root（攻撃/防御/特殊行動/アイテム/戦況判断）＋各下層（5枠＋Back固定）
- Library：Skill/Magic/Itemの選択導線（最小）
- TargetSelect：単体/自分（最小）
- Confirm：検証チェックリスト v1.0 を実装（壊れる予約を弾く）
- Commit：ActionIntentを凍結してキューへ投入
- 防御割り込み：解決中UIからDefenseMenuへ遷移→予約できる
- 魔法：snapshotPolicy=AtCastComplete（詠唱完了時点で発射判定）

### 12.3 実装制約（必須）
- **Plan-First**：私が「Approved」と言うまで実装禁止
- **Touch List厳守**：Touch List以外のファイル変更禁止（.meta/asmdef禁止）
- **差分最小**：全文置換禁止。必要箇所のみ。
- **1ターン=1件**：機能1単位、または最上段コンパイルエラー1件のみ。

### 12.4 Plan出力フォーマット（必須）
1) Touch List（編集する.csの一覧）
2) 状態遷移（Root→下層→Library/Target/Confirm→Commit、Resolving割り込み含む）
3) ActionIntentの生成箇所（どこで雛形作成、どこで確定、どこで検証）
4) Confirm検証の実装方針（チェックリスト v1.0を満たす）
5) リスク（参照切れ、イベント二重登録、戻る破綻）
6) テスト手順（Unity上で押す順番と期待結果）

### 12.5 Done条件（α1.0）
- Rootから全カテゴリへ遷移できる
- 下層からLibrary/Target/Confirmへ進める
- Confirmで未選択/参照切れ/対象未選択が弾ける
- CommitでActionIntentが凍結され、キューへ投入される
- Resolving中でも防御だけ割り込める
- 魔法はAtCastCompleteで判定される（決定時点で判定しない）

---

## 7. 変更履歴（追記）
- 2026-01-08 / Noa / 追記：戦況判断の内部仕様（ActionProposal/AssistPlan）、TargetSelect仕様、LLM/API統合方針、Antigravity投下用完全仕様書

