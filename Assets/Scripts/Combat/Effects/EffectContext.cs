using System.Collections.Generic;

namespace ReE.Combat.Effects
{
    // C-13: Attribute Hooks Interface
    public interface IAttributeHook { }

    // C-15: Job Hooks Interface
    public interface IJobHook { }

    // Packet_004.3: Ordered Hook Interface (SAFE)
    public interface IOrderedHook
    {
        int ExecutionOrder { get; }
    }

    public class EffectContext
    {
        // Payload (Packet_004.6)
        public BattleStateSnapshot BattleState { get; set; }

        // Knowledge
        public int ObservationLevel { get; private set; } = 0;
        public bool HasObservationLevel { get; private set; } = false;

        public void SetObservationLevel(int level)
        {
            ObservationLevel = level;
            HasObservationLevel = true;
        }

        // Hooks
        public List<IAttributeHook> AttributeHooks { get; set; } = new List<IAttributeHook>();
        public List<IJobHook> JobHooks { get; set; } = new List<IJobHook>();
        
        // Future extensions (Distance, Fatigue, etc.) can be added here.
    }
}
