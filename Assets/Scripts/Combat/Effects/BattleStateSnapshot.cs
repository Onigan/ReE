namespace ReE.Combat.Effects
{
    // Packet_004.6: Context Payload Skeleton (Snapshot)
    public class BattleStateSnapshot
    {
        public int? TurnIndex { get; set; }
        public string WeatherId { get; set; }
        public int? EncounterSeed { get; set; }

        // Packet_006.0: Source of Truth ID
        public string SourceId { get; private set; }

        public void SetSourceId(string id)
        {
            SourceId = id;
        }
    }
}
