using UnityEngine;
using ReE.Combat; // WeaponDef ‚ª ReE.Combat ‚É‚ ‚é‚½‚ß

namespace ReE.Stats
{
    [CreateAssetMenu(menuName = "ReE/Character Definition", fileName = "CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;

        [Header("Base Stats")]
        public StatBlock baseStats = new StatBlock();

        [Header("Combat Loadout")]
        public WeaponDef defaultWeapon; // ‘fè/’Ü/‰å/•Ší ‚·‚×‚Ä‚±‚±‚É·‚·
    }
}
