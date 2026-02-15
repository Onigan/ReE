namespace ReE
{
    public static class ReEFeatureFlags
    {
        // Packet_001: Effect System MVP
        public const bool EnableEffectMvp = true;

        // Packet_002: Fact Log Bridge
        public static bool EnableEffectFactLogBridge = true;

        // Packet_003: Observation Accuracy & Element Hooks
        public static bool EnableObservationAndElementHooks = false;

        // Packet_004: Multi-stage Multiplier Skeleton (C-08-1)
        public static bool EnablePacket004_MultistageMultiplierSkeleton = true;
        public static bool EnablePacket004_MultistageMultiplierCandidateValues = true;

        // Packet_004.2: EffectContext DI Skeleton (Non-destructive)
        public static bool EnablePacket004_2_EffectContextDI = true;

        // Packet_004.3: Hook Priority Skeleton (SAFE)
        public static bool EnablePacket004_3_HookPrioritySkeleton = true;

        // Packet_004.4: Observed Value Skeleton (C-15, SAFE)
        public static bool EnablePacket004_4_ObservedValueSkeleton = false;

        // Packet_004.5: Context External Injection (Log-Only)
        public static bool EnablePacket004_5_ContextExternalInjection = false;

        // Packet_004.6: Context Payload Skeleton (Log-Only)
        public static bool EnablePacket004_6_ContextPayloadSkeleton = false;

        // Packet_004.7: EffectContext Builder (Log-Only)
        public static bool EnablePacket004_7_EffectContextBuilder = false;

        // Packet_004.8: Context Log Layer Router (Log-Only)
        public static bool EnablePacket004_8_ContextLogRouter = false;

        // Packet_005.0: BattleStateSnapshot Minimal Fill (TurnIndex only, Log-Only)
        public static bool EnablePacket005_0_BattleStateTurnIndexFill = false;

        // Packet_005.1: BattleStateSnapshot Minimal Fill (WeatherId only, Log-Only)
        public static bool EnablePacket005_1_BattleStateWeatherIdFill = false;

        // Packet_005.2: BattleStateSnapshot Debug Fill All (Turn & Weather, Log-Only)
        public static bool EnablePacket005_2_BattleStateDebugFillAll = false;

        // Packet_006.0: BattleStateSnapshot Source-of-Truth Scaffold (Log-Only)
        public static bool EnablePacket006_0_BattleStateSotScaffold = false;

        // Packet_006.1: Weather Multiplier Minimal Apply (Safe-First, Log-Only)
        public static bool EnablePacket006_1_WeatherMultiplierApply = false;

        // Packet_006.2: BattleStateSnapshot WeatherId Real-Source Wiring
        public static bool EnablePacket006_2_BattleStateWeatherIdRealSource = false;

        // Packet_006.3: Weather Multiplier Active Minimal (Lv3 Calc Active)
        public static bool EnablePacket006_3_WeatherMultiplierActive = false;

        // Packet_007.0: SkillData Scaffold (Lv4 Entry)
        public static bool EnablePacket007_0_SkillDataScaffold = false;

        // Packet_007.1: Skill Effect Mapping Minimal (BasePower Override)
        public static bool EnablePacket007_1_SkillEffectMappingMinimal = false;

        // Packet_007.2: SkillKind Execution Minimal (Heal/Buff Split)
        public static bool EnablePacket007_2_SkillKindExecutionMinimal = true;

        // Packet_007.3: Target Side Minimal (Ally Targeting Pipeline)
        public static bool EnablePacket007_3_TargetSideMinimal = true;

        // Packet_007.4: Buff Effect Minimal (Reinforce/Coating)
        public static bool EnablePacket007_4_BuffEffectMinimal = true;

        // Packet_007.5: Reactive Minimal (Nullify/Invert)
        public static bool EnablePacket007_5_ReactiveMinimal = true;

        // Packet_010: Log Layer Router Skeleton (C-10-1)
        public static bool EnablePacket010_LogLayerRouter = false;
    }
}
