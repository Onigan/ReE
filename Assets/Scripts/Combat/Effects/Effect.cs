using System;
using UnityEngine;

namespace ReE.Combat.Effects
{
    public enum EffectKind
    {
        Damage,
        Heal
    }

    [Serializable]
    public class Effect
    {
        public EffectKind kind;
        public float basePower;
        public string attributeTag;

        public Effect()
        {
            attributeTag = "";
        }
    }
}
