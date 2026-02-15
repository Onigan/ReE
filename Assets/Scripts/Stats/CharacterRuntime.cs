using System;
using System.Reflection;
using UnityEngine;
using ReE.Combat;
namespace ReE.Stats
{
    /// <summary>
    /// CharacterDefinition（CD_***）を参照し、戦闘中に変動する値（HPなど）を保持するランタイム。
    /// ※ Definition の型に依存しない（ScriptableObjectとして受け、リフレクションで読む）ことで、
    ///    フィールド名差異によるコンパイルエラーを回避する。
    ///
    /// 互換レイヤー：
    /// - CharacterStatus から参照される Runtime.resources.hp / maxHP を提供
    /// - ResetRuntimeHPToMax() を提供
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterRuntime : MonoBehaviour
    {
        [Header("Definition (Source of Truth)")]
        [SerializeField] private ScriptableObject definition; // CD_Player / CD_Enemy_Goblin 等を入れる

        [Header("Runtime (Mutable)")]
        [SerializeField] private int currentHP;

        [SerializeField] private WeaponDef equippedWeapon; // ★追加：戦闘で使う武器（初期はCDのdefaultWeapon）

        [Header("Init Options")]
        [SerializeField] private bool autoInitializeOnAwake = true;

        public ScriptableObject Definition => definition;

        /// <summary>
        /// 戦闘で使用する武器（素手/爪/牙/武器を含む）。
        /// 基本は CharacterDefinition の defaultWeapon を初期値として読み込む。
        /// </summary>
        public WeaponDef EquippedWeapon
        {
            get
            {
                EnsureInitialized();
                return equippedWeapon;
            }
            set
            {
                EnsureInitialized();
                equippedWeapon = value;
                RecalculateBaseATK();
            }
        }

        // -----------------------
        // Compatibility: Runtime.resources.hp / maxHP
        // -----------------------
        [Serializable]
        public sealed class RuntimeData
        {
            public ResourcesData resources = new ResourcesData();
        }

        [Serializable]
        public sealed class ResourcesData
        {
            public int hp = 1;
            public int maxHP = 1;
        }

        [Header("Compatibility Runtime View")]
        [SerializeField] private RuntimeData runtime = new RuntimeData();

        /// <summary>
        /// 旧コード互換：reeRuntime.Runtime.resources.hp / maxHP
        /// </summary>
        public RuntimeData Runtime
        {
            get
            {
                EnsureInitialized();
                // 念のため同期して返す
                runtime.resources.maxHP = Mathf.Max(1, MaxHP);
                runtime.resources.hp = Mathf.Clamp(CurrentHP, 0, runtime.resources.maxHP);
                return runtime;
            }
        }

        public string DisplayName { get; private set; } = "Unknown";
        // 互換用：過去コードが actorName を参照しても落ちないようにする
        public string actorName => DisplayName;




       


        public int MaxHP { get; private set; } = 1;
        public int BaseATK { get; private set; } = 0;
        public int DefATK { get; private set; } = 0; // Definition由来の基礎攻撃力
        public int WeaponATK => (equippedWeapon != null ? Mathf.RoundToInt(equippedWeapon.BasePower) : 0);

        private void RecalculateBaseATK()
        {
            BaseATK = DefATK + WeaponATK;
        }

        public int BaseDEF { get; private set; } = 0;
        public int DefDEF { get; private set; } = 0; // Definition由来
        public int ArmorDEF { get; private set; } = 0; // 装備由来（現状0）

        // P0.6: Guard Stance
        public bool IsGuarding { get; private set; } = false;
        public float GuardMitigation => 0.30f;
        public int GuardTurnId { get; private set; } = 0;

        private void RecalculateBaseDEF()
        {
            BaseDEF = DefDEF + ArmorDEF;
        }

        public void EnterGuard(int turnKey)
        {
            IsGuarding = true;
            GuardTurnId = turnKey;
        }

        public void ExitGuard(string reason)
        {
            if (!IsGuarding) return;
            IsGuarding = false;
            GuardTurnId = 0;
            // Logger hooks can go here if needed, but we used BattleTimeManager for logging
        }

        // P0.8: TimeShift Accumulator (P0.9: Clamped [-2, 0])
        private int _pendingTimeShiftTick = 0;
        public int PendingTimeShiftTick 
        {
            get => _pendingTimeShiftTick;
            set => _pendingTimeShiftTick = Mathf.Clamp(value, -2, 0);
        }

        // P1.2: Grid Coordinate (SSOT)
        public Vector3Int GridPos { get; set; } = Vector3Int.zero;

        // P1.2.1: Contact State Hook (State-based "Melee")
        public enum ContactState { None, CloseBody, Grapple }
        public ContactState CurrentContactState { get; set; } = ContactState.None;

        private bool _initialized = false;

        public int CurrentHP
        {
            get => currentHP;
            set
            {
                EnsureInitialized();
                currentHP = Mathf.Max(0, value);
                runtime.resources.maxHP = Mathf.Max(1, MaxHP);
                runtime.resources.hp = Mathf.Clamp(currentHP, 0, runtime.resources.maxHP);
            }
        }

        private void Awake()
        {
            if (autoInitializeOnAwake) EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // definitionが無い場合は最低限の値で走らせる（テスト用）
            if (definition == null)
            {
                DisplayName = gameObject.name;
                MaxHP = Mathf.Max(1, MaxHP);

                // currentHP が未設定なら満タン開始
                if (currentHP <= 0) currentHP = MaxHP;

                // 互換Runtimeも同期
                runtime.resources.maxHP = MaxHP;
                runtime.resources.hp = Mathf.Clamp(currentHP, 0, MaxHP);
                return;
            }

            DisplayName = ReadString(definition, "displayName") ?? definition.name;

            // MaxHP は resources.MaxHP / resources.maxHP / MaxHp など揺れに対応
            MaxHP = ReadInt(definition, 1,
                "baseStats.resources.MaxHP",
                "baseStats.resources.maxHP",
                "baseStats.resources.MaxHp",
                "resources.MaxHP",
                "resources.maxHP",
                "MaxHP",
                "maxHP",
                "baseHP",
                "HP");

            // 攻撃力
            // 攻撃力 (P0.4: AttackPower配線)
            DefATK = ReadInt(definition, 0,
                "baseStats.combat.AttackPower",
                "baseStats.combat.attackPower",
                "combat.AttackPower",
                "baseStats.combat.ATK",
                "baseStats.combat.Atk",
                "combat.ATK",
                "combat.Atk",
                "ATK");

            // 防御力
            DefDEF = ReadInt(definition, 0,
                "baseStats.combat.DefensePower",
                "baseStats.combat.defensePower",
                "combat.DefensePower",
                "baseStats.combat.DEF",
                "baseStats.combat.Def",
                "combat.DEF",
                "DEF");
            RecalculateBaseDEF();

            // ★追加：Weapon（optional）CharacterDefinition の defaultWeapon を初期装備として読む
            if (equippedWeapon == null)
            {
                equippedWeapon = ReadWeaponDef(definition,
                    "defaultWeapon",
                    "DefaultWeapon");
            }
            RecalculateBaseATK();

            // 初期HPはMaxHPで満タン開始
            currentHP = Mathf.Clamp(MaxHP, 0, MaxHP);

            // 互換Runtimeも同期
            runtime.resources.maxHP = MaxHP;
            runtime.resources.hp = currentHP;
        }

        public void ResetToFullHP()
        {
            EnsureInitialized();
            CurrentHP = MaxHP;
        }

        /// <summary>
        /// 旧コード互換：CharacterStatus が呼ぶ想定のメソッド
        /// </summary>
        public void ResetRuntimeHPToMax()
        {
            ResetToFullHP();
        }

        // -----------------------
        // Reflection helpers
        // -----------------------
        private static string ReadString(object root, params string[] paths)
        {
            foreach (var p in paths)
            {
                if (TryGetValue(root, p, out var value))
                {
                    if (value is string s) return s;
                }
            }
            return null;
        }

        private static int ReadInt(object root, int fallback, params string[] paths)
        {
            foreach (var p in paths)
            {
                if (TryGetValue(root, p, out var value))
                {
                    try
                    {
                        if (value is int i) return i;
                        if (value is float f) return Mathf.RoundToInt(f);
                        if (value is double d) return (int)Math.Round(d);
                        if (value is long l) return (int)l;
                        if (value is string s && int.TryParse(s, out var parsed)) return parsed;
                    }
                    catch { /* ignore */ }
                }
            }
            return fallback;
        }

        private static WeaponDef ReadWeaponDef(object root, params string[] paths)
        {
            foreach (var p in paths)
            {
                if (TryGetValue(root, p, out var value))
                {
                    if (value is WeaponDef w) return w;
                }
            }
            return null;
        }

        private static bool TryGetValue(object root, string path, out object value)
        {
            value = null;
            if (root == null || string.IsNullOrWhiteSpace(path)) return false;

            object current = root;
            var parts = path.Split('.');
            foreach (var part in parts)
            {
                if (current == null) return false;

                var t = current.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var field = t.GetField(part, flags);
                if (field != null)
                {
                    current = field.GetValue(current);
                    continue;
                }

                var prop = t.GetProperty(part, flags);
                if (prop != null)
                {
                    current = prop.GetValue(current);
                    continue;
                }

                return false;
            }

            value = current;
            return true;
        }
    }
}
