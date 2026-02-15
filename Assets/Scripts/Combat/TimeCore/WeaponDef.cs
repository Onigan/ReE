using UnityEngine;

namespace ReE.Combat
{
    [CreateAssetMenu(menuName = "ReE/Combat/WeaponDef")]
    public class WeaponDef : ScriptableObject
    {
        [Header("Legacy / Basic")]
        public float BasePower;

        public enum WeaponCategory
        {
            Knife,
            Sword,
            Greatsword,
            Spear,
            Bow,
            Gun,
            Unknown
        }

        [Header("Classification")]
        public WeaponCategory category = WeaponCategory.Unknown;

        public string ToDisplayName() => $"{name} ({category})";
    }
}
