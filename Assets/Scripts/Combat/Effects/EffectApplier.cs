using System.Collections.Generic;
using ReE.Stats;
using UnityEngine;

namespace ReE.Combat.Effects
{
    public static class EffectApplier
    {
        public static void Apply(List<EffectEvent> events, CharacterRuntime actor, CharacterRuntime target, EffectContext context = null)
        {
            if (events == null) return;

            foreach (var evt in events)
            {
                // Packet_006.1 / 006.3: Weather Multiplier Logic
                bool p061 = ReEFeatureFlags.EnablePacket006_1_WeatherMultiplierApply;
                bool p063 = ReEFeatureFlags.EnablePacket006_3_WeatherMultiplierActive;

                if ((p061 || p063) && evt.kind == EffectKind.Damage)
                {
                    string weatherId = (context != null && context.BattleState != null) ? context.BattleState.WeatherId : null;
                    string logWeather = string.IsNullOrEmpty(weatherId) ? "?" : weatherId;
                    
                    float multiplier = 1.0f;

                    // P006.3: Active Calculation
                    if (p063)
                    {
                        if (weatherId == "Rain") multiplier = 0.95f;
                        else if (weatherId == "Clear") multiplier = 1.0f;
                        // Default 1.0
                    }
                    // Else P006.1 (Minimal Apply) -> Fixed 1.0

                    float preAmount = evt.amount;

                    // Strict Application (One-time, RoundToInt)
                    if (Mathf.Abs(multiplier - 1.0f) > Mathf.Epsilon)
                    {
                        evt.amount = Mathf.RoundToInt(preAmount * multiplier);
                    }
                    
                    float postAmount = evt.amount;

                    // Log Generation
                    string tagLog = "";
                    if (p063)
                    {
                        tagLog = $" [P063 WxMulActive(Weather={logWeather},M={multiplier:F2},Pre={preAmount},Post={postAmount})]";
                    }
                    else
                    {
                        // P006.1 Legacy Log
                        tagLog = $" [P061 WxMul(Weather={logWeather},M={multiplier:F1})]";
                    }

                    // Router Append
                    if (ReEFeatureFlags.EnablePacket004_8_ContextLogRouter)
                    {
                        evt.debugNote = ContextLogRouter.AppendTagOnce(evt.debugNote, tagLog);
                    }
                    else
                    {
                        evt.debugNote += tagLog;
                    }
                }

                // LOG (Required for MVP)
                Debug.Log($"[Effect] {evt.kind} {evt.amount} tag={evt.attributeTag} actor={evt.actorId} target={evt.targetId} note={evt.debugNote}");

                // Apply Logic (MVP: basePower=0 expected for Damage, so no actual change mostly)
                // If amount != 0, we try to apply.
                if (Mathf.Abs(evt.amount) > 0.001f)
                {
                    if (evt.kind == EffectKind.Damage)
                    {
                        // Safe HP manipulation via BattleEngine or similar if possible.
                        // For MVP, we use BattleEngine_TimeCore_v01.ApplyDamage if available, or just log.
                        // Assuming BattleEngine_TimeCore_v01 is in global/combat namespace.
                        if (target != null)
                        {
                            // Cast or use existing method
                            try
                            {
                                // We need CharacterStatus.
                                var status = target.GetComponent<CharacterStatus>();
                                if (status != null)
                                {
                                    // Using Reflection based ApplyDamage from Unit-B Engine
                                    ReE.Combat.TimeCore.BattleEngine_TimeCore_v01.ApplyDamage(status, Mathf.RoundToInt(evt.amount));
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"[EffectApplier] Error applying damage: {e.Message}");
                            }
                        }
                    }
                    else if (evt.kind == EffectKind.Heal)
                    {
                         // Placeholder for Heal
                         Debug.Log("[EffectApplier] Heal logic placeholder");
                         // If we wanted to implement: target.status.CurrentHP += amount ...
                    }
                }
            }
        }
    }
}
