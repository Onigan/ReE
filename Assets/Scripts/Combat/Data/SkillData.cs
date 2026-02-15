using UnityEngine;

namespace ReE.Combat.Data
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "ReE/Combat/SkillData")]
    public class SkillData : ScriptableObject
    {
        public string skillId;
        public string displayName;
        // Packet_008.3: FreeText Support
        public string alias;
        public string incantation;
        public SkillKind kind;

        // Packet_008.4: Auto-ID
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(skillId)) skillId = name;
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }

        // Packet_007.1: Base Power for Minimal Mapping
        public int basePower = 0;

        // Packet_007.3: Targeting Rules
        public TargetPolicy targetPolicy = TargetPolicy.EnemyOnly;
        public TargetSide defaultTargetSide = TargetSide.Enemy;

        // Packet_007.4: Buff Definition
        public ReE.Combat.Effects.EffectType buffType;
        public ReE.Combat.Effects.EffectTag buffTag;
        public float buffMagnitude;
        public float buffDurationSeconds;
        public ReE.Combat.Effects.StackRule buffStackRule;
        public string fallbackSkillId; // C-07-4-2: Fallback Slot

        // Packet_007.5: Reactive Definition
        public bool grantsReactive;
        public ReE.Combat.Effects.ReactiveType reactiveType;
        public float reactiveMagnitude;
        public string reactiveScope;
        public float reactiveDurationSeconds;
        public ReE.Combat.Effects.StackRule reactiveStackRule;

        // Packet_008.1: UI Categories
        public SkillUISubCategory uiSub;
        public SkillUIMainCategory uiMain;

        // Future fields: Range, TargetType, etc. (Packet_007.x+)
    }

    public enum SkillUISubCategory
    {
        Normal = 0,
        Skill = 1,
        Magic = 2
    }

    public enum SkillUIMainCategory
    {
        None = 0,
        Attack = 1,
        Defense = 2,
        Support = 3,
        Heal = 4,
        Utility = 5
    }
}
