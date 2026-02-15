using ReE.Combat.Effects;

namespace ReE.Combat.Effects
{
    // Packet_004.7: EffectContext Builder (Responsibility Consolidation)
    public static class EffectContextBuilder
    {
        // P005.0: Minimal argument expansion (int? turnIndex). 
        // Future: Use ContextBuildArgs if arguments grow.
        public static EffectContext BuildForIntent(int? turnIndex = null, string weatherId = null)
        {
            var ctx = new EffectContext();

            // Packet_004.6: Context Payload Skeleton (Log-Only)
            // Packet_004.6 / Packet_005 / Packet_006: BattleStateSnapshot Logic
            bool p046 = ReEFeatureFlags.EnablePacket004_6_ContextPayloadSkeleton;
            bool p005_0 = ReEFeatureFlags.EnablePacket005_0_BattleStateTurnIndexFill;
            bool p005_1 = ReEFeatureFlags.EnablePacket005_1_BattleStateWeatherIdFill;
            bool p005_2 = ReEFeatureFlags.EnablePacket005_2_BattleStateDebugFillAll;
            bool p006_0 = ReEFeatureFlags.EnablePacket006_0_BattleStateSotScaffold;
            bool p006_2 = ReEFeatureFlags.EnablePacket006_2_BattleStateWeatherIdRealSource;

            bool needSnapshot = p046 || p005_0 || p005_1 || p005_2 || p006_0 || p006_2;
            bool fillTurn = p005_0 || p005_2;
            bool fillWeather = p005_1 || p005_2;

            if (needSnapshot)
            {
                // Common Null Safety: Ensure BattleState exists
                if (ctx.BattleState == null)
                {
                    ctx.BattleState = new BattleStateSnapshot
                    {
                        TurnIndex = null,
                        WeatherId = null,
                        EncounterSeed = null
                    };
                }

                // Packet_005.0 / 005.2: Fill TurnIndex
                if (fillTurn)
                {
                    ctx.BattleState.TurnIndex = turnIndex;
                }

                // Packet_005.1 / 005.2: Fill WeatherId (Default Debug Fill)
                if (fillWeather)
                {
                    ctx.BattleState.WeatherId = "Clear";
                }

                // Packet_006.2: Real-Source Weather Overwrite
                if (p006_2 && !string.IsNullOrEmpty(weatherId))
                {
                    ctx.BattleState.WeatherId = weatherId;
                }

                // Packet_006.0: Set Source ID
                if (p006_0)
                {
                    ctx.BattleState.SetSourceId(BattleStateProvider.GetSelectedSourceId());
                }
            }

            return ctx;
        }
    }
}
