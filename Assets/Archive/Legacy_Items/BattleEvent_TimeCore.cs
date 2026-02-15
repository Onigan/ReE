namespace ReE.BattleTimeCore
{
    public enum BattleEventType_TimeCore
    {
        StartCast,
        Hit,
        Death,
        TurnChange
    }

    public readonly struct BattleEvent_TimeCore
    {
        public readonly float timeSec;
        public readonly BattleEventType_TimeCore type;
        public readonly int actorIndex;
        public readonly int targetIndex;
        public readonly ActionDef_TimeCore action;

        public BattleEvent_TimeCore(float timeSec, BattleEventType_TimeCore type, int actorIndex, int targetIndex, ActionDef_TimeCore action)
        {
            this.timeSec = timeSec;
            this.type = type;
            this.actorIndex = actorIndex;
            this.targetIndex = targetIndex;
            this.action = action;
        }
    }
}
