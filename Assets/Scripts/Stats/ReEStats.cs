using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReE.Stats
{
    public enum DamageKind { Slash, Blunt, Pierce }
    public enum ElementKind { Fire, Water, Wind, Earth, Light, Dark, Void }
    public enum AilmentKind
    {
        Poison, Bleed, Burn, Freeze, Paralysis,
        Blind, Silence, Confuse, Fear, Charm, Curse, Fatigue
    }

    [Serializable]
    public struct Attributes
    {
        public int STR, DEX, AGI, VIT, INT, WIL, PER, CHA, LUK;
    }

    [Serializable]
    public struct Resources
    {
        public int MaxHP, MaxMP, MaxStamina, MaxFocus, MaxSoul;
        public int HP, MP, Stamina, Focus, Soul;
    }

    [Serializable]
    public struct CombatDerived
    {
        public int Accuracy;
        public int Evasion;
        public int AttackPower;
        public int DefensePower;

        public int CritChance;
        public float CritPower;

        public float GuardRate;      // 0.0～0.9
        public int Poise;
        public int Stability;

        public float ActionTimeMod;  // 例：0.9で10%短縮
        public int MoveStepMeters;   // 接近/離脱で動く距離
    }

    [Serializable]
    public struct Resistances
    {
        public int Slash, Blunt, Pierce; // 例：-50～+200（内部）
        public int Fire, Water, Wind, Earth, Light, Dark, Void;

        // 状態耐性（同じく内部）
        public int Poison, Bleed, Burn, Freeze, Paralysis, Blind, Silence, Confuse, Fear, Charm, Curse, Fatigue;
    }

    [Serializable]
    public struct Proficiencies
    {
        // 0～100想定（内部）。表示は “未熟/習熟/達人” 等に変換
        public int Sword, Axe, Spear, Bow, Unarmed, Shield, Throwing;
        public int LightArmor, MediumArmor, HeavyArmor;

        // 観測系
        public int SelfDiagnosis, EnemyReading, Appraisal, Medical;

        // 探索・制作
        public int Survival, Navigation, Stealth, Lockpick, Traps;
        public int Gathering, Mining, Hunting, Cooking, Smithing, Alchemy, Research;
    }

    [Serializable]
    public struct Social
    {
        public int Reputation; // 全体（将来は地域別Dictionaryへ）
        public int Trade;
        public int Leadership;
        public int Authority;
    }

    [Serializable]
    public struct Observation
    {
        // “数値非表示”のための表示レベル（0～5）
        public int ObservationLevel;
        public int EquipmentReadLevel;
        public int EnemyIntelLevel;
    }

    [Serializable]
    public class StatBlock
    {
        public Attributes attributes;
        public Resources resources;
        public CombatDerived combat;
        public Resistances resist;
        public Proficiencies prof;
        public Social social;
        public Observation obs;

        // 状態異常（ランタイム）
        public List<AilmentKind> ailments = new List<AilmentKind>();

        /// <summary>
        /// α1.x：計算はここで一括。まずは“簡易式”で良い（後で差し替える）。
        /// </summary>
        public void Recalculate()
        {
            // --- Resources ---
            resources.MaxHP = Mathf.Max(1, 50 + attributes.VIT * 10);
            resources.MaxStamina = Mathf.Max(1, 30 + attributes.VIT * 6 + attributes.AGI * 2);
            resources.MaxFocus = Mathf.Max(1, 10 + attributes.WIL * 3 + attributes.PER * 2);
            // MP/Soulは導入段階で式を確定

            resources.HP = Mathf.Clamp(resources.HP, 0, resources.MaxHP);
            resources.Stamina = Mathf.Clamp(resources.Stamina, 0, resources.MaxStamina);
            resources.Focus = Mathf.Clamp(resources.Focus, 0, resources.MaxFocus);

            // --- Combat ---
            combat.AttackPower = Mathf.Max(0, attributes.STR * 2);
            combat.DefensePower = Mathf.Max(0, attributes.VIT * 1);

            combat.Accuracy = Mathf.Max(0, attributes.DEX * 2 + attributes.PER);
            combat.Evasion = Mathf.Max(0, attributes.AGI * 2 + attributes.PER);

            combat.CritChance = Mathf.Clamp(3 + attributes.DEX - attributes.AGI / 2, 0, 25);
            combat.CritPower = 1.5f;

            combat.GuardRate = Mathf.Clamp01(0.15f + attributes.VIT * 0.01f);
            combat.ActionTimeMod = Mathf.Clamp(1.0f - attributes.AGI * 0.005f, 0.7f, 1.2f);
            combat.MoveStepMeters = Mathf.Clamp(2 + attributes.AGI / 5, 2, 8);
        }
    }


}
