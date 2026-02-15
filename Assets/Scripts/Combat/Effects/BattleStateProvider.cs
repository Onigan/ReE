using ReE.Combat.Effects;

namespace ReE.Combat.Effects
{
    // Packet_006.0: BattleStateSnapshot Source-of-Truth Scaffold (Log-Only)
    public static class BattleStateProvider
    {
        /// <summary>
        /// Determines the selected source ID for the BattleStateSnapshot.
        /// Note: Current logic is temporary and based on FeatureFlags (DebugFill) for skeleton validation.
        /// Future implementation will check for actual data availability (TimeCore, etc).
        /// </summary>
        public static string GetSelectedSourceId()
        {
            // Priority: RealSource > All > Mixed > Individual
            
            // Packet_006.2: Real Source
            if (ReEFeatureFlags.EnablePacket006_2_BattleStateWeatherIdRealSource)
            {
                return "BattleTimeManager";
            }

            if (ReEFeatureFlags.EnablePacket005_2_BattleStateDebugFillAll)
            {
                return "DebugFill(All)";
            }

            bool p005_0 = ReEFeatureFlags.EnablePacket005_0_BattleStateTurnIndexFill;
            bool p005_1 = ReEFeatureFlags.EnablePacket005_1_BattleStateWeatherIdFill;

            if (p005_0 && p005_1) return "DebugFill(Mixed)";
            if (p005_0) return "DebugFill(Turn)";
            if (p005_1) return "DebugFill(Weather)";

            // Fallback if no debug fill is active
            return "None";
        }
    }
}
