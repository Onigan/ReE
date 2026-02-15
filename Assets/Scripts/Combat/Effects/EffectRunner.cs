using System.Collections.Generic;
using ReE.Combat.TimeCore;
using ReE.Stats;
using UnityEngine;

namespace ReE.Combat.Effects
{
    public static class EffectRunner
    {
        public static List<EffectEvent> Execute(ActionIntent intent, CharacterRuntime actor, CharacterRuntime target, EffectContext context = null)
        {
            var events = new List<EffectEvent>();
            if (intent == null || intent.effects == null) return events;

            string actorId = actor != null ? BattleTimeManager.GetActorKey(actor) : (intent.actorId ?? "Unknown");
            string targetId = target != null ? BattleTimeManager.GetActorKey(target) : "Unknown";

            // Packet_004.5: Check if context was injected externally
            bool isExternalContext = (context != null);

            // Packet_004.2: EffectContext DI Skeleton
            string ctxLog = "";
            if (ReEFeatureFlags.EnablePacket004_2_EffectContextDI)
            {
                if (context == null) context = new EffectContext();

                // Populate Context logic (FillIfMissing)
                // Use HasObservationLevel to check if missing. (Refined P004.5)
                if (!context.HasObservationLevel)
                {
                    // Use canonical key (Hotfix 003.2a logic)
                    string qKey = targetId; 
                    if (target != null) qKey = BattleTimeManager.GetActorKey(target);

                    if (ResearchNoteManager.Instance != null && ResearchNoteManager.Instance.TryGetObservationLevel(qKey, out int ctxObsLv))
                    {
                        context.SetObservationLevel(ctxObsLv);
                    }
                }

                ctxLog = $" [P042 Ctx(Knowledge=ObsLv{context.ObservationLevel}, AttrHooks={context.AttributeHooks.Count}, JobHooks={context.JobHooks.Count})]";
            }

            // Packet_004.5: Context External Injection Log
            string injectLog = "";
            if (ReEFeatureFlags.EnablePacket004_5_ContextExternalInjection && isExternalContext)
            {
                injectLog = " [P045 FromCaller(Chk=OK)]";
            }

            // Packet_004.6: Context Payload Skeleton Log
            string payloadLog = "";
            if (ReEFeatureFlags.EnablePacket004_6_ContextPayloadSkeleton && context != null && context.BattleState != null)
            {
                var st = context.BattleState;
                string t = st.TurnIndex.HasValue ? st.TurnIndex.Value.ToString() : "?";
                string w = !string.IsNullOrEmpty(st.WeatherId) ? st.WeatherId : "?";
                string s = st.EncounterSeed.HasValue ? st.EncounterSeed.Value.ToString() : "?";
                payloadLog = $" [P046 CtxState(Turn={t},Weather={w},Seed={s})]";
            }

            // Packet_004.7: EffectContext Builder Log
            string builderLog = "";
            if (ReEFeatureFlags.EnablePacket004_7_EffectContextBuilder)
            {
                builderLog = " [P047 CtxBuilder(Chk=OK)]";
            }

            // Packet_006.0: Source of Truth Log
            string sotLog = "";
            if (ReEFeatureFlags.EnablePacket006_0_BattleStateSotScaffold && context != null && context.BattleState != null && !string.IsNullOrEmpty(context.BattleState.SourceId))
            {
                 sotLog = $" [P060 Sot={context.BattleState.SourceId}]";
            }

            // Packet_004.3: Hook Priority Skeleton (SAFE)
            string prioLog = "";
            if (ReEFeatureFlags.EnablePacket004_3_HookPrioritySkeleton)
            {
                if (context == null) context = new EffectContext();

                // Sort AttributeHooks (SAFE: Treat non-IOrderedHook as order 0)
                // Note: Modifying the list in place or re-assigning? Re-assigning new sorted list is safer for local logic,
                // but since this is "Skeleton" and we want to persist order, modifying context list is correct.
                // However, Context is passed by reference.
                // Sorting logic:
                if (context.AttributeHooks == null) context.AttributeHooks = new List<IAttributeHook>();
                var sortedAttr = new List<IAttributeHook>(context.AttributeHooks);
                sortedAttr.Sort((a, b) => 
                {
                    int oa = (a is IOrderedHook oha) ? oha.ExecutionOrder : 0;
                    int ob = (b is IOrderedHook ohb) ? ohb.ExecutionOrder : 0;
                    return oa.CompareTo(ob);
                });
                context.AttributeHooks = sortedAttr; // Apply sorted list back

                if (context.JobHooks == null) context.JobHooks = new List<IJobHook>();
                var sortedJob = new List<IJobHook>(context.JobHooks);
                sortedJob.Sort((a, b) => 
                {
                    int oa = (a is IOrderedHook oha) ? oha.ExecutionOrder : 0;
                    int ob = (b is IOrderedHook ohb) ? ohb.ExecutionOrder : 0;
                    return oa.CompareTo(ob);
                });
                context.JobHooks = sortedJob;

                // Logging
                string attrOrders = string.Join(",", sortedAttr.ConvertAll(h => (h is IOrderedHook oh) ? oh.ExecutionOrder.ToString() : "0"));
                string jobOrders = string.Join(",", sortedJob.ConvertAll(h => (h is IOrderedHook oh) ? oh.ExecutionOrder.ToString() : "0"));
                
                // Format: [P043 HookOrder(Attr=[...], Job=[...])]
                prioLog = $" [P043 HookOrder(Attr=[{attrOrders}], Job=[{jobOrders}])]";
            }

            foreach (var eff in intent.effects)
            {
                var evt = new EffectEvent
                {
                    actorId = actorId,
                    targetId = targetId,
                    kind = eff.kind,
                    amount = eff.basePower, // MVP: Logic = basePower (No calculation)
                    attributeTag = eff.attributeTag,
                    debugNote = "MVP"
                };

                // Packet_003: Observation Accuracy & Element Hooks (Non-destructive injection)
                if (ReEFeatureFlags.EnableObservationAndElementHooks)
                {
                    // 1. Observation Accuracy (Packet_003.2)
                    int obsLv = -1; 
                    bool obsSuccess = false;

                    // Try to get Observation Level (Read-Only inference)
                    // Hotfix 003.2a: Use canonical key from actor runtime if available
                    string queryKey = targetId;
                    if (target != null) queryKey = BattleTimeManager.GetActorKey(target);

                    // Try to get Observation Level (Read-Only inference)
                    if (ResearchNoteManager.Instance != null)
                    {
                        obsSuccess = ResearchNoteManager.Instance.TryGetObservationLevel(queryKey, out obsLv);
                    }
                    else
                    {
                        // Instance null = cannot determine
                        obsSuccess = false;
                    }

                    // No numeric change allowed in this packet.
                    float accuracy = 1.0f; 
                    
                    if (evt.debugNote == null) evt.debugNote = "";

                    string lvStr = obsSuccess ? obsLv.ToString() : "?";
                    evt.debugNote += $" [P003 ObsLv={lvStr} Acc={accuracy:F2} Hook=Noop]";

                    // 2. Elemental Hook (Stub) - No-Op
                }

                // Packet_004: Multi-stage Multiplier Skeleton (C-08-1)
                if (ReEFeatureFlags.EnablePacket004_MultistageMultiplierSkeleton)
                {
                    // Non-destructive Skeleton: No calculation, only logging.
                    // Steps: Dist, Fatigue, Guard, Attr, Obs
                    
                    string stepsLog = "(Dist=?,Fatigue=?,Guard=?,Attr=?,Obs=?)";

                    // Packet_004.1: Candidate Values Visualization
                    if (ReEFeatureFlags.EnablePacket004_MultistageMultiplierCandidateValues)
                    {
                        // 1. Dist (Vector3.Distance)
                        string distStr = "?";
                        if (actor != null && target != null && actor.transform != null && target.transform != null)
                        {
                            float d = Vector3.Distance(actor.transform.position, target.transform.position);
                            distStr = $"1.00({d:F1}m)";
                        }
                        
                        // 2. Obs (ResearchNoteManager)
                        string obsStr = "?";
                        // Re-use queryKey logic from P003
                        string qKey = targetId; 
                        if (target != null) qKey = BattleTimeManager.GetActorKey(target);

                        if (ResearchNoteManager.Instance != null && ResearchNoteManager.Instance.TryGetObservationLevel(qKey, out int oLv))
                        {
                            obsStr = $"1.00(Lv{oLv})";
                        }
                        else
                        {
                            obsStr = "1.00(Unk)"; // Safe fallback
                        }

                        // 3. Attr (Stub)
                        string attrStr = "1.00(Noop)";

                        // 4. Fatigue/Guard (Unknown for now)
                        string fatStr = "?";
                        string grdStr = "?";

                        stepsLog = $"(Dist={distStr},Fatigue={fatStr},Guard={grdStr},Attr={attrStr},Obs={obsStr})";
                    }

                    if (evt.debugNote == null) evt.debugNote = "";
                    evt.debugNote += $" [P004 Base={evt.amount} Mult=1.00 Steps={stepsLog}]";
                }

                // Packet_004.2: Append Context Log
                if (ReEFeatureFlags.EnablePacket004_2_EffectContextDI)
                {
                    evt.debugNote += ctxLog;
                }

                // Packet_004.3: Append Priority Log
                if (ReEFeatureFlags.EnablePacket004_3_HookPrioritySkeleton)
                {
                    evt.debugNote += prioLog;
                }

                // Packet_004.4: Observed Value Skeleton (C-15, SAFE)
                if (ReEFeatureFlags.EnablePacket004_4_ObservedValueSkeleton)
                {
                    var info = new EffectObservationInfo();
                    
                    // 1. Actual
                    int actualVal = (int)evt.amount; // MVP assumes int base
                    info.ActualAmount = actualVal;

                    // 2. ObsLv (Determine Lv for this specific calc)
                    // If Context DI (P042) is used, use it. Otherwise re-evaluate.
                    int myObsLv = 0; // Default Unseen
                    if (context != null)
                    {
                        myObsLv = context.ObservationLevel;
                    }
                    else
                    {
                         // Fallback re-query if context not present (though P042 creates it internally if ON)
                         // But if P042 is OFF and P044 ON, we need to query.
                         string qKey = targetId; 
                         if (target != null) qKey = BattleTimeManager.GetActorKey(target);
                         if (ResearchNoteManager.Instance != null && ResearchNoteManager.Instance.TryGetObservationLevel(qKey, out int lv))
                         {
                             myObsLv = lv;
                         }
                    }
                    info.ObservedLevel = myObsLv;

                    // 3. Observed Text
                    if (myObsLv >= 1)
                    {
                        info.ObservedAmountText = actualVal.ToString();
                    }
                    else
                    {
                        info.ObservedAmountText = "???";
                    }

                    evt.observationInfo = info;
                    
                    evt.debugNote += $" [P044 Obs(Actual={info.ActualAmount}, Observed=\"{info.ObservedAmountText}\", Lv={info.ObservedLevel})]";
                }

                // Packet_004.8: Context Log Router Logic
                if (ReEFeatureFlags.EnablePacket004_8_ContextLogRouter)
                {
                    // P045
                    if (ReEFeatureFlags.EnablePacket004_5_ContextExternalInjection)
                    {
                        evt.debugNote = ContextLogRouter.AppendTagOnce(evt.debugNote, injectLog);
                    }
                    // P046
                    if (ReEFeatureFlags.EnablePacket004_6_ContextPayloadSkeleton)
                    {
                        evt.debugNote = ContextLogRouter.AppendTagOnce(evt.debugNote, payloadLog);
                    }
                    // P047
                    if (ReEFeatureFlags.EnablePacket004_7_EffectContextBuilder)
                    {
                        evt.debugNote = ContextLogRouter.AppendTagOnce(evt.debugNote, builderLog);
                    }
                    // P060
                    if (ReEFeatureFlags.EnablePacket006_0_BattleStateSotScaffold)
                    {
                         evt.debugNote = ContextLogRouter.AppendTagOnce(evt.debugNote, sotLog);
                    }
                }
                else
                {
                    // Legacy Direct Append (Fallthrough)
                    
                    // Packet_004.5: Append External Injection Log
                    if (ReEFeatureFlags.EnablePacket004_5_ContextExternalInjection)
                    {
                        evt.debugNote += injectLog;
                    }

                    // Packet_004.6: Append Payload Log
                    if (ReEFeatureFlags.EnablePacket004_6_ContextPayloadSkeleton)
                    {
                        evt.debugNote += payloadLog;
                    }

                    // Packet_004.7: Append Builder Log
                    if (ReEFeatureFlags.EnablePacket004_7_EffectContextBuilder)
                    {
                        evt.debugNote += builderLog;
                    }

                    // Packet_006.0: Append Sot Log
                    if (ReEFeatureFlags.EnablePacket006_0_BattleStateSotScaffold)
                    {
                        evt.debugNote += sotLog;
                    }
                }

                events.Add(evt);
            }

            return events;
        }
    }
}
