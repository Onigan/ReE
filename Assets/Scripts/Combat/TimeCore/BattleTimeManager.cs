using ReE.Stats;
using ReE.Combat.Effects;
using ReE;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

namespace ReE.Combat.TimeCore
{
    public class BattleTimeManager : MonoBehaviour
    {
        [Header("TimeCore Unit-B Settings")]
        [SerializeField] private float _secondsPerTick = 0.5f; 
        private const int GUARD_TIMESHIFT_TICK = 1; // P0.8: TimeShift Award 

        // P0.9: Normal Attack Timing Baseline
        private const int PREP_TICKS = 1;
        private const int ACTION_TICKS = 1;
        private const int RECOVERY_TICKS = 1;
        private const int MOVE_CHECK_TICKS = 0; // Future
        private const int BASE_WAIT_TICKS = 0;  // Future

        // P1.2: MoveCheck MVP
        private const int MOVE_TICKS_MANUAL = 1;
        private const int MOVE_TICKS_AUTO = 2;
        private const int MELEE_RANGE_GRID = 2;
        [SerializeField] private bool _autoMoveFatigueEnabled = true;

        [Header("Windup (Seconds -> Ticks Internal)")] 

        [Header("Windup (Seconds -> Ticks Internal)")]
        [SerializeField] private float _windupSeconds = 0.6f; 
        private int _windupTicks;

        [Header("Result Line Delay (Seconds -> Ticks Internal)")]
        [SerializeField] private float _resultLineDelay = 0.0f;
        private int _resultLineDelayTicks;

        [Header("Guard Settings (Seconds -> Ticks Internal)")]
        [SerializeField] private float _guardDuration = 1.2f;
        private int _guardDurationTicks;

        [SerializeField] private float _perfectGuardWindow = 0.25f;
        private int _perfectGuardWindowTicks;

        [SerializeField] private float _guardDamageRate = 0.6f;
        [SerializeField] private float _perfectGuardDamageRate = 0.2f;

        [Header("Actors")]
        [SerializeField] private CharacterRuntime _playerActor;
        [SerializeField] private CharacterRuntime _enemyActor;
        [FormerlySerializedAs("Actors")][SerializeField] private List<CharacterRuntime> _actors = new List<CharacterRuntime>();
        
        // Packet 008.10R: Difficulty + No-Numbers Gate
        public enum Difficulty { Dev, Easy, Normal, Hard, Immersive, NoHUD, Hardcore }
        [Header("Difficulty Settings")]
        [SerializeField] private Difficulty _difficulty = Difficulty.Normal;
        [SerializeField] private bool _useDifficultyLinkedObservationMode = true;
        
        // Packet 008.11: Relief Suggestion
        [Header("Relief Settings")]
        [SerializeField] private double _reliefWindowGameSeconds = 63072000.0; // 2 years
        [SerializeField] private int _reliefTriggerCount = 10;
        private List<double> _deathHistoryGameTime = new List<double>();
        private bool _hasSuggestedRelief = false;
        private long _persistentSessionUt = 0; // For session-based tracking

        // Packet 008.9: Observation Mode
        public enum ObservationMode { Numeric, ObserveText }
        [Header("Observation Settings")]
        [SerializeField] private ObservationMode _observationMode = ObservationMode.Numeric;

        // Packet 008.10R Helper Properties
        private bool IsNoNumbersDifficulty => 
            _difficulty == Difficulty.Normal || 
            _difficulty == Difficulty.Hard || 
            _difficulty == Difficulty.Immersive || 
            _difficulty == Difficulty.NoHUD || 
            _difficulty == Difficulty.Hardcore;
        
        private ObservationMode EffectiveObservationMode => IsNoNumbersDifficulty ? ObservationMode.ObserveText : _observationMode;

        [Header("Data Source (Packet_007)")]
        [SerializeField] private ReE.Combat.Data.SkillDatabase _skillDB;

        public IReadOnlyList<CharacterRuntime> ActiveActors => _actors;
        
        // P0.6: Expose Player for UI Adapter
        public CharacterRuntime PlayerActor => _playerActor;

        // --- TimeCore Kernel State ---
        public const long UT_PER_TICK = 8;
        private long _battleGlobalTimeUt = 0; 
        private long _lastEventsTimeUt = 0;   

        private bool _turnToggleToEnemy = false; 

        private enum GuardState { None, Guard, Perfect }
        private struct GuardBuff
        {
            public long StartTimeUt;
            public int DurationTicks;
            public int PerfectWindowTicks;
        }
        private readonly Dictionary<CharacterRuntime, GuardBuff> _guardStates = new Dictionary<CharacterRuntime, GuardBuff>();

        private readonly Queue<QueuedLine> _queue = new Queue<QueuedLine>();

        public bool BattleEnded { get; private set; } = false;
        public string WinnerName { get; private set; } = "";
        
        // Unit-F: BattleOutcome
        public enum BattleOutcome { Ongoing, Victory, Retreat, Death, Down }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
        public string BattleResult { get; private set; } = "";

        // Unit-K: Observation Token
        // Moved to ResearchNoteManager.cs
        public List<ObservationToken> ObservationTokens = new List<ObservationToken>();

        // Unit-O: Persistence Guard
        private bool _savedOnce = false;

        // ...

        private void SaveTokens()
        {
            if (_savedOnce) return;
            _savedOnce = true;
            if (ResearchNoteManager.Instance != null)
            {
                ResearchNoteManager.Instance.AppendTokens(ObservationTokens);
            }
        }

        // Unit-M: Debuff
        public enum DebuffType { None, Panic, Slow, Trip }
        private Dictionary<CharacterRuntime, List<DebuffType>> _debuffs = new Dictionary<CharacterRuntime, List<DebuffType>>();
        
        // Unit-R: Unique Display Names
        private Dictionary<CharacterRuntime, string> _uniqueDisplayNames = new Dictionary<CharacterRuntime, string>();

        private struct QueuedLine
        {
            public long eventTimeUt; 
            public string text;
            public Action onDequeue;
            public QueuedLine(long t, string txt, Action a = null) { eventTimeUt = t; text = txt; onDequeue = a; }
        }

        private void Awake()
        {
            if (_secondsPerTick <= 0.001f) _secondsPerTick = 0.5f; 
            _windupTicks = Mathf.CeilToInt(_windupSeconds / _secondsPerTick);
            _resultLineDelayTicks = Mathf.CeilToInt(_resultLineDelay / _secondsPerTick);
            _guardDurationTicks = Mathf.CeilToInt(_guardDuration / _secondsPerTick);
            _perfectGuardWindowTicks = Mathf.CeilToInt(_perfectGuardWindow / _secondsPerTick);
        }

        private void Start()
        {
            ApplyDifficultySettings(); // Packet 008.10

            // Actor Discovery
            bool pFound = (_playerActor != null);
            bool eFound = (_enemyActor != null);

            if (!pFound || !eFound)
            {
                if (_actors == null || _actors.Count == 0)
                {
#if UNITY_2023_1_OR_NEWER
                    var found = UnityEngine.Object.FindObjectsByType<CharacterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                    var found = FindObjectsOfType<CharacterRuntime>();
#endif
                    if (found != null) _actors.AddRange(found);
                }
                if (!pFound) { _playerActor = FindActorByName("Player") ?? FindActorByName("Hero"); pFound = (_playerActor != null); }
                if (!eFound) { _enemyActor = FindActorByName("Enemy") ?? FindActorByName("Boss") ?? FindActorByName("Monster"); eFound = (_enemyActor != null); }
            }

            if (!pFound && _actors.Count > 0) _playerActor = _actors[0];
            if (!eFound) {
                if (_actors.Count > 1) _enemyActor = _actors[1];
                else if (_actors.Count > 0 && _actors[0] != _playerActor) _enemyActor = _actors[0]; 
            }

            if (_playerActor != null && !_actors.Contains(_playerActor)) _actors.Add(_playerActor);
            if (_enemyActor != null && !_actors.Contains(_enemyActor)) _actors.Add(_enemyActor);
            
            // P1.2 Init GridPos (MVP)
            if (_playerActor != null) _playerActor.GridPos = Vector3Int.zero; 
            if (_enemyActor != null) _enemyActor.GridPos = new Vector3Int(0, 0, 4); // Distance 4 (Medium/Close border)

            // Unit-R: Initialize Unique Names (Format A)
            InitializeUniqueNames();
        }
        
        private void InitializeUniqueNames()
        {
            _uniqueDisplayNames.Clear();
            var nameCounts = new Dictionary<string, int>();

            // Pass 1: Count names
            foreach (var a in _actors)
            {
                if (a == null) continue;
                string baseName = GetBaseDisplayName(a);
                if (!nameCounts.ContainsKey(baseName)) nameCounts[baseName] = 0;
                nameCounts[baseName]++;
            }

            // Pass 2: Assign names
            var nameIndex = new Dictionary<string, int>();
            foreach (var a in _actors)
            {
                if (a == null) continue;
                string baseName = GetBaseDisplayName(a);
                
                if (nameCounts[baseName] > 1)
                {
                    if (!nameIndex.ContainsKey(baseName)) nameIndex[baseName] = 0;
                    int idx = nameIndex[baseName];
                    string suffix = GetAlphaSuffix(idx); // A, B, C...
                    _uniqueDisplayNames[a] = $"{baseName}{suffix}";
                    nameIndex[baseName]++;
                }
                else
                {
                    _uniqueDisplayNames[a] = baseName;
                }
            }
        }

        private string GetAlphaSuffix(int index)
        {
            // 0->A, 1->B ... 25->Z, 26->AA? For Alpha, simple A-Z is enough.
            if (index < 0) return "";
            if (index < 26) return ((char)('A' + index)).ToString();
            return (index + 1).ToString(); // Fallback for huge numbers
        }

        private CharacterRuntime FindActorByName(string keyword)
        {
            foreach (var a in _actors) {
                if (a == null) continue;
                if (a.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return a;
                if (GetActorKey(a).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return a;
            }
            return null;
        }

        public bool TryDequeue(out float delaySeconds, out string text)
        {
            if (_queue.Count == 0 && _turnToggleToEnemy)
            {
                if (!BattleEnded) EnqueueEnemyTurn();
                _turnToggleToEnemy = false;
            }

            if (_queue.Count == 0)
            {
                delaySeconds = 0f; text = null; return false;
            }

            var q = _queue.Dequeue();
            long deltaUt = q.eventTimeUt - _battleGlobalTimeUt;
            if (deltaUt < 0) deltaUt = 0; 

            _battleGlobalTimeUt = q.eventTimeUt; 

            double secondsPerUt = _secondsPerTick / (double)UT_PER_TICK;
            delaySeconds = (float)(deltaUt * secondsPerUt);

            text = q.text;
            q.onDequeue?.Invoke();

            return true;
        }

        public int QueueCount => _queue.Count;
        public bool IsBusy => _queue.Count > 0;
        
        // Packet_008.3: Debug Auto-Resolve
        public void DebugProcessUntilIdle(int safety = 64)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int count = 0;
            while (IsBusy)
            {
                if (count >= safety)
                {
                    Debug.LogWarning($"[BTM] DebugProcessUntilIdle: Loop safety limit reached ({safety}). Aborting.");
                    break;
                }

                if (TryDequeue(out float delay, out string text))
                {
                    // Invoke logic done by TryDequeue.
                    // Log for visibility in Console since UI isn't consuming it
                    Debug.Log($"[BTM-Debug] {text}");
                }
                count++;
            }
#endif
        }
        
        // --- Unit-G/J: Intent Entry ---
        public void EnqueueIntent(ActionIntent intent)
        {
            if (intent == null) return;

            // Packet_008.2: Intent Log (User Request)
            string uiMainStr = "None";
            if (!string.IsNullOrEmpty(intent.libraryId) && _skillDB != null)
            {
                var s = _skillDB.FindById(intent.libraryId);
                if (s != null) uiMainStr = s.uiMain.ToString();
            }
            string targetStr = (intent.targetIds.Count > 0) ? intent.targetIds[0] : "None";
            string freeTextLog = string.IsNullOrEmpty(intent.freeTextMeta) ? "" : $" freeText='{intent.freeTextMeta}'";
            EnqueueLine(0, $"[Intent] type={intent.type} uiMain={uiMainStr} skill={intent.libraryId} target={targetStr}{freeTextLog}");


            // Packet_007.4: Time Progression (Action-Based MVP)
            if (ReEFeatureFlags.EnablePacket007_4_BuffEffectMinimal)
            {
                TickAllEffects(5.0f); // 5 seconds logical step per action
            }

            // Packet_008.4: IntentType Dispatch
            // Replaces string-based intentType check and partial feature flags
            
            switch (intent.type)
            {
                case IntentType.NormalAttack:
                    {
                        string t = (intent.targetIds.Count > 0) ? intent.targetIds[0] : "Enemy";
                        EnqueuePlayerNormalAttack("Player", t, intent);
                    }
                    break;

                case IntentType.Defend:
                    EnqueuePlayerGuard();
                    break;

                case IntentType.Skill:
                case IntentType.Magic:
                    {
                        if (_skillDB == null)
                        {
                            EnqueueLine(0, "[Intent] SkillDB is null");
                            return;
                        }
                        
                        var skill = _skillDB.FindById(intent.libraryId);
                        if (skill == null)
                        {
                            EnqueueLine(0, $"[Intent] skill not found id={intent.libraryId}");
                            // Fallback to Root? intent implies we already committed. 
                            // Just log and do nothing (end turn essentially, or returns to idle).
                            return; 
                        }

                        // Packet_007.3: Policy Check (Moved here)
                        if (ReEFeatureFlags.EnablePacket007_3_TargetSideMinimal)
                        {
                             // Logic from previous implementation
                             // We don't modify intent.targetSide logic deep here, but we can check policy.
                             // Actually, just pass to EnqueueSkill?
                             // Previous logic mutated intent.targetSide.
                             if (skill.targetPolicy == ReE.Combat.Data.TargetPolicy.EnemyOnly && intent.targetSide == ReE.Combat.Data.TargetSide.Ally)
                             {
                                 EnqueueLine(0, "[P073 TargetPolicyBlock(EnemyOnly)]");
                                 intent.targetSide = ReE.Combat.Data.TargetSide.Enemy;
                             }
                             else if (skill.targetPolicy == ReE.Combat.Data.TargetPolicy.AllyOnly && intent.targetSide == ReE.Combat.Data.TargetSide.Enemy)
                             {
                                 EnqueueLine(0, "[P073 TargetPolicyBlock(AllyOnly)]");
                                 intent.targetSide = ReE.Combat.Data.TargetSide.Ally; 
                             }
                        }

                        EnqueueSkill(intent, skill);
                    }
                    break;

                case IntentType.Observe:
                    // Unit-K
                    EnqueueObserve(intent.actorId, intent.targetIds);
                    break;
                
                case IntentType.Retreat:
                    TryRequestRetreat();
                    break;
                
                case IntentType.Move: // P1.2
                    {
                        string t = (intent.targetIds.Count > 0) ? intent.targetIds[0] : "Enemy";
                        EnqueueMove("Player", t);
                    }
                    break;

                default:
                    EnqueueLine(0, $"[Intent] unhandled type={intent.type}");
                    break;
            }
        }

        // Packet_007.2: Skill Execution Entry
        public void EnqueueSkill(ActionIntent intent, ReE.Combat.Data.SkillData skill)
        {
             if (intent == null || skill == null) return;

             // Packet_008.4: Handle Damage Skills
             // Delegate to Normal Attack Logic (which handles basePower override via libraryId)
             if (skill.kind == ReE.Combat.Data.SkillKind.Damage)
             {
                 string t = (intent.targetIds.Count > 0) ? intent.targetIds[0] : "Enemy";
                 EnqueuePlayerNormalAttack("Player", t, intent);
                 return;
             }

             var actor = ResolveActorRuntime("Player");
             var actorDisp = GetDisplayName(actor);

             // Packet_008.5: Field Gating
             // Only access Buff/Reactive fields if logic requires them.

             if (skill.kind == ReE.Combat.Data.SkillKind.Heal)
             {
                 EnqueueLine(0, $"[P072 SkillHeal(Id={skill.skillId},Amt={skill.basePower})]");
                 EnqueueLine(0, $"{actorDisp}の{skill.displayName}！");

                 EnqueueLine(_windupTicks, "...", () => {
                     var s = ResolveTargetStatus(intent);
                     if (s != null)
                     {
                         if (ReEFeatureFlags.EnablePacket007_5_ReactiveMinimal)
                         {
                             int applied; string note;
                             // Packet_008.5: Reactive Gating (grantsReactive check inside logic? No, check here)
                             // Wait, TryApplyHeal_WithReactive logic assumes reactive interaction check is internal to Status.
                             // But does the SKILL apply a reactive effect? Heal usually doesn't APPLY a buff,
                             // it triggers reactive effects ON the target. 
                             // So gating 'grantsReactive' for Heal is irrelevant unless Heal ITSELF grants a side-effect.
                             // Current spec: Heal acts as 'Heal' event. Target's existing Reactives react to it.
                             // So we don't pass skill.reactiveXXX here.
                             
                             if (s.TryApplyHeal_WithReactive(skill.basePower, out applied, out note, skill.skillId))
                             {
                                  EnqueueLine(_resultLineDelayTicks, $"{s.DisplayName}は回復した！{BuildHpText(s)}");
                             }
                             else
                             {
                                  if (note == "InvertedToDamage") EnqueueLine(_resultLineDelayTicks, $"{s.DisplayName}はダメージを受けた！{BuildHpText(s)}");
                                  else EnqueueLine(_resultLineDelayTicks, "しかし効果はかき消された！");
                             }
                         }
                         else
                         {
                             s.ApplyHeal(skill.basePower);
                             EnqueueLine(_resultLineDelayTicks, $"{s.DisplayName}は回復した！{BuildHpText(s)}");
                         }
                     }
                 });
             }
             else if (skill.kind == ReE.Combat.Data.SkillKind.Buff)
             {
                 EnqueueLine(0, $"{actorDisp}の{skill.displayName}！");
                 
                 EnqueueLine(_windupTicks, "...", () => {
                     var s = ResolveTargetStatus(intent);
                     if (s != null)
                     {
                         var eff = new ReE.Combat.Effects.ActiveEffect {
                             effectId = skill.skillId,
                             sourceSkillId = skill.skillId
                         };
                         
                         // Packet_008.5: Strict Gating for Reactive / Buff
                         
                         if (ReEFeatureFlags.EnablePacket007_5_ReactiveMinimal && skill.grantsReactive)
                         {
                             // User Request: Explicit log for skipping standard buff
                             EnqueueLine(0, $"[Buff] Skipped (ReactiveOnly): {skill.skillId}");
                             
                             // Packet_008.5: Combat Feel Log (Reactive Granted)
                             EnqueueLine(0, $"[Reactive] Granted: skillId={skill.skillId} type={skill.reactiveType} scope={skill.reactiveScope}");

                             eff.isReactive = true;
                             eff.reactiveType = skill.reactiveType;
                             eff.reactiveMagnitude = skill.reactiveMagnitude;
                             eff.reactiveScope = skill.reactiveScope;
                             eff.durationSeconds = skill.reactiveDurationSeconds;
                             eff.stackRule = skill.reactiveStackRule;
                         }
                         else
                         {
                             // Packet_008.5: Explicit Buff Type Gating
                             if (skill.buffType == ReE.Combat.Effects.EffectType.None)
                             {
                                 EnqueueLine(0, $"[P074 Warning: Skill '{skill.skillId}' is kind=Buff but buffType is None. Skipped.]");
                                 return; // Abort apply
                             }

                             eff.type = skill.buffType;
                             eff.magnitude = skill.buffMagnitude;
                             eff.tag = skill.buffTag;
                             eff.durationSeconds = skill.buffDurationSeconds;
                             eff.stackRule = skill.buffStackRule;
                             eff.remainingSeconds = skill.buffDurationSeconds; 
                         }
                         
                         if (!eff.isReactive) eff.remainingSeconds = eff.durationSeconds; 
                         else eff.remainingSeconds = eff.durationSeconds;

                         bool applied = true;
                         string note = "";

                         if (ReEFeatureFlags.EnablePacket007_5_ReactiveMinimal)
                         {
                             applied = s.TryApplyEffect_WithReactive(eff, out note, skill.skillId);
                         }
                         else
                         {
                             s.ApplyEffect(eff);
                         }

                         if (applied)
                         {
                             if (eff.isReactive)
                             {
                                 // User Request: Explicit log for Reactive application
                                 EnqueueLine(_resultLineDelayTicks, $"[Reactive] Applied: {skill.skillId} (Type={eff.reactiveType}, Scope={eff.reactiveScope})");
                             }
                             else
                             {
                                 EnqueueLine(_resultLineDelayTicks, $"[P074 EffectApply(Type={eff.type},Id={eff.effectId},Dur={eff.durationSeconds},Mag={eff.magnitude},Tag={eff.tag})]");
                             }
                         }
                         else
                         {
                             EnqueueLine(_resultLineDelayTicks, "しかし効果はかき消された！");
                         }
                     }
                     else
                     {
                         EnqueueLine(_resultLineDelayTicks, $"[P074 BuffApplyFailed(SkillId={skill.skillId},Fallback={skill.fallbackSkillId})]");
                     }
                 });
             }
        }
        
        // Packet_007.4: Tick Driver (Action-Based MVP)
        private void TickAllEffects(float dt)
        {
            if (_playerActor != null) GetStatus(_playerActor)?.TickEffects(dt);
            if (_enemyActor != null) GetStatus(_enemyActor)?.TickEffects(dt);
            // Verify _actors if consistent with above
        }
        
        // Packet_007.3: Target Resolution Helper
        private ReE.Stats.CharacterStatus ResolveTargetStatus(ActionIntent intent)
        {
             string tName = (intent.targetIds.Count > 0) ? intent.targetIds[0] : null;

             // Flag OFF or Enemy Side -> Standard Resolution
             if (!ReEFeatureFlags.EnablePacket007_3_TargetSideMinimal || intent.targetSide == ReE.Combat.Data.TargetSide.Enemy)
             {
                 if (string.IsNullOrEmpty(tName)) tName = "Enemy";
                 var runtime = ResolveActorRuntime(tName);
                 return GetStatus(runtime);
             }

             // Ally Side logic
             if (intent.targetSide == ReE.Combat.Data.TargetSide.Ally)
             {
                 CharacterRuntime runtime = null;
                 if (!string.IsNullOrEmpty(tName))
                 {
                      runtime = ResolveActorRuntime(tName);
                      // Reject if resolved to Enemy actor explicitly
                      if (runtime == _enemyActor) runtime = null; 
                 }

                 if (runtime == null)
                 {
                      EnqueueLine(0, "[P073 TargetFallback(Self)]");
                      runtime = _playerActor;
                 }
                 return GetStatus(runtime);
             }
             return null;
        }

        // Unit-K: Enqueue Observe
        public void EnqueueObserve(string actorName, List<string> targetNames)
        {
            var actor = ResolveActorRuntime(actorName);
            if (actor == null && _playerActor != null) actor = _playerActor;

            string targetName = (targetNames != null && targetNames.Count > 0) ? targetNames[0] : "Enemy";
            // Check if we can verify the target Runtime for better name?
            var targetActor = ResolveActorRuntime(targetNames != null && targetNames.Count > 0 ? targetNames[0] : "Enemy");

            string targetKey = "Enemy";
            string targetDisp = "Enemy";
            if (targetActor != null)
            {
                targetKey = GetActorKey(targetActor); // Base Key (e.g. Goblin)
                targetDisp = GetDisplayName(targetActor); // Unique Name (e.g. Goblin A)
            }

            var actorDisp = GetDisplayName(actor);

            EnqueueLine(0, $"【観察】{actorDisp}は{targetDisp}を注意深く観察した。", () => {
                 ObservationTokens.Add(new ObservationToken { 
                     TargetKey = targetKey, 
                     TargetDisplayName = targetDisp, 
                     ObservedAt = _battleGlobalTimeUt 
                 });
            });
            EnqueueLine(4, "..."); 
            _turnToggleToEnemy = true;
        }

        public void EnqueuePlayerNormalAttack(string attackerName = "Player", string targetName = "Enemy", ActionIntent intent = null)
        {
            // P0.6: Guard Exit on Attack Intent
            if (_playerActor != null && _playerActor.IsGuarding)
            {
                 _playerActor.ExitGuard("AttackChosen");
                 EnqueueLine(0, $"[Guard] {_uniqueDisplayNames[_playerActor]} exits guard (reason=AttackChosen)");
            }

            if (_playerActor != null && _enemyActor != null) EnqueueNormalAttack(_playerActor, _enemyActor, intent);
            else EnqueueBasicAttack(attackerName, targetName);
            _turnToggleToEnemy = true;
        }

        public void EnqueueEnemyTurn()
        {
            // Packet 008.8: AI Turn Weight (Log only, no real-time wait)
            EnqueueLine(0, "[Enemy] Thinking...");
            EnqueueLine(4, "...", () => {
                 if (_playerActor != null && _enemyActor != null) EnqueueNormalAttack(_enemyActor, _playerActor);
                 else EnqueueBasicAttack("Enemy", "Player");
            });
        }

        public void EnqueuePlayerGuard()
        {
            if (_playerActor != null) EnqueueGuard(_playerActor);
            else Debug.LogError("[BTM] PlayerGuard failed: PlayerActor is null.");
            _turnToggleToEnemy = true;
        }

        // Unit-F2/L/M/N: Retreat Chance & Penalty
        public bool TryRequestRetreat()
        {
            if (BattleEnded) return false;

            // Chance Calculation
            float baseChance = 0.70f;
            float chance = Mathf.Clamp(baseChance, 0.05f, 0.95f);

            bool success = UnityEngine.Random.value < chance;

            if (success)
            {
                EnqueueLine(0, "撤退の機会をうかがっている...");
                EnqueueLine(4, "...", () => {
                     BattleEnded = true;
                     Outcome = BattleOutcome.Retreat;
                     BattleResult = "Retreat";
                     SaveTokens(); // Unit-O
                     EnqueueLine(0, "[System] 撤退に成功した。");
                });
                return true;
            }
            else
            {
                EnqueueLine(0, "[System] 撤退に失敗した！");

                AddDebuff(_playerActor, DebuffType.Panic, 15.0f);
                EnqueueLine(0, $"{GetDisplayName(_playerActor)}は恐慌状態になった！");

                int interruptDelayTicks = 8; 
                EnqueueLine(interruptDelayTicks, "...");

                if (_enemyActor != null && _playerActor != null)
                {
                     EnqueueLine(0, "[System] 敵が隙を突いて割り込んだ！");
                     EnqueueNormalAttack(_enemyActor, _playerActor);
                }
                else
                {
                     EnqueueLine(0, "[System] 敵が隙を突いて割り込んだ！");
                     EnqueueBasicAttack("Enemy", "Player");
                }

                EnqueueLine(10, "逃げ場を探している...");

                return false;
            }
        }
        
        // Unit-M: Debuff Helper
        public void AddDebuff(CharacterRuntime target, DebuffType type, float duration)
        {
            if (target == null) return;
            if (!_debuffs.ContainsKey(target)) _debuffs[target] = new List<DebuffType>();
            _debuffs[target].Add(type);
        }

        public void EnqueueGuard(CharacterRuntime actor)
        {
            if (actor == null) return;
            // P0.6: Enter Guard
            // MVP: TurnKey = 1 (Fixed)
            actor.EnterGuard(1);
            
            long startTimeUt = GetNextScheduleBaseUt();
            _guardStates[actor] = new GuardBuff { StartTimeUt = startTimeUt, DurationTicks = _guardDurationTicks, PerfectWindowTicks = _perfectGuardWindowTicks };
            var disp = GetDisplayName(actor);
            EnqueueLine(0, $"{disp}は身構えた。");
            // Packet_008.5: Combat Feel Log (Guard Start)
            EnqueueLine(0, $"[Guard] {disp} enters guard until next turn");
        }

        public void EnqueueNormalAttack(CharacterRuntime attacker, CharacterRuntime target, ActionIntent intent = null, int retryCount = 0)
        {
            // Unit-N: No IsBusy check to allow interrupt piping

            if (attacker == null || target == null) { EnqueueLine(0, "[System] エラー：攻撃者または対象が不明です。"); return; }

            var atkStatus = GetStatus(attacker);
            var tgtStatus = GetStatus(target);
            if (atkStatus == null || tgtStatus == null || atkStatus.IsDead || tgtStatus.IsDead) return;

            var attackerDisp = GetDisplayName(attacker);
            var targetDisp = GetDisplayName(target);

            // Phase 1: Declaration (0 Tick)
            if (retryCount == 0) EnqueueLine(0, $"{attackerDisp}の攻撃！");

            // P1.2: Range Check & Auto-Move
            int startDist = CalculateGridDistance(attacker, target);
            // P1.3.1: Weapon Logic Integration
            var wpnCat = GetWeaponCategory(attacker);
            if (_weaponProfiles == null || !_weaponProfiles.ContainsKey(wpnCat)) InitializeWeaponProfiles(); // Safety
            var profile = _weaponProfiles[wpnCat];

            // 1. Impossible Check (Auto Step Back / Move)
            var startBand = GetRangeBandInternal(startDist);
            if (profile.IsImpossible(startBand))
            {
                // Simple MVP Strategy: If too close (PointBlank), StepBack.
                // If too far (Ranged), AutoMove is handled below by MELEE_RANGE_GRID check? 
                // Wait, MELEE_RANGE_GRID is simplistic (2). 
                // Weapon might want 12 (Bow).
                // MVP: If Impossible + (Dist < Optimal), StepBack (Increase Dist).
                // MVP: If Impossible + (Dist > Optimal), Advance (Decrease Dist).
                
                // For MVP, let's assume "Impossible" usually means "Too Close" for Long weapons, 
                // or "Too Far" (handled by AutoMove). 
                // Special case: Greatsword/Bow at PointBlank.
                if (startBand == RangeBand.PointBlank)
                {
                    // P1.3.2: Max Retry Check
                    if (retryCount >= 2)
                    {
                        EnqueueLine(0, $"[Weapon] 距離を作れない…（行動中断中断）");
                        return; // Abort
                    }

                    EnqueueLine(0, $"[Weapon] 武器は振れない…距離を作る (Impossible / {wpnCat})");
                    // Step Back (Increase Dist to 2)
                    int reqSteps = 1;
                    int moveCost = reqSteps * STEPBACK_TICKS; // Used Constant
                    EnqueueLine(moveCost, "...", () => {
                         // Step Back Logic (Inverse Move)
                         var dir = attacker.GridPos - target.GridPos; // Away
                         // Normalize
                         int dx=0, dz=0;
                         if (Mathf.Abs(dir.z) >= Mathf.Abs(dir.x)) dz = (target.GridPos.z > attacker.GridPos.z) ? -1 : 1;
                        
                         attacker.GridPos += new Vector3Int(dx, 0, dz);
                         
                         int endD = CalculateGridDistance(attacker, target);
                         string endCat = GetRangeCategory(endD);
                         EnqueueLine(0, $"[Range] {GetRangeCategory(startDist)} → {endCat} (StepBack)");
                         
                         // Restart Attack with Retry++
                         EnqueueNormalAttack(attacker, target, null, retryCount + 1);
                    });
                    return; // Abort this instance
                }
            }

            // Standard Melee Auto-Approach (Legacy + Weapon Aware?)
            // If Weapon is Bow, MELEE_RANGE_GRID (2) is bad. 
            // We should use Profile Optimal Range Max?
            // MVP: Stick to MELEE_RANGE_GRID logic for melee weapons. 
            // If Bow/Gun, skip Approach if within Ranged.
            bool skipApproach = false;
            if (wpnCat == WeaponDef.WeaponCategory.Bow || wpnCat == WeaponDef.WeaponCategory.Gun)
            {
                if (startDist <= 60 && startDist >= 2) skipApproach = true;
            }

            string startCat = GetRangeCategory(startDist); // Refresh
            
            if (!skipApproach && startDist > MELEE_RANGE_GRID)
            {
                int reqSteps = startDist - MELEE_RANGE_GRID;
                int moveCost = reqSteps * MOVE_TICKS_AUTO;
                
                EnqueueLine(0, $"[Move] {attackerDisp} closes distance (auto)");
                
                // Flavor B: Fatigue
                if (_autoMoveFatigueEnabled)
                {
                    EnqueueLine(0, $"{attackerDisp}は息が上がる...");
                }
                
                EnqueueLine(moveCost, "...", () => {
                     MoveTowards(attacker, target, reqSteps);
                     int endDist = CalculateGridDistance(attacker, target);
                     string endCat = GetRangeCategory(endDist);
                     if (startCat != endCat) EnqueueLine(0, $"[Range] {startCat} → {endCat}");
                });
            }

            // P0.9: Timing & TimeShift Calculation
            int basePreImpact = PREP_TICKS + ACTION_TICKS; // 1 + 1 = 2
            
            // P1.3.1: Weapon Profile Penalty
            // Re-calc band based on "assumed position after auto-move"? 
            // For MVP, we use startDist (or clamped) for calculation.
            // If Auto-Move happened, we are at MELEE_RANGE_GRID (2).
            int effectiveDist = (startDist > MELEE_RANGE_GRID && !skipApproach) ? MELEE_RANGE_GRID : startDist;
            var effectiveBand = GetRangeBandInternal(effectiveDist);
            
            if (profile.IsUnfavorable(effectiveBand))
            {
                EnqueueLine(0, $"[Weapon] 武器は取り回しが悪い… (Unfavorable / {wpnCat})");
                basePreImpact += 1;
            }
            // Contact Penalty
            if (attacker.CurrentContactState != CharacterRuntime.ContactState.None)
            {
                if (wpnCat == WeaponDef.WeaponCategory.Bow || wpnCat == WeaponDef.WeaponCategory.Gun || wpnCat == WeaponDef.WeaponCategory.Spear) {
                    EnqueueLine(0, $"[Weapon] {wpnCat}は狙いを付けられない… (Contact)");
                    basePreImpact += 2;
                }
                else if (wpnCat == WeaponDef.WeaponCategory.Knife) {
                     EnqueueLine(0, $"[Weapon] ナイフは鋭く動く (Contact Bonus)");
                     basePreImpact = Mathf.Max(1, basePreImpact - 1);
                }
            }

            int timeShift = attacker.PendingTimeShiftTick; // Clamped [-2, 0] by Runtime
            int actualPreImpact = Mathf.Max(1, basePreImpact + timeShift);
            
            // Clean up TimeShift
            if (timeShift < 0)
            {
                EnqueueLine(0, $"[TimeShift] {attackerDisp} acts fast! (PreImpact {basePreImpact}->{actualPreImpact})");
                attacker.PendingTimeShiftTick = 0;
            }

            // Distribute Split
            int actualAction = ACTION_TICKS; // Fixed 1
            int actualPrep = actualPreImpact - actualAction; // Remainder (0 or 1)
            
            // Phase 2: Preparation (Prep Ticks)
            if (actualPrep > 0)
            {
                EnqueueLine(actualPrep, $"{attackerDisp}は構える...");
            }

            // Phase 3: Action (Action Ticks) -> Execution
            EnqueueLine(actualAction, "...", () => 
            {
                if (intent == null) intent = new ActionIntent(); // Ensure intent exists
                if (intent.result != null && intent.result.executed) return; // Prevent double execution

                ExecuteAttackLogic(attacker, target, intent);

                // State Mutation
                if (tgtStatus != null && !tgtStatus.IsDead)
                {
                    tgtStatus.ApplyDamage(intent.result.finalDamage);
                }

                // P0.8: Apply TimeShift (Result Phase)
                if (intent.result != null && intent.result.timeShiftDeltaTick != 0)
                {
                    target.PendingTimeShiftTick += intent.result.timeShiftDeltaTick;
                }

                // Mark Executed
                if (intent.result != null) intent.result.executed = true;

                // Log Playback
                if (intent.result != null && intent.result.logs.Count > 0)
                {
                    foreach (var log in intent.result.logs)
                    {
                        EnqueueLine(0, log);
                    }
                }

                // Phase 4: Recovery (Recovery Ticks)
                EnqueueLine(RECOVERY_TICKS, $"{attackerDisp}は体勢を立て直している...");
                
                // Effect System (Legacy/Packet_001 Mix)
                 if (ReEFeatureFlags.EnableEffectMvp && intent != null)
                 {
                     // ... (Legacy Effect code, preserved as requested in P0.7.1)
                    // Packet_004.5: Context External Injection (Log-Only)
                    ReE.Combat.Effects.EffectContext ctx = null;

                    if (ReEFeatureFlags.EnablePacket004_7_EffectContextBuilder)
                    {
                        string weatherArg = null;
                        ctx = ReE.Combat.Effects.EffectContextBuilder.BuildForIntent(0, weatherArg);
                    }
                    else if (ReEFeatureFlags.EnablePacket004_5_ContextExternalInjection)
                    {
                        ctx = new ReE.Combat.Effects.EffectContext();
                        if (ReEFeatureFlags.EnablePacket004_6_ContextPayloadSkeleton)
                        {
                            ctx.BattleState = new ReE.Combat.Effects.BattleStateSnapshot
                            {
                                TurnIndex = null, 
                                WeatherId = null,
                                EncounterSeed = null
                            };
                        }
                    }

                    var events = EffectRunner.Execute(intent, attacker, target, ctx);
                    EffectApplier.Apply(events, attacker, target, ctx);

                    if (ReE.ReEFeatureFlags.EnableEffectFactLogBridge && events != null && events.Count > 0)
                    {
                        var factEvents = ReE.Combat.Logs.FactLogBuilder.FromEffectEvents(events);
                        if (factEvents != null)
                        {
                            foreach (var f in factEvents)
                            {
                                if (f == null) continue;
                                EnqueueLine(0, f.ToDebugLine());
                            }
                        }
                    }
                 }



                // Packet_001: Effect System (Conditional Insert)
                // Packet_001: Effect System (Conditional Insert)
                if (ReEFeatureFlags.EnableEffectMvp && intent != null)
                {
                    // Packet_004.5: Context External Injection (Log-Only)
                    // Packet_004.7: EffectContext Builder (Responsibility Consolidation)
                    ReE.Combat.Effects.EffectContext ctx = null;

                    if (ReEFeatureFlags.EnablePacket004_7_EffectContextBuilder)
                    {
                        // Packet_005.0: Pass dummy TurnIndex=0 (Log-Only)
                        // Packet_006.2: Pass Real-Source WeatherId if enabled
                        // Packet_006.2: Pass Real-Source WeatherId if enabled
                        string weatherArg = null; // Hotfix-P0: Force null to fix CS0103 (compile error) until 006.2 matches implementation
                        ctx = ReE.Combat.Effects.EffectContextBuilder.BuildForIntent(0, weatherArg);
                    }
                    else if (ReEFeatureFlags.EnablePacket004_5_ContextExternalInjection)
                    {
                        ctx = new ReE.Combat.Effects.EffectContext();
                        
                        // Packet_004.6: Context Payload Skeleton (Log-Only)
                        if (ReEFeatureFlags.EnablePacket004_6_ContextPayloadSkeleton)
                        {
                            ctx.BattleState = new ReE.Combat.Effects.BattleStateSnapshot
                            {
                                TurnIndex = null, // Fixed per user request
                                WeatherId = null,
                                EncounterSeed = null
                            };
                        }
                    }

                    var events = EffectRunner.Execute(intent, attacker, target, ctx);
                    EffectApplier.Apply(events, attacker, target, ctx);

                    // Packet_002: Fact Log Bridge (Safe Insert)
                    if (ReE.ReEFeatureFlags.EnableEffectFactLogBridge && events != null && events.Count > 0)
                    {
                        var factEvents = ReE.Combat.Logs.FactLogBuilder.FromEffectEvents(events);
                        if (factEvents != null)
                        {
                            foreach (var f in factEvents)
                            {
                                if (f == null) continue;
                                EnqueueLine(0, f.ToDebugLine());
                            }
                        }
                    }
                }

                var hpText = BuildHpText(tgtStatus);
                EnqueueLine(_resultLineDelayTicks, FormatDamageLog(targetDisp, intent.result.finalDamage, hpText, attackerDisp, tgtStatus.MaxHP));

                if (tgtStatus.IsDead)
                {
                    // Hotfix: HP Check Visualization
                    string hpSource = (tgtStatus.Runtime != null) ? "Runtime" : "Legacy";
                    EnqueueLine(0, $"[HPCheck] targetHP={tgtStatus.CurrentHP} (source={hpSource})");
                    
                    EnqueueLine(0, $"{targetDisp} は倒れた。");
                    EndBattle(attacker.name);
                }
            });
        }

        public void EnqueueBasicAttack(string attackerName, string targetName)
        {
            // if (IsBusy) return;
            var atkRuntime = ResolveActorRuntime(attackerName);
            var tgtRuntime = ResolveActorRuntime(targetName);

            if (atkRuntime == null || tgtRuntime == null) { EnqueueLine(0, $"[System] 参加者不明: {attackerName} / {targetName}"); return; }

            var atk = GetStatus(atkRuntime);
            var tgt = GetStatus(tgtRuntime);

            if (atk == null || tgt == null || atk.IsDead || tgt.IsDead) return;

            var attackerDisp = GetDisplayName(atkRuntime);
            var targetDisp = GetDisplayName(tgtRuntime);

            EnqueueLine(0, $"{attackerDisp}はじっくりと構える。");
            EnqueueLine(_windupTicks, "...", () => 
            {
                if (atk.IsDead || tgt.IsDead) return;
                int baseDmg = Mathf.Max(1, atk.ATK - tgt.DEF);
                int finalDmg = CalculateFinalDamage(atkRuntime, tgtRuntime, baseDmg);
                tgt.ApplyDamage(finalDmg);
                var hpText = BuildHpText(tgt);
                EnqueueLine(_resultLineDelayTicks, FormatDamageLog(targetDisp, finalDmg, hpText, attackerDisp, tgt.MaxHP));
                if (tgt.IsDead) {
                    EnqueueLine(0, $"{targetDisp} は倒れた。");
                    EndBattle(attackerName);
                }
            });
        }

        private long GetNextScheduleBaseUt()
        {
            long baseUt = _lastEventsTimeUt;
            if (_battleGlobalTimeUt > baseUt) baseUt = _battleGlobalTimeUt;
            return baseUt;
        }

        private void EnqueueLine(int delayTicks, string text, Action action = null)
        {
            // Packet_010: Log Layer Router Skeleton (C-10-1)
            if (ReEFeatureFlags.EnablePacket010_LogLayerRouter)
            {
                // Router Logic: Treat 'text' as Facts layer.
                // Interp/Narr layers are currently empty (Noop).
                string factsLine = text;
                // string interpLine = ""; 
                // string narrLine = "";
                
                // For now, pass factsLine through directly to maintain exact behavior.
                text = factsLine;
            }

            long baseUt = GetNextScheduleBaseUt();
            long scheduleUt = baseUt + (delayTicks * UT_PER_TICK);
            _lastEventsTimeUt = scheduleUt; 
            _queue.Enqueue(new QueuedLine(scheduleUt, text, action));
        }

        private int CalculateFinalDamage(CharacterRuntime attacker, CharacterRuntime target, int baseDamage)
        {
            // Purity Fix (P0.7.1): Side-effects removed. 
            // - Guard calculation delegated to ExecuteAttackLogic (P0.6 Logic).
            // - Logs moved to ExecuteAttackLogic or caller.
            
            if (target == null) return baseDamage;
            float multiplier = 1.0f;

            // Legacy _guardStates logic removed to prevent double application.
            // P0.6 Guard (Runtime.IsGuarding) is handled in ExecuteAttackLogic.

            int final = Mathf.FloorToInt(baseDamage * multiplier);
            return Mathf.Max(1, final); // Minimum 1 damage RULE
        }

        private void EndBattle(string winnerName)
        {
            if (BattleEnded) return;
            BattleEnded = true;
            WinnerName = winnerName;
            
            // Unit-F: Determine Outcome
            if (winnerName == "Player")
            {
                Outcome = BattleOutcome.Victory;
            }
            else
            {
                 // Player Defeated -> Down (Unit-F)
                 Outcome = BattleOutcome.Down;
                 // Packet 008.11: Relief Hook
                 RecordDeathEvent_GameTime();
            }

            BattleResult = Outcome.ToString();
            SaveTokens(); // Unit-O
            EnqueueLine(0, $"[System] 戦闘終了：結果={Outcome}");
            
            if (Outcome == BattleOutcome.Down)
            {
                 EnqueueLine(0, "[System] Processing P0...");
            }
        }

        // Packet 008.8: Debug Reset
        public void DebugResetBattle()
        {
            // Packet 008.11: Persistence Update
            _persistentSessionUt += _battleGlobalTimeUt;

            BattleEnded = false;
            Outcome = BattleOutcome.Ongoing;
            BattleResult = "";
            _queue.Clear();
            _battleGlobalTimeUt = 0;
            _lastEventsTimeUt = 0;
            _turnToggleToEnemy = false;
            _guardStates.Clear();
            _debuffs.Clear();
            ObservationTokens.Clear();
            _savedOnce = false;

            // Note: _deathHistoryGameTime and _hasSuggestedRelief are NOT cleared.

            foreach (var a in _actors)
            {
                GetStatus(a)?.ResetBattleHP();
            }
            EnqueueLine(0, "[Debug] 再戦用リセット（物語上の転生ではない）");
        }

        // Packet 008.8 Refinement (Updated for 008.10R): Damage Log Helper/Gate
        private string FormatDamageLog(string targetName, int damage, string hpText, string attackerName = null, int maxHP = 1)
        {
             // Packet 008.10R Check
             if (EffectiveObservationMode == ObservationMode.Numeric)
             {
                 if (attackerName != null) return $"{targetName}は{attackerName}に{damage}ダメージ！{hpText}";
                 return $"{targetName}に{damage}ダメージ！{hpText}";
             }
             else // ObserveText
             {
                 if (damage <= 0) return $"{targetName}は攻撃を完全に防いだ！"; // or "効いていない"

                 float ratio = 0f;
                 if (maxHP > 0) ratio = (float)damage / maxHP;

                 string desc = "";
                 if (ratio < 0.05f) desc = "かすり傷を負った。"; // < 5%
                 else if (ratio < 0.15f) desc = "軽傷を負った。"; // < 15%
                 else if (ratio < 0.30f) desc = "痛手を負った。"; // < 30% (Medium)
                 else if (ratio < 0.50f) desc = "重傷を負った。"; // < 50%
                 else desc = "致命的な一撃を受けた！"; // >= 50%

                 if (attackerName != null) return $"{targetName}は{attackerName}から{desc}";
                 return $"{targetName}は{desc}";
             }
        }

        private string FormatReductionLog(int original, int reduced)
        {
             // Packet 008.10R Check
             if (EffectiveObservationMode == ObservationMode.Numeric)
             {
                 return $"[Guard] Damage reduced: {original} -> {reduced}";
             }
             else
             {
                 // ObserveText
                 if (original <= 0) return ""; // Should not happen in guard context often, but safe fallback

                 float saved = original - reduced;
                 float reductionRate = (float)saved / original;

                 if (reductionRate >= 0.70f) return "完全に受け流した！";
                 if (reductionRate < 0.30f) return "わずかに受け流した。";
                 return "勢いを大きく殺した。";
             }
        }

        // --- Helpers ---
        private static object GetMemberValue(object obj, params string[] names) {
            if (obj == null) return null;
            var t = obj.GetType();
            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var n in names) { var p = t.GetProperty(n, BF); if (p != null) return p.GetValue(obj); var f = t.GetField(n, BF); if (f != null) return f.GetValue(obj); }
            return null;
        }
        private static string NormalizeName(string s) { if (string.IsNullOrEmpty(s)) return ""; return s.Trim().Replace("(Clone)", "").Trim(); }
        public static string GetActorKey(CharacterRuntime a) {
            if (a == null) return "";
            var v = GetMemberValue(a, "actorName", "ActorName", "_actorName");
            if (v is string s && !string.IsNullOrEmpty(s)) return NormalizeName(s);
            return NormalizeName(a.gameObject.name);
        }

        // Unit-R: Updated GetDisplayName
        public string GetDisplayName(CharacterRuntime a) {
            if (a == null) return "";
            if (_uniqueDisplayNames != null && _uniqueDisplayNames.TryGetValue(a, out string uniqueName)) return uniqueName;

            // Fallback (e.g. not initialized yet)
            return GetBaseDisplayName(a);
        }

        private string GetBaseDisplayName(CharacterRuntime a)
        {
            if (a == null) return "";
            var v = GetMemberValue(a, "displayName", "DisplayName", "_displayName");
            if (v is string s && !string.IsNullOrEmpty(s)) return NormalizeName(s);
            return GetActorKey(a);
        }

        private static CharacterStatus GetStatus(CharacterRuntime a) {
            if (a == null) return null;
            var comp = a.GetComponent<CharacterStatus>();
            if (comp != null) return comp;
            var v = GetMemberValue(a, "Status", "status", "_status", "characterStatus", "CharacterStatus");
            if (v is CharacterStatus cs1) return cs1;
            return null;
        }
        private CharacterRuntime ResolveActorRuntime(string actorName) {
            if (_playerActor != null && IsNameMatch(_playerActor, actorName)) return _playerActor;
            if (_enemyActor != null && IsNameMatch(_enemyActor, actorName)) return _enemyActor;
            foreach (var a in _actors) { if (a == null) continue; if (IsNameMatch(a, actorName)) return a; }
            return null;
        }
        private bool IsNameMatch(CharacterRuntime a, string name) {
            var key = GetActorKey(a);
            // Also match display name if possible
            string disp = GetDisplayName(a);
            if (disp.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;

            if (key.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            if (key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        private string BuildHpText(CharacterStatus s) {
            // Packet 008.10R: No-Numbers Gate
            if (IsNoNumbersDifficulty) return "";

            if (s == null) return "";
            object hpObj = GetMemberValue(s, "HP", "CurrentHP", "currentHP", "_hp", "_currentHP");
            object maxObj = GetMemberValue(s, "MaxHP", "maxHP", "_maxHP");
            if (hpObj is int hp && maxObj is int max) return $" (HP {hp}/{max})";
            return "";
        }

        // Packet 008.10R: Difficulty Logic
        private void ApplyDifficultySettings()
        {
            if (_useDifficultyLinkedObservationMode)
            {
                // Link Logic
                // Dev, Easy -> Usually Numeric (unless blocked by Gate, which isn't the case for Easy/Dev)
                // Normal+ -> Usually ObserveText
                
                if (IsNoNumbersDifficulty)
                {
                    // This sets the inspector value for clarity, but EffectiveObservationMode is what matters.
                    _observationMode = ObservationMode.ObserveText;
                }
                else
                {
                    _observationMode = ObservationMode.Numeric;
                }
                
                // Logging Effective Result
                EnqueueLine(0, $"[System] ObservationMode = {EffectiveObservationMode} (Source: Difficulty:{_difficulty})");
                EnqueueLine(0, $"[System] NoNumbers = {IsNoNumbersDifficulty} (Source: DifficultyGate)");
            }
            else
            {
                // Manual Override
                EnqueueLine(0, $"[System] ObservationMode = {EffectiveObservationMode} (Source: ManualOverride)");
                EnqueueLine(0, $"[System] NoNumbers = {IsNoNumbersDifficulty} (Source: DifficultyGate)");
            }
        }

        // Packet 008.11: Relief Logic
        private void RecordDeathEvent_GameTime()
        {
            // Calculate Current Game Time (Seconds)
            long currentUt = _persistentSessionUt + _battleGlobalTimeUt;
            double nowGameSec = (double)currentUt / UT_PER_TICK * _secondsPerTick;

            _deathHistoryGameTime.Add(nowGameSec);

            // Filter old entries
            double cutoff = nowGameSec - _reliefWindowGameSeconds;
            _deathHistoryGameTime.RemoveAll(t => t < cutoff);

            // Check Trigger
            if (_deathHistoryGameTime.Count >= _reliefTriggerCount && !_hasSuggestedRelief)
            {
                _hasSuggestedRelief = true;
                EnqueueLine(0, "[System] 難易度を下げると遊びやすくなります（設定から変更可能）");
            }
        }

        // P1.2 Helper Methods
        private int CalculateGridDistance(CharacterRuntime a, CharacterRuntime b)
        {
            if (a == null || b == null) return 0;
            var d = a.GridPos - b.GridPos;
            return Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z));
        }

        private string GetRangeCategory(int dist)
        {
            // P1.3: RangeBand Mapping A (Strict Grid Boundaries)
            // POINTBLANK (0-1): 0.0m - 0.5m
            if (dist <= 1) return "至近";
            // SHORT (2): 1.0m
            if (dist == 2) return "近い";
            // MID (3-4): 1.5m - 2.0m
            if (dist <= 4) return "中くらい";
            // LONG (5-12): 2.5m - 6.0m
            if (dist <= 12) return "離れている";
            // RANGED (13-60): 6.5m - 30.0m
            if (dist <= 60) return "遠い";
            // FAR (61-160): 30.5m - 80.0m
            if (dist <= 160) return "かなり遠い";
            // EXTREME (161+): 80.5m+
            return "極めて遠い";
        }

        private void MoveTowards(CharacterRuntime mover, CharacterRuntime target, int steps)
        {
            if (mover == null || target == null) return;
            // Simple 1D approach logic for MVP (Z-axis primary)
            
            var dir = target.GridPos - mover.GridPos;
            
            for(int i=0; i<steps; i++)
            {
                var d = target.GridPos - mover.GridPos;
                int dx = 0; int dz = 0;
                if (Mathf.Abs(d.z) > Mathf.Abs(d.x)) dz = (d.z > 0) ? 1 : -1;
                else if (Mathf.Abs(d.x) > 0) dx = (d.x > 0) ? 1 : -1;
                
                mover.GridPos += new Vector3Int(dx, 0, dz);
            }
        }

        public void EnqueueMove(string attackerName, string targetName)
        {
            var atk = ResolveActorRuntime(attackerName);
            var tgt = ResolveActorRuntime(targetName);
            if (atk == null || tgt == null) return;

            int startDist = CalculateGridDistance(atk, tgt);
            string startCat = GetRangeCategory(startDist);
            
            EnqueueLine(0, $"[Move] {GetDisplayName(atk)} advances (manual)");
            
            EnqueueLine(MOVE_TICKS_MANUAL, "...", () => {
                MoveTowards(atk, tgt, 1);
                int endDist = CalculateGridDistance(atk, tgt);
                string endCat = GetRangeCategory(endDist);
                if (startCat != endCat)
                {
                    EnqueueLine(0, $"[Range] {startCat} → {endCat}");
                }
            });
        }

        // P1.2.1: Contact State Hook (State-based "Melee")
        public void UpdateContactState(CharacterRuntime actor, CharacterRuntime.ContactState newState, string reason = "")
        {
            if (actor == null) return;
            if (actor.CurrentContactState == newState) return; // No Change

            var oldState = actor.CurrentContactState;
            actor.CurrentContactState = newState;

            string reasonStr = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";

            // Log Logic (Minimal / Spam Prevented)
            if (oldState == CharacterRuntime.ContactState.None && newState == CharacterRuntime.ContactState.CloseBody)
            {
                EnqueueLine(0, $"[Contact] 絡み合う距離だ{reasonStr}");
            }
            else if (oldState == CharacterRuntime.ContactState.CloseBody && newState == CharacterRuntime.ContactState.None)
            {
                EnqueueLine(0, $"[Contact] 距離が開いた{reasonStr}");
            }
            else if (newState == CharacterRuntime.ContactState.Grapple)
            {
                EnqueueLine(0, $"[Contact] 組み付いた！{reasonStr}");
            }
            else
            {
                // Generic Fallback
                EnqueueLine(0, $"[Contact] {oldState} → {newState}{reasonStr}");
            }
        }

        // P1.3: RangeBand Definition (Restored)
        private enum RangeBand { PointBlank, Short, Mid, Long, Ranged, Far, Extreme }

        // P1.3.2: StepBack Constants
        private const int STEPBACK_TICKS = 1;

        // P1.3.1: Weapon Range Profile Structures (MVP)
        private class WeaponRangeProfile
        {
            public System.Collections.Generic.HashSet<RangeBand> Optimal = new System.Collections.Generic.HashSet<RangeBand>();
            public System.Collections.Generic.HashSet<RangeBand> Unfavorable = new System.Collections.Generic.HashSet<RangeBand>();
            public System.Collections.Generic.HashSet<RangeBand> Impossible = new System.Collections.Generic.HashSet<RangeBand>();

            public bool IsImpossible(RangeBand b) => Impossible.Contains(b);
            public bool IsUnfavorable(RangeBand b) => Unfavorable.Contains(b);
        }

        private System.Collections.Generic.Dictionary<WeaponDef.WeaponCategory, WeaponRangeProfile> _weaponProfiles;

        private void InitializeWeaponProfiles()
        {
            _weaponProfiles = new System.Collections.Generic.Dictionary<WeaponDef.WeaponCategory, WeaponRangeProfile>();

            // 1. Knife: Strong at PointBlank/Short. Weak at Long+.
            var knife = new WeaponRangeProfile();
            knife.Optimal.Add(RangeBand.PointBlank); knife.Optimal.Add(RangeBand.Short);
            knife.Optimal.Add(RangeBand.Mid); // Allow Mid
            knife.Unfavorable.Add(RangeBand.Long);
            knife.Impossible.Add(RangeBand.Ranged); knife.Impossible.Add(RangeBand.Far); knife.Impossible.Add(RangeBand.Extreme);
            _weaponProfiles[WeaponDef.WeaponCategory.Knife] = knife;

            // 2. Sword: Standard. Weak at PointBlank.
            var sword = new WeaponRangeProfile();
            sword.Optimal.Add(RangeBand.Short); sword.Optimal.Add(RangeBand.Mid);
            sword.Unfavorable.Add(RangeBand.PointBlank); // Too close
            sword.Unfavorable.Add(RangeBand.Long);
            sword.Impossible.Add(RangeBand.Ranged); sword.Impossible.Add(RangeBand.Far);
            _weaponProfiles[WeaponDef.WeaponCategory.Sword] = sword;

            // 3. Greatsword: Mid is best. PointBlank is Impossible (MVP: Auto-stepback).
            var gs = new WeaponRangeProfile();
            gs.Optimal.Add(RangeBand.Short); gs.Optimal.Add(RangeBand.Mid);
            gs.Impossible.Add(RangeBand.PointBlank); // Must step back
            gs.Unfavorable.Add(RangeBand.Long);
            gs.Impossible.Add(RangeBand.Ranged);
            _weaponProfiles[WeaponDef.WeaponCategory.Greatsword] = gs;

            // 4. Spear: Long is best. PointBlank/Short weak.
            var spear = new WeaponRangeProfile();
            spear.Optimal.Add(RangeBand.Long); spear.Optimal.Add(RangeBand.Mid);
            spear.Unfavorable.Add(RangeBand.Short);
            spear.Unfavorable.Add(RangeBand.PointBlank); // Very hard
            spear.Impossible.Add(RangeBand.Ranged);
            _weaponProfiles[WeaponDef.WeaponCategory.Spear] = spear;

            // 5. Bow: Ranged. PointBlank Impossible.
            var bow = new WeaponRangeProfile();
            bow.Optimal.Add(RangeBand.Ranged); bow.Optimal.Add(RangeBand.Far);
            bow.Unfavorable.Add(RangeBand.Short); bow.Unfavorable.Add(RangeBand.Mid);
            bow.Impossible.Add(RangeBand.PointBlank); // Cannot shoot
            _weaponProfiles[WeaponDef.WeaponCategory.Bow] = bow;

            // 6. Gun: Ranged.
            var gun = new WeaponRangeProfile();
            gun.Optimal.Add(RangeBand.Ranged); gun.Optimal.Add(RangeBand.Far);
            gun.Unfavorable.Add(RangeBand.Short); gun.Unfavorable.Add(RangeBand.Mid);
            gun.Impossible.Add(RangeBand.PointBlank);
            _weaponProfiles[WeaponDef.WeaponCategory.Gun] = gun;
        }

        private WeaponDef.WeaponCategory GetWeaponCategory(CharacterRuntime c)
        {
            if (c == null || c.EquippedWeapon == null) return WeaponDef.WeaponCategory.Knife; // Default (Unarmed/Knife)
            
            // P1.3.2: Check Data Source First
            if (c.EquippedWeapon.category != WeaponDef.WeaponCategory.Unknown)
            {
                return c.EquippedWeapon.category;
            }

            // Name-based Mapping (Legacy/Fallback)
            string n = c.EquippedWeapon.name; // ScriptableObject name
            if (n.Contains("Knife") || n.Contains("Dagger")) return WeaponDef.WeaponCategory.Knife;
            if (n.Contains("Greatsword") || n.Contains("Claymore")) return WeaponDef.WeaponCategory.Greatsword;
            if (n.Contains("Sword") || n.Contains("Blade")) return WeaponDef.WeaponCategory.Sword;
            if (n.Contains("Spear") || n.Contains("Lance")) return WeaponDef.WeaponCategory.Spear;
            if (n.Contains("Bow") || n.Contains("Archer")) return WeaponDef.WeaponCategory.Bow;
            if (n.Contains("Gun") || n.Contains("Rifle")) return WeaponDef.WeaponCategory.Gun;
            
            return WeaponDef.WeaponCategory.Sword; // Fallback
        }


        private RangeBand GetRangeBandInternal(int dist)
        {
            if (dist <= 1) return RangeBand.PointBlank;
            if (dist == 2) return RangeBand.Short;
            if (dist <= 4) return RangeBand.Mid;
            if (dist <= 12) return RangeBand.Long;
            if (dist <= 60) return RangeBand.Ranged;
            if (dist <= 160) return RangeBand.Far;
            return RangeBand.Extreme;
        }

        // Packet_P0.7: Execute Logic
        private ActionResult ExecuteAttackLogic(CharacterRuntime attacker, CharacterRuntime target, ActionIntent intent)
        {
            if (intent == null) intent = new ActionIntent();
            if (intent.result == null) intent.result = new ActionResult();
            
            // Initialization (Purity Fix P0.7.1)
            var result = intent.result;
            result.executed = false; // Will be set to true by caller (EnqueueNormalAttack)
            result.logs.Clear();
            result.wasGuarded = false;
            result.mitigationRateUsed = 0f;
            result.finalDamage = 0;
            result.timeShiftDeltaTick = 0;

            var atkStatus = GetStatus(attacker);
            var tgtStatus = GetStatus(target);
            if (atkStatus == null || tgtStatus == null || atkStatus.IsDead || tgtStatus.IsDead) return result;

            var attackerDisp = GetDisplayName(attacker);
            var targetDisp = GetDisplayName(target);

            int baseDmg = Mathf.Max(1, atkStatus.ATK - tgtStatus.DEF);

            // Packet_007.1: Skill Effect Mapping Minimal (BasePower Override)
            if (ReEFeatureFlags.EnablePacket007_1_SkillEffectMappingMinimal && intent != null && !string.IsNullOrEmpty(intent.libraryId))
            {
                if (_skillDB == null)
                {
                    result.logs.Add("[P071 SkillDB Missing]");
                }
                else
                {
                    var skill = _skillDB.FindById(intent.libraryId);
                    if (skill == null)
                    {
                        result.logs.Add($"[P071 Skill Missing(Id={intent.libraryId})]");
                    }
                    else
                    {
                        int pre = baseDmg;
                        baseDmg = skill.basePower; // Override as Base Power
                        
                        string logTag = $"[P071 SkillMap(Id={skill.skillId},Base={skill.basePower})] [P071 SkillAmt(Pre={pre},Post={baseDmg})]";
                        result.logs.Add(logTag);
                    }
                }
            }

            // Packet_007.4: Reinforce Hook & Coating Log
            if (ReEFeatureFlags.EnablePacket007_4_BuffEffectMinimal)
            {
                 foreach (var eff in atkStatus.ActiveEffects)
                 {
                     if (eff.type == ReE.Combat.Effects.EffectType.Reinforce)
                     {
                         float mult = 1.0f + eff.magnitude;
                         baseDmg = Mathf.FloorToInt(baseDmg * mult);
                         result.logs.Add($"[P074 ReinforceMul(M={eff.magnitude})]");
                     }
                     if (eff.type == ReE.Combat.Effects.EffectType.Coating)
                     {
                          result.logs.Add($"[P074 CoatingTag({eff.tag})]");
                     }
                 }
            }

            int finalDmg = CalculateFinalDamage(attacker, target, baseDmg);

            // P0.6: Guard Mitigation
            if (tgtStatus.Runtime.IsGuarding)
            {
                result.wasGuarded = true;
                result.mitigationRateUsed = tgtStatus.Runtime.GuardMitigation; // 0.30f
                
                int pre = finalDmg;
                float reduction = result.mitigationRateUsed;
                finalDmg = Mathf.FloorToInt(finalDmg * (1.0f - reduction));
                
                // P0.8: Award TimeShift
                result.timeShiftDeltaTick = -GUARD_TIMESHIFT_TICK;
                
                result.logs.Add($"[Guard] target guarding: -{reduction*100:F0}% applied (before:{pre} after:{finalDmg})");
                result.logs.Add($"[TimeShift] Granting {targetDisp} {result.timeShiftDeltaTick} tick(s) (Guard)");
            }

            // Hotfix: Stat Reference Visualization
            result.logs.Add($"[Stats] {attackerDisp}(ATK:{atkStatus.ATK} [Base:{attacker.DefATK}+Wep:{attacker.WeaponATK}]) vs {targetDisp}(DEF:{tgtStatus.DEF} [Base:{target.DefDEF}+Arm:{target.ArmorDEF}]) BaseDmg:{baseDmg}");

            result.finalDamage = finalDmg;
            return result;
        }
    }
}
