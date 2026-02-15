using UnityEngine;

namespace ReE.Combat.Effects
{
    [System.Serializable]
    public class ActiveEffect
    {
        public string effectId;
        public EffectType type;
        public float magnitude;
        public EffectTag tag;
        public float durationSeconds;
        public float remainingSeconds;
        public StackRule stackRule;
        public string sourceSkillId;

        // Packet_007.5: Reactive Properties
        public bool isReactive;
        public ReactiveType reactiveType;
        public float reactiveMagnitude;
        public string reactiveScope; // "Heal", "Buff", etc.

        public bool IsExpired => remainingSeconds <= 0f;
    }
}
