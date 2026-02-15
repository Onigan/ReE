using UnityEngine;

namespace ReE.Stats
{
    /// <summary>
    /// 戦闘で参照される「このキャラのステータス入口」。
    /// 1) ReE Runtime があるならそれを正とする（CD由来）
    /// 2) 無い場合は Legacy Base Status を使う（テスト用）
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterStatus : MonoBehaviour
    {
        [Header("Legacy Base Status (fallback)")]
        [SerializeField] private int baseHP = 100;
        [SerializeField] private int baseATK = 10;
        [SerializeField] private int baseDEF = 5;

        [Header("ReE Runtime (optional)")]
        [SerializeField] private CharacterRuntime reeRuntime;
        [SerializeField] private bool useReeRuntimeMaxHP = true;
        
        [Header("Debug Override")]
        [SerializeField] private bool _debugUseInspectorStats = true;

        public CharacterRuntime Runtime => reeRuntime;

        public string DisplayName
        {
            get
            {
                if (reeRuntime != null)
                {
                    reeRuntime.EnsureInitialized();
                    return reeRuntime.DisplayName;
                }
                return gameObject.name;
            }
        }

        public int MaxHP
        {
            get
            {
                if (useReeRuntimeMaxHP && reeRuntime != null)
                {
                    reeRuntime.EnsureInitialized();
                    return Mathf.Max(1, reeRuntime.MaxHP);
                }
                return Mathf.Max(1, baseHP);
            }
        }

        [Header("Legacy Current HP (fallback)")]
        [SerializeField] private int _legacyCurrentHP = -1; // -1 = 未初期化

        private void Awake()
        {
            if (reeRuntime == null)
                reeRuntime = GetComponent<CharacterRuntime>();
        }



        public int ATK
        {
            get
            {
                if (_debugUseInspectorStats) return Mathf.Max(1, baseATK);

                if (reeRuntime != null)
                {
                    reeRuntime.EnsureInitialized();
                    return Mathf.Max(1, reeRuntime.BaseATK);
                }
                return Mathf.Max(1, baseATK);
            }
        }

        public int DEF
        {
            get
            {
                if (_debugUseInspectorStats) return Mathf.Max(0, baseDEF);

                if (reeRuntime != null)
                {
                    reeRuntime.EnsureInitialized();
                    return Mathf.Max(0, reeRuntime.BaseDEF);
                }
                return Mathf.Max(0, baseDEF);
            }
        }

        public int CurrentHP
        {
            get
            {
                if (reeRuntime != null)
                {
                    reeRuntime.EnsureInitialized();
                    return reeRuntime.CurrentHP;
                }

                if (_legacyCurrentHP < 0) _legacyCurrentHP = MaxHP; // 初期化
                return _legacyCurrentHP;
            }
            set
            {
                if (reeRuntime != null)
                {
                    reeRuntime.EnsureInitialized();
                    reeRuntime.CurrentHP = Mathf.Clamp(value, 0, reeRuntime.MaxHP);

                    return;
                }

                if (_legacyCurrentHP < 0) _legacyCurrentHP = MaxHP;
                _legacyCurrentHP = Mathf.Clamp(value, 0, MaxHP);
            }
        }


        public bool IsDead => CurrentHP <= 0;

        public void ResetBattleHP()
        {
            if (reeRuntime != null)
            {
                reeRuntime.ResetToFullHP();
                return;
            }
            _legacyCurrentHP = MaxHP;
        }


        public int ApplyDamage(int rawDamage)
        {
            int dmg = Mathf.Max(0, rawDamage);

            // Runtime を正本として処理
            if (reeRuntime != null)
            {
                reeRuntime.EnsureInitialized();
                int before = reeRuntime.CurrentHP;
                reeRuntime.CurrentHP = Mathf.Max(0, before - dmg);
                return before - reeRuntime.CurrentHP;
            }

            // Legacy fallback
            if (_legacyCurrentHP < 0) _legacyCurrentHP = MaxHP;
            int legacyBefore = _legacyCurrentHP;
            _legacyCurrentHP = Mathf.Max(0, _legacyCurrentHP - dmg);
            return legacyBefore - _legacyCurrentHP;
        }


        public void ApplyHeal(int amount) => Heal(amount);

        // Packet_008.5.1: Log Unification
        public static event System.Action<string> OnLog;

        public int Heal(int amount)
        {
            int heal = Mathf.Max(0, amount);
            int before = 0;
            int after = 0;

            if (reeRuntime != null)
            {
                reeRuntime.EnsureInitialized();
                before = reeRuntime.CurrentHP;
                reeRuntime.CurrentHP = Mathf.Min(reeRuntime.MaxHP, before + heal);
                after = reeRuntime.CurrentHP;
            }
            else
            {
                if (_legacyCurrentHP < 0) _legacyCurrentHP = MaxHP;
                before = _legacyCurrentHP;
                _legacyCurrentHP = Mathf.Min(MaxHP, _legacyCurrentHP + heal);
                after = _legacyCurrentHP;
            }
            
            int diff = after - before;
            // Packet_008.5: Combat Feel Log (Heal) -> 008.5.1: UI Log
            OnLog?.Invoke($"[Heal] {DisplayName} +{diff} HP (before={before} after={after})");
            return diff;
        }

        // Packet_007.4: Active Effects Container
        private System.Collections.Generic.List<ReE.Combat.Effects.ActiveEffect> _activeEffects 
            = new System.Collections.Generic.List<ReE.Combat.Effects.ActiveEffect>();

        public System.Collections.Generic.IReadOnlyList<ReE.Combat.Effects.ActiveEffect> ActiveEffects => _activeEffects;

        public void ApplyEffect(ReE.Combat.Effects.ActiveEffect e)
        {
            if (e == null) return;

            var existing = _activeEffects.Find(x => x.type == e.type && x.tag == e.tag);
            if (existing != null && e.stackRule == ReE.Combat.Effects.StackRule.Refresh)
            {
                existing.remainingSeconds = e.durationSeconds;
                return;
            }
            if (existing != null && e.stackRule == ReE.Combat.Effects.StackRule.Extend)
            {
                existing.remainingSeconds += e.durationSeconds;
                return;
            }
            
            _activeEffects.Add(e);
        }

        public void TickEffects(float dt)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].remainingSeconds -= dt;
                if (_activeEffects[i].IsExpired)
                {
                    Debug.Log($"[P074 EffectExpire(Type={_activeEffects[i].type},Id={_activeEffects[i].effectId})]");
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        // Packet_007.5: Reactive Gates
        public bool TryApplyHeal_WithReactive(int amount, out int appliedAmount, out string note, string sourceId = "")
        {
            appliedAmount = 0;
            note = "";

            foreach (var eff in _activeEffects)
            {
                if (!eff.isReactive) continue;
                if (string.IsNullOrEmpty(eff.reactiveScope) || !eff.reactiveScope.Contains("Heal")) continue;

                if (eff.reactiveType == ReE.Combat.Effects.ReactiveType.NullifySupport)
                {
                    Debug.Log($"[P075 ReactiveNullify(Heal,Src={sourceId},Eff={eff.effectId})]");
                    // Packet_008.5: Triggered -> 008.5.1: UI Log
                    OnLog?.Invoke($"[Reactive] Triggered: NullifySupport (blocked Heal from {sourceId})");
                    note = "Nullified";
                    return false;
                }
                if (eff.reactiveType == ReE.Combat.Effects.ReactiveType.InvertHealToDamage)
                {
                    int dmg = Mathf.FloorToInt(amount * eff.reactiveMagnitude);
                    Debug.Log($"[P075 ReactiveInvert(Heal->Dmg,Src={sourceId},Mul={eff.reactiveMagnitude})]");
                    // Packet_008.5: Triggered -> 008.5.1: UI Log
                    OnLog?.Invoke($"[Reactive] Triggered: InvertHealToDamage (Heal transformed to {dmg} Dmg)");
                    ApplyDamage(dmg);
                    note = "InvertedToDamage";
                    return false;
                }
            }

            int diff = Heal(amount);
            appliedAmount = diff;
            return true;
        }

        public bool TryApplyEffect_WithReactive(ReE.Combat.Effects.ActiveEffect newEff, out string note, string sourceId = "")
        {
            note = "";
            // Check only if new effect is NOT reactive itself (prevent blocking the blocker, unless desired? MVP: simple)
            // User requested "Nullify Buff".
            // If I cast Nullify (Buff), checking Scope="Buff".
            // Usually internal logic doesn't block "Debuff". But here generic "Buff".
            // Let's assume Reactive effects are "Buffs" too?
            // "kind=Buff" grantsReactive=true.
            // If I have Nullify, and I cast another Nullify, does it block?
            // "scope" contains "Buff".
            // MVP: Block non-reactive buffs is safer. Or block all.
            // Requirement: "NullifySupport targets Heal/Buff".
            // Let's block *any* effect if scope matches.
            
            foreach (var eff in _activeEffects)
            {
                if (!eff.isReactive) continue;
                if (string.IsNullOrEmpty(eff.reactiveScope) || !eff.reactiveScope.Contains("Buff")) continue;

                if (eff.reactiveType == ReE.Combat.Effects.ReactiveType.NullifySupport)
                {
                    Debug.Log($"[P075 ReactiveNullify(Buff,Type={newEff.type},Src={sourceId})]");
                    // Packet_008.5: Triggered -> 008.5.1: UI Log
                    OnLog?.Invoke($"[Reactive] Triggered: NullifySupport (blocked Buff {newEff.effectId})");
                    note = "Nullified";
                    return false;
                }
            }

            ApplyEffect(newEff);
            return true;
        }

    }
}
