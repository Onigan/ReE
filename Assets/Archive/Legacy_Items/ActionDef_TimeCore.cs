using UnityEngine;
using ReE.Stats;

namespace ReE.BattleTimeCore
{
    public enum TargetType
    {
        EnemySingle,
        Self
    }

    /// <summary>
    /// 行動定義（通常攻撃/スキル/魔法…）
    /// ScriptableObject化しておくと、後で「武技一覧」「魔法一覧」を資産として増やせる。
    /// 今は未作成でも動くよう、BattleTimeManager側でデフォルトを生成できる設計にする。
    /// </summary>
    [CreateAssetMenu(menuName = "ReE/BattleTimeCore/ActionDef", fileName = "AD_Action")]
    public sealed class ActionDef_TimeCore : ScriptableObject
    {
        public string actionId = "normal_attack";
        public string displayName = "通常攻撃";

        [Header("Timing")]
        public float castTimeSec = 2.0f; // 「準備(2秒)」みたいなログ用

        [Header("Damage Model (simple)")]
        [Tooltip("最終ダメージ = max(1, (ATK * powerMul) - DEF)")]
        public float powerMul = 1.0f;

        public TargetType targetType = TargetType.EnemySingle;

        public int ComputeDamage(CharacterStatus attacker, CharacterStatus defender)
        {
            int atk = attacker != null ? attacker.ATK : 1;
            int def = defender != null ? defender.DEF : 0;
            int dmg = Mathf.RoundToInt(atk * powerMul) - def;
            return Mathf.Max(1, dmg);
        }
    }
}
