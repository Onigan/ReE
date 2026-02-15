using ReE.Stats;

namespace ReE.BattleTimeCore
{
    /// <summary>
    /// 戦闘中に必要な「参照まとめ」。
    /// </summary>
    public sealed class ActorState_TimeCore
    {
        public readonly string name;
        public readonly CharacterStatus status;

        public ActorState_TimeCore(string name, CharacterStatus status)
        {
            this.name = name;
            this.status = status;
        }

        public bool IsDead => status == null || status.IsDead;
    }
}
