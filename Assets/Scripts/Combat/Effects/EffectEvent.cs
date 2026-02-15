using UnityEngine;

namespace ReE.Combat.Effects
{
    public class EffectEvent
    {
        public string actorId;
        public string targetId;
        public EffectKind kind;
        public float amount;
        public string attributeTag;
        public string debugNote;
        
        // Packet_004.4: Observed Value Info (SAFE)
        public EffectObservationInfo observationInfo;

        public EffectEvent()
        {
            attributeTag = "";
            debugNote = "";
        }
    }
}
