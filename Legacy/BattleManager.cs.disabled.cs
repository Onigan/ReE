using UnityEngine;
using TMPro;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    private enum BattlePhase { Start, Player, Enemy, EndTurn }
    private BattlePhase phase = BattlePhase.Start;
    private int currentTurn = 1;

    [Header("キャラ参照")]
    [SerializeField] private CharacterStatus playerStatus;
    [SerializeField] private CharacterStatus enemyStatus;

    [Header("UI参照")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_Text playerHPText;
    [SerializeField] private TMP_Text enemyHPText;

    // 防御フラグ
    private bool playerDefending = false;

    private void Start()
    {
        AppendLog("戦闘開始！");
        AppendLog($"--- Turn {currentTurn} 開始 ---");
        UpdateUI();
        phase = BattlePhase.Player;
    }

    private void Update()
    {
        // Coroutineで制御
        if (phase == BattlePhase.Enemy)
        {
            StartCoroutine(EnemyPhaseRoutine());
            phase = BattlePhase.Start; // 二重呼び出し防止
        }
        else if (phase == BattlePhase.EndTurn)
        {
            StartCoroutine(EndTurnRoutine());
            phase = BattlePhase.Start; // 二重呼び出し防止
        }
    }

    // ========= UI更新 =========
    private void UpdateUI()
    {
        if (playerHPText) playerHPText.SetText($"プレイヤーHP: {SafeHP(playerStatus)}");
        if (enemyHPText) enemyHPText.SetText($"敵HP: {SafeHP(enemyStatus)}");
    }
    private int SafeHP(CharacterStatus s) => s ? Mathf.Max(0, s.baseHP) : 0;

    private void AppendLog(string msg)
    {
        if (!logText) return;
        logText.text += msg + "\n";
    }

    // ========= プレイヤー攻撃 =========
    public void OnAttackButton()
    {
        if (phase != BattlePhase.Player) return;

        ResolveAttack(playerStatus, enemyStatus, "プレイヤー", "敵");
        UpdateUI();

        if (enemyStatus.baseHP <= 0)
        {
            AppendLog("敵を倒した！");
            return;
        }

        phase = BattlePhase.Enemy;
    }

    // ========= プレイヤー防御 =========
    public void OnDefendButton()
    {
        if (phase != BattlePhase.Player) return;

        playerDefending = true;
        AppendLog("プレイヤーは身を守っている！");
        UpdateUI();

        phase = BattlePhase.Enemy;
    }

    // ========= 敵行動（Coroutine経由で呼ぶ） =========
    private IEnumerator EnemyPhaseRoutine()
    {
        yield return null; // 1フレーム待機
        EnemyAction();
    }

    private void EnemyAction()
    {
        int damageBefore = playerStatus.baseHP;

        ResolveAttack(enemyStatus, playerStatus, "敵", "プレイヤー");

        // 防御時はダメージ半減
        if (playerDefending)
        {
            int takenDamage = damageBefore - playerStatus.baseHP;
            int reduced = Mathf.RoundToInt(takenDamage * 0.5f);
            playerStatus.baseHP = damageBefore - reduced;

            AppendLog("防御でダメージを軽減！");
            playerDefending = false;
        }

        UpdateUI();

        if (playerStatus.baseHP <= 0)
        {
            AppendLog("プレイヤーは倒れた…");
            return;
        }

        phase = BattlePhase.EndTurn;
    }

    // ========= ターン終了（Coroutine経由で呼ぶ） =========
    private IEnumerator EndTurnRoutine()
    {
        yield return null; // 1フレーム待機
        EndTurnPhase();
    }

    private void EndTurnPhase()
    {
        AppendLog($"--- Turn {currentTurn} 終了 ---");
        currentTurn++;
        AppendLog($"--- Turn {currentTurn} 開始 ---");

        phase = BattlePhase.Player;
    }

    // ========= 攻撃処理 =========
    private void ResolveAttack(CharacterStatus attacker, CharacterStatus defender, string attackerName, string defenderName)
    {
        int atk = attacker.TotalATK;
        int def = defender.TotalDEF;

        int physDamage = Mathf.Max(0, atk - def);

        defender.baseHP = Mathf.Max(0, defender.baseHP - physDamage);

        AppendLog($"{attackerName}の攻撃！ {defenderName}に {physDamage} ダメージ");
    }


    // BattleManager.cs のクラス内のどこか（public メソッドが置ける位置）に追記
    #region === Facade API (UIからは基本ここだけ呼ぶ) ===
    public void AttackCommand()
    {
        // 既存の攻撃入口に委譲（既に存在する OnAttackButton を呼ぶ）
        OnAttackButton();
    }

    public void DefendCommand()
    {
        // 既存の防御入口に委譲
        OnDefendButton();
    }

    public enum SpecialAction { Flee, CloseDistance, OpenDistance, Inspect }

    public void ItemCommand(int slot)
    {
        // TODO: 実装予定（とりあえず何もしない or ログだけ）
    }

    public void SpecialCommand(SpecialAction action)
    {
        // TODO: 実装予定
    }

    public void TacticalCommand(int index)
    {
        // TODO: 実装予定
    }
    #endregion



}
