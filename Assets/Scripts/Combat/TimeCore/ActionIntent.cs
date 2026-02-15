using System.Collections.Generic;
using ReE.Combat.Effects;

namespace ReE.Combat.TimeCore
{
    // Simple data holder for an action intent
    [System.Serializable]
    public class ActionIntent
    {
        public string intentType;   // "Attack", "Defend", "Observe", etc.
        public string actorId;      // Who is acting
        public List<string> targetIds = new List<string>();

        // Packet_007.3: Target Side
        public ReE.Combat.Data.TargetSide targetSide;

        // Packet_001: Effect System
        public List<Effect> effects = new List<Effect>();

        // Optional metadata for future use
        public string actionKind;   
        public string libraryId; 
        
        // Packet_008.3: FreeText Meta
        public string freeTextMeta; 

        // Packet_008.2: Strict Typing
        public IntentType type;

        // Packet_P0.7_ExecuteSplit: Result Container
        public ActionResult result = new ActionResult();

        public static ActionIntent CreateAttack()
        {
            return new ActionIntent { 
                intentType = "Attack",
                type = IntentType.NormalAttack, // Default
                actorId = "Player", 
                targetIds = new List<string>() 
            };
        }

        public static ActionIntent CreateGuard()
        {
             return new ActionIntent { 
                intentType = "Defend",
                type = IntentType.Defend,
                actorId = "Player",
                targetIds = new List<string>() 
            };
        }

        // Unit-H/I/J/K: Observe
        public static ActionIntent CreateObserve()
        {
             return new ActionIntent { 
                intentType = "Observe",
                type = IntentType.Observe,
                actorId = "Player",
                targetIds = new List<string>() 
            };
        }

        // P1.2: Move Intent
        public static ActionIntent CreateMove()
        {
             return new ActionIntent { 
                intentType = "Move",
                type = IntentType.Move,
                actorId = "Player",
                targetIds = new List<string>() 
            };
        }
    }

    [System.Serializable]
    public class ActionResult
    {
        public bool executed = false;           // Prevent double execution
        public int finalDamage = 0;             // Internal use
        public bool wasGuarded = false;         // Guard check
        public float mitigationRateUsed = 0f;   // Rate used
        public int timeShiftDeltaTick = 0;      // Guard/Counter time shift
        public List<string> logs = new List<string>(); // Resolved system logs
    }

    public enum IntentType
    {
        NormalAttack,
        Skill,
        Magic,
        Defend,
        Observe,
        Item,
        Special,
        Retreat,
        Move // P1.2
    }
}
