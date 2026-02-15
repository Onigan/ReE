// ReE - TimeCore Battle Engine v0.1 (safe & minimal)
// Purpose: provide deterministic damage calculation + safe HP access.
// NOTE: Keep this file in global namespace to avoid Unity "Missing Script" issues.

using ReE.Stats;
using System;
using System.Reflection;
using UnityEngine;

namespace ReE.Combat.TimeCore
{


    public class BattleEngine_TimeCore_v01
    {
    // ----- Damage -----

    public static int ComputeDamage(
        int atk,
        int def,
        float atkMultiplier = 1.0f,
        int flatBonus = 0,
        bool ignoreDefense = false)
    {
        int effectiveDef = ignoreDefense ? 0 : def;
        // atk * multiplier is rounded to int (Unity-style).
        int raw = Mathf.RoundToInt(atk * atkMultiplier) + flatBonus - effectiveDef;
        return Mathf.Max(0, raw);
    }

    public static int ComputeDamage(
        CharacterStatus attacker,
        CharacterStatus target,
        float atkMultiplier = 1.0f,
        int flatBonus = 0,
        bool ignoreDefense = false)
    {
        if (attacker == null || target == null) return 0;

        int atk = SafeGetInt(attacker, new[] { "ATK", "Atk", "atk", "BaseATK", "baseATK" }, fallback: 0);
        int def = SafeGetInt(target, new[] { "DEF", "Def", "def", "BaseDEF", "baseDEF" }, fallback: 0);

        return ComputeDamage(atk, def, atkMultiplier, flatBonus, ignoreDefense);
    }

    // ----- Apply Damage -----

    /// <summary>
    /// Apply damage to target if possible. Returns remaining HP if readable; otherwise -1.
    /// </summary>
    public static int ApplyDamage(CharacterStatus target, int damage)
    {
        if (target == null) return -1;
        damage = Mathf.Max(0, damage);

        // 1) Prefer method ApplyDamage(int)
        var t = target.GetType();
        var m = t.GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
        if (m != null)
        {
            try { m.Invoke(target, new object[] { damage }); } catch { /* ignore */ }
            return GetHP(target);
        }

        // 2) Try common HP fields/properties and subtract
        int hp = GetHP(target);
        if (hp >= 0)
        {
            int newHp = Mathf.Max(0, hp - damage);
            if (TrySetInt(target, new[] { "HP", "Hp", "hp", "CurrentHP", "currentHP", "currentHp" }, newHp))
                return newHp;
        }

        return -1;
    }

    /// <summary>Try read HP from known member names. Returns -1 if not found.</summary>
    public static int GetHP(CharacterStatus s)
    {
        if (s == null) return -1;
        return SafeGetInt(s, new[] { "HP", "Hp", "hp", "CurrentHP", "currentHP", "currentHp" }, fallback: -1);
    }

    // ----- Reflection helpers -----

    private static int SafeGetInt(object obj, string[] names, int fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();

        foreach (var name in names)
        {
            // property
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(int))
            {
                try { return (int)p.GetValue(obj); } catch { }
            }

            // field
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                try { return (int)f.GetValue(obj); } catch { }
            }
        }

        return fallback;
    }

    private static bool TrySetInt(object obj, string[] names, int value)
    {
        if (obj == null) return false;
        var t = obj.GetType();

        foreach (var name in names)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite && p.PropertyType == typeof(int))
            {
                try { p.SetValue(obj, value); return true; } catch { }
            }

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                try { f.SetValue(obj, value); return true; } catch { }
            }
        }

        return false;
    }
}
}
