using ReE.Combat.TimeCore;
using ReE.Combat.Effects;
using ReE.CommonUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Unit-H: Fix for CharacterRuntime ambiguity
using CharacterRuntime = ReE.Stats.CharacterRuntime;

// Packet_007.0: Data Namespace
using ReE.Combat.Data;

namespace ReE.Combat.UI
{
    public class BattleUIAdapter_TimeCore : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameUIManager ui;
        [SerializeField] private BattleTimeManager battle;

        [Header("Retreat (Hold Esc)")]
        [SerializeField] private float escHoldSeconds = 0.7f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnBattleEndOk;

        // --- State Management ---
        private enum BattleState
        {
            Root,
            Attack,
            Defense,
            Special,
            Special_SituationAction,
            Special_Interpersonal,  // Unit-Q: New State
            Item,
            Ammunition,
            Situation,
            TargetSelect,
            ObserveConfirm, 
            Resolving,
            RetreatConfirm,
            BattleEnd
        }

        // Hotfix-P0.3: Stub for AdviceMode
        private enum AdviceMode { Off, EasyAuto, NormalAssist, Free }

        private BattleState _currentState = BattleState.Root;
        // Unit-E State Memory
        private BattleState _lastRootSubState = BattleState.Root; 
        private BattleState _returnStateFromTargetSelect = BattleState.Root;
        private BattleState _returnStateFromRetreatConfirm = BattleState.Root;

        // Unit-H: Target Tab
        private enum TargetTab { Enemy, Self, Ally }
        private TargetTab _currentTargetTab = TargetTab.Enemy;
        private int _targetSelectPageIndex = 0; // P-1: Target Select Paging

        private ActionIntent _currentIntent;

        // Auto-Advance & Failsafe (Unit-A)
        private Coroutine _autoAdvanceCo;
        private bool _isWaitingForAutoAdvance = false;
        private float _lastAdvanceTime = 0f;
        private const float FAILSAFE_TIMEOUT = 10f;

        // Hold Logic
        private Coroutine _escHoldCo;
        private float _escPressedTime = 0f;

        // Hotfix-E1
        private bool _endScreenShown = false;

        // Hotfix: RetreatFail Click-Skip Guard
        private float _blockResolveClickSkipUntil = 0f;
        [SerializeField] private float _resolveClickSkipGuardSeconds = 3.0f;
        
        // Spec v1.1.1: Suppress first Esc KeyUp after Hold
        private bool _suppressNextEscKeyUp = false;

        // Packet_007.0: Skill Data
        [Header("Data Source")]
        [SerializeField] private SkillDatabase _skillDB;

        // Hotfix-P0.3: Stub Field
        [SerializeField] private AdviceMode _adviceMode = AdviceMode.Off;

        private void Awake()
        {
            if (ui == null) ui = FindFirstObjectByType<GameUIManager>();
            if (battle == null) battle = FindFirstObjectByType<BattleTimeManager>();
            
            // Packet_008.5.1: Log Unification
            ReE.Stats.CharacterStatus.OnLog += HandleCharacterStatusLog;
        }

        private void OnDestroy()
        {
            ReE.Stats.CharacterStatus.OnLog -= HandleCharacterStatusLog;
        }

        private void HandleCharacterStatusLog(string msg)
        {
            if (ui != null) ui.AppendLog(msg);
        }

        private void Start()
        {
            // Packet 008.8: Wire up default restart loop (Hotfix-BattleEndOk: Strict)
            // Ensure our handler is always attached, even if Inspector events exist.
            OnBattleEndOk.RemoveListener(HandleBattleEndOk); // Prevent duplicates
            OnBattleEndOk.AddListener(HandleBattleEndOk);

            SwitchState(BattleState.Root);
        }

        private void Update()
        {
            // Unit-E & Hotfix-E1: Strict Gate (Progress Block)
            bool isProgressBlocked = ui.IsHistoryOpen || 
                                     _currentState == BattleState.RetreatConfirm ||
                                     _currentState == BattleState.ObserveConfirm || 
                                     _currentState == BattleState.BattleEnd; 

            if (_currentState == BattleState.Resolving && !isProgressBlocked)
            {
                // Failsafe Logic
                if (Time.time - _lastAdvanceTime > FAILSAFE_TIMEOUT)
                {
                    Debug.LogWarning("[UI] Failsafe Triggered: Resolving timeout.");
                    ForceRecoveryToRoot();
                    return; 
                }

                // Click Skip Trigger
                if (Input.GetMouseButtonDown(0) && _isWaitingForAutoAdvance)
                {
                    bool isPointerOverUI = false;
#if UNITY_EDITOR || UNITY_STANDALONE
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                        isPointerOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
#endif
                    if (!isPointerOverUI)
                    {
                        // Unit-L: 10.0s Click-Skip Guard
                        if (Time.time < _blockResolveClickSkipUntil) return;

                        Debug.Log("[UI] Click Skip Triggered");
                        _isWaitingForAutoAdvance = false; // Gate Open
                        if (_autoAdvanceCo != null) StopCoroutine(_autoAdvanceCo);
                        ResolveNextLine();
                    }
                }
            }

            HandleInput();
        }

        private void HandleInput()
        {
            // Hotfix-E1: Terminal State Block
            if (_currentState == BattleState.BattleEnd) return;

            // Unit-E: Strict Gate (Input Block)
            if (ui.IsHistoryOpen) return; 
            if (_currentState == BattleState.Resolving) return;

            // --- Input Spec v1.1 Implementation ---

            // 1. Retreat Hold Trigger (Root Only, Esc Only - B key EXCLUDED)
            if (_currentState == BattleState.Root)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _escPressedTime = Time.time;
                    if (_escHoldCo != null) StopCoroutine(_escHoldCo);
                    _escHoldCo = StartCoroutine(EscHoldRoutine());
                }
                // Stop hold if key released
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    if (_escHoldCo != null)
                    {
                        StopCoroutine(_escHoldCo);
                        _escHoldCo = null;
                    }
                }
            }

            // 2. Cancel / Back (Esc Short OR B key)
            bool isCancelInput = false;
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                // Guard: If suppression flag is set (by Hold completion), consume this input and do nothing
                if (_suppressNextEscKeyUp)
                {
                    _suppressNextEscKeyUp = false;
                    return;
                }
                isCancelInput = true; 
            }
            if (Input.GetKeyDown(KeyCode.B)) isCancelInput = true;    // Use KeyDown for B (Instant)

            if (isCancelInput)
            {
                PerformCancelLogic();
            }
        }

        private void PerformCancelLogic()
        {
            // (P0) TargetSelect: Tab Back (Ally->Self->Enemy)
            if (_currentState == BattleState.TargetSelect)
            {
                if (_currentTargetTab != TargetTab.Enemy)
                {
                    // P-2: Tab Back (Ally -> Self -> Enemy)
                    int prev = (int)_currentTargetTab - 1;
                    if (prev < 0) prev = 0; // Should not happen due to check above
                    _currentTargetTab = (TargetTab)prev;
                    _targetSelectPageIndex = 0; // Reset Page
                    ShowTargetSelect(false); // Refresh, preserve tab (which we just changed manually)
                    return;
                }
                // If Tab == Enemy(0), fall through to P1
            }

            // (P1) Return State (TargetSelect, ObserveConfirm, RetreatConfirm)
            if (_currentState == BattleState.TargetSelect)
            {
                SwitchState(_returnStateFromTargetSelect);
                return;
            }
            if (_currentState == BattleState.ObserveConfirm)
            {
                // Back to TargetSelect, Preserving Tab
                ShowTargetSelect(false); 
                return;
            }
            if (_currentState == BattleState.RetreatConfirm)
            {
                SwitchState(_returnStateFromRetreatConfirm);
                return;
            }

            // (P3) Submenu Back (Parent)
            switch (_currentState)
            {
                case BattleState.Attack:
                case BattleState.Defense:
                case BattleState.Special:
                case BattleState.Item:
                case BattleState.Situation:
                    SwitchState(BattleState.Root);
                    return;
                
                case BattleState.Special_SituationAction:
                    SwitchState(BattleState.Special);
                    return;
                case BattleState.Special_Interpersonal: // Unit-Q
                    SwitchState(BattleState.Special_SituationAction);
                    return;
                case BattleState.Ammunition:
                    SwitchState(BattleState.Item);
                    return;
            }

            // (P4) Root Memory Back
            /* 
            if (_currentState == BattleState.Root)
            {
                if (_lastRootSubState != BattleState.Root)
                {
                    SwitchState(_lastRootSubState);
                }
            }
            */
        }

        private IEnumerator EscHoldRoutine()
        {
            float t = 0f;
            while (t < escHoldSeconds)
            {
                // Spec 4-3: Confirm state ignores Hold
                if (_currentState != BattleState.Root) yield break; // Abort if state changed

                t += Time.deltaTime;
                yield return null;
            }
            
            // Validate Logic (Strict)
            if (_currentState == BattleState.Root && Input.GetKey(KeyCode.Escape))
            {
                 _escHoldCo = null;
                 _suppressNextEscKeyUp = true; // Guard: suppress the release event
                 ShowRetreatConfirm();
            }
            else
            {
                _escHoldCo = null;
            }
        }
        
        // --- Helper ---
        private void Stub(string label)
        {
             ui.AppendLog($"[System] 未実装: {label}");
             SwitchState(BattleState.Root);
        }

        private UIOption Empty() => new UIOption(" ", null, false);

        // --- State Switching ---

        private void SwitchState(BattleState newState)
        {
            // Unit-E: Update Memory when entering Subs from Root
            if (_currentState == BattleState.Root)
            {
                if (newState == BattleState.Attack || newState == BattleState.Defense || 
                    newState == BattleState.Special || newState == BattleState.Item || 
                    newState == BattleState.Situation)
                {
                    _lastRootSubState = newState;
                }
            }

            Debug.Log($"[Transition] {_currentState} -> {newState}");
            _currentState = newState;
            
            ui.SetBackButton(() => { PerformCancelLogic(); }, "", false); // Bind Back Button to unified logic

            switch (_currentState)
            {
                case BattleState.Root: ShowRoot(); break;
                case BattleState.Attack: ShowAttack(); break;
                case BattleState.Defense: ShowDefense(); break;
                case BattleState.Special: ShowSpecial(); break;
                case BattleState.Special_SituationAction: ShowSpecial_SituationAction(); break;
                case BattleState.Special_Interpersonal: ShowSpecial_Interpersonal(); break; // Unit-Q
                case BattleState.Item: ShowItem(); break;
                case BattleState.Ammunition: ShowAmmunition(); break;
                case BattleState.Situation: ShowSituation(); break;
                case BattleState.TargetSelect: ShowTargetSelect(true); break; // Default Reset
                case BattleState.ObserveConfirm: ShowObserveConfirm(); break;
                case BattleState.RetreatConfirm: ShowRetreatConfirm_Internal(); break;
                case BattleState.Resolving:
                    _lastAdvanceTime = Time.time;
                    ShowResolving();
                    break;
                case BattleState.BattleEnd: // Hotfix-E1
                    ShowBattleEnd();
                    break;
            }
        }

        // --- Menus ---

        private void ShowRoot()
        {
            // P0.6: Guard Exit on Turn Start (Input Ready)
            if (battle != null && battle.PlayerActor != null && battle.PlayerActor.IsGuarding)
            {
                battle.PlayerActor.ExitGuard("TurnStart");
                ui.AppendLog($"[Guard] {battle.PlayerActor.DisplayName} exits guard (reason=TurnStart)");
            }

            _currentIntent = null;
            _isWaitingForAutoAdvance = false;
            if (_autoAdvanceCo != null) StopCoroutine(_autoAdvanceCo);

            // Hotfix-E1: Strict Safety to BattleEnd
            if (battle != null && battle.BattleEnded)
            {
                SwitchState(BattleState.BattleEnd);
                return;
            }

            ui.SetMenuButtons(new List<UIOption>
            {
                new UIOption("攻撃", () => SwitchState(BattleState.Attack), true),
                new UIOption("防御", () => SwitchState(BattleState.Defense), true),
                new UIOption("特殊", () => SwitchState(BattleState.Special), true),
                new UIOption("アイテム", () => SwitchState(BattleState.Item), true),
                new UIOption("戦況", () => SwitchState(BattleState.Situation), true),
            });
        }

        private void ShowAttack()
        {
            ui.SetMenuButtons(new List<UIOption>
            {
                new UIOption("通常攻撃", () => { 
                    _currentIntent = ActionIntent.CreateAttack();
                    // Packet_008.2: Explicit Type
                    _currentIntent.type = IntentType.NormalAttack;
                    
                    // Packet_001: Add MVP Effect
                    _currentIntent.effects.Add(new Effect { kind = EffectKind.Damage, basePower = 0f, attributeTag = "physical" });
                    _returnStateFromTargetSelect = BattleState.Attack; 
                    SwitchState(BattleState.TargetSelect); 
                }, true),
                new UIOption("攻撃スキル", () => ShowSkyUI(SkillUISubCategory.Skill, SkillUIMainCategory.Attack), true),
                new UIOption("攻撃魔法", () => ShowSkyUI(SkillUISubCategory.Magic, SkillUIMainCategory.Attack), true),
                Empty(),
                Empty(),
            });
            ui.SetBackButton(() => SwitchState(BattleState.Root), "戻る", true);
        }

        private void ShowDefense()
        {
            ui.SetMenuButtons(new List<UIOption>
            {
                new UIOption("ガード", () => { 
                    _currentIntent = ActionIntent.CreateGuard(); 
                    // Packet_008.2
                    _currentIntent.type = IntentType.Defend;
                    OnCommit(); 
                }, true),
                new UIOption("防御スキル", () => ShowSkyUI(SkillUISubCategory.Skill, SkillUIMainCategory.Defense), true),
                new UIOption("防御魔法", () => ShowSkyUI(SkillUISubCategory.Magic, SkillUIMainCategory.Defense), true),
                new UIOption("回復支援スキル", () => ShowSkyUI(SkillUISubCategory.Skill, SkillUIMainCategory.Heal, SkillUIMainCategory.Support), true),
                new UIOption("回復支援魔法", () => ShowSkyUI(SkillUISubCategory.Magic, SkillUIMainCategory.Heal, SkillUIMainCategory.Support), true),
            });
            ui.SetBackButton(() => SwitchState(BattleState.Root), "戻る", true);
        }

        private void ShowSpecial()
        {
            ui.SetMenuButtons(new List<UIOption>
            {
                new UIOption("魔法一覧", () => ShowSkyUI(SkillUISubCategory.Magic), true),
                new UIOption("スキル一覧", () => ShowSkyUI(SkillUISubCategory.Skill), true),
                new UIOption("状況行動", () => SwitchState(BattleState.Special_SituationAction), true),
                new UIOption("環境利用", () => Stub("特殊/環境利用"), true),
                new UIOption("撤退", () => ShowRetreatConfirm(), true), 
            });
            ui.SetBackButton(() => SwitchState(BattleState.Root), "戻る", true);
        }

        private void ShowSituation()
        {
            // Packet_008.1: AI Proposal Only
            // Packet_008.8 Refinement: Updated Labels
            ui.SetMenuButtons(new List<UIOption>
            {
                new UIOption("攻め", () => Stub("AI/Aggro"), true),
                new UIOption("安定", () => Stub("AI/Stable"), true),
                new UIOption("補助/防衛", () => Stub("AI/Support"), true),
                new UIOption("最善", () => Stub("AI/Best"), true),
                new UIOption("交ぜ書き", () => Stub("AI/Shuffle"), true),
            });
            ui.SetBackButton(() => SwitchState(BattleState.Root), "戻る", true);
        }

        // Packet_008.3: FreeText Input
        private string _freeTextInput = "";
        private bool _focusFreeText = false;

#if false // Legacy OnGUI FreeText Disabled (Superseded by TMP FreeInput)
        private void OnGUI()
        {
            // Simple MVP Text Field at bottom of screen
            // Only show if not waiting / busy? Or always?
            // "FreeText入力確定（Enter）で..."
            
            float width = 300;
            float height = 24;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - 40;

            GUI.SetNextControlName("FreeTextField");
            _freeTextInput = GUI.TextField(new Rect(x, y, width, height), _freeTextInput);

            // Handle Enter
            Event e = Event.current;
            if (e.isKey && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "FreeTextField")
            {
                if (!string.IsNullOrEmpty(_freeTextInput))
                {
                    HandleFreeText(_freeTextInput);
                    _freeTextInput = ""; // Clear after submit
                    GUI.FocusControl(""); // Unfocus
                }
            }
        }
#endif

        private void HandleFreeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            
            // 1) Input Normalization
            input = input.Trim().Replace("　", " "); // Full-width space -> half-width
            while (input.Contains("  ")) input = input.Replace("  ", " "); // Compress spaces
            
            // Packet_008.7: Advice Command
            if (input.StartsWith("相談:") || input.StartsWith("相談：") || input.StartsWith("/advice"))
            {
                // Extract query (after command)
                string query = "";
                if (input.StartsWith("/advice")) query = input.Substring("/advice".Length).Trim();
                else query = input.Substring("相談:".Length).Trim(); // Works for both ":" and "：" if lengths match (they do in C# length logic? No, check char)
                
                // Robust extraction
                int sepIndex = input.IndexOfAny(new char[] { ':', '：', ' ' });
                if (sepIndex >= 0) query = input.Substring(sepIndex + 1).Trim();
                
                string advice = GetAdviceTextStub(AdviceMode.Free, query);
                ui.AppendLog($"[AI] {advice}");
                return;
            }

            // A) Command Search (/magic, /skill)
            if (input.StartsWith("/"))
            {
                var parts = input.Split(new char[]{' '}, System.StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToLower();
                string query = (parts.Length > 1) ? string.Join(" ", parts.Skip(1)) : "";

                if (cmd == "/magic")
                {
                    PerformSearch(SkillUISubCategory.Magic, query, cmd);
                    return;
                }
                else if (cmd == "/skill")
                {
                    PerformSearch(SkillUISubCategory.Skill, query, cmd);
                    return;
                }
            }

            // B) Smart Match (No slash) based on Name/Alias/Incantation
            if (_skillDB != null)
            {
                var matches = _skillDB.skills.Where(s => IsSmartMatch(s, input)).ToList();

                if (matches.Count == 1)
                {
                    // Single Match -> Instant Confirm
                    var s = matches[0];
                    CreateIntentFromSkill(s);
                    ui.AppendLog($"[FreeText] matched skillId={s.skillId}");
                    return;
                }
                else if (matches.Count > 1)
                {
                    // Multiple -> List
                    ShowSkyUI_Render(matches, true);
                    ui.AppendLog($"[FreeText] SmartMatch hits={matches.Count}");
                    return;
                }
            }

            // C) Fallback Logic
            // C-1) Normal Attack Heuristic
            if (input.Contains("通常攻撃") || input.Contains("攻撃") || input.Contains("殴る"))
            {
                _currentIntent = ActionIntent.CreateAttack();
                _currentIntent.type = IntentType.NormalAttack;
                _currentIntent.freeTextMeta = input;
                // P001 MVP
                _currentIntent.effects.Add(new Effect { kind = EffectKind.Damage, basePower = 0f, attributeTag = "physical" });
                
                ui.AppendLog($"[FreeText] fallback=NormalAttack meta='{input}'");
                _returnStateFromTargetSelect = BattleState.Root;
                SwitchState(BattleState.TargetSelect);
                return;
            }

            // C-2) Fallback to Magic List
            ui.AppendLog($"[FreeText] fallback=MagicList meta='{input}'");
            ShowSkyUI(SkillUISubCategory.Magic);
        }

        private bool IsSmartMatch(SkillData s, string query)
        {
            if (s == null) return false;
            // Case-insensitive check
            if (ContainsIgnoreCase(s.displayName, query)) return true;
            if (ContainsIgnoreCase(s.skillId, query)) return true;
            if (ContainsIgnoreCase(s.alias, query)) return true;
            if (ContainsIgnoreCase(s.incantation, query)) return true;
            return false;
        }
        
        private bool ContainsIgnoreCase(string source, string check)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return source.IndexOf(check, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void PerformSearch(SkillUISubCategory sub, string query, string modeLabel)
        {
             if (_skillDB == null) return;
             
             var candidates = _skillDB.skills.Where(s => {
                if (s == null) return false;
                if (s.uiSub != sub) return false;
                if (string.IsNullOrEmpty(query)) return true; // Show all if query is empty
                
                return IsSmartMatch(s, query);
             }).ToList();
             
             ShowSkyUI_Render(candidates, true);
             ui.AppendLog($"[FreeText] mode={modeLabel} query='{query}' hits={candidates.Count}");
        }

        // Deprecated: merged into PerformSearch
        private void ShowSkyUI_Search(SkillUISubCategory sub, string query)
        {
             PerformSearch(sub, query, "search");
        }

        // Hotfix-P0.3: Stub Method
        private string GetAdviceTextStub(AdviceMode mode, string query)
        {
             return "Advice Feature Not Implemented";
        }

        private void CreateIntentFromSkill(ReE.Combat.Data.SkillData s)
        {
            _currentIntent = ActionIntent.CreateAttack(); 
            _currentIntent.libraryId = s.skillId;
            
            if (s.uiSub == SkillUISubCategory.Magic) _currentIntent.type = IntentType.Magic;
            else if (s.uiSub == SkillUISubCategory.Skill) _currentIntent.type = IntentType.Skill;
            else _currentIntent.type = IntentType.NormalAttack;
            
            _currentIntent.targetSide = s.defaultTargetSide;
            
            _returnStateFromTargetSelect = _currentState; 
            SwitchState(BattleState.TargetSelect);
        }

        // --- SkyUI Implementation (Packet_008.1 / 008.2) ---
        private void ShowSkyUI(SkillUISubCategory sub, params SkillUIMainCategory[] mains)
        {
            ShowSkyUI_Internal(sub, mains, true);
        }

        private void ShowSkyUI_Internal(SkillUISubCategory sub, IList<SkillUIMainCategory> mains, bool resetPage)
        {
            if (_skillDB == null)
            {
                ui.AppendLog("[System] SkillDB Missing");
                return;
            }

            var candidates = _skillDB.skills.Where(s => {
                if (s == null) return false;
                if (s.uiSub != sub) return false;
                
                if (mains != null && mains.Count > 0)
                {
                    bool match = false;
                    foreach(var m in mains)
                    {
                        if (s.uiMain == m) { match = true; break; }
                    }
                    if (!match) return false;
                }
                
                return true;
            }).ToList();

            ShowSkyUI_Render(candidates, resetPage);
            
            string catLabel = (mains == null || mains.Count == 0) ? "All" : string.Join("|", mains);
            ui.AppendLog($"[{sub}/{catLabel}] 選択中...");
        }

        private void ShowSkyUI_Render(List<ReE.Combat.Data.SkillData> candidates, bool resetPage)
        {
            if (resetPage) _targetSelectPageIndex = 0;

            int capacity = (candidates.Count <= 5) ? 5 : 4;
            int totalPages = Mathf.CeilToInt((float)candidates.Count / capacity);
            if (totalPages < 1) totalPages = 1;
            if (_targetSelectPageIndex >= totalPages) _targetSelectPageIndex = 0;

            var pageItems = candidates.Skip(_targetSelectPageIndex * capacity).Take(capacity).ToList();
            var opts = new List<UIOption>();

            foreach(var s in pageItems)
            {
                string label = string.IsNullOrEmpty(s.displayName) ? s.skillId : s.displayName;
                opts.Add(new UIOption(label, () => {
                    CreateIntentFromSkill(s);
                }, true));
            }

            while(opts.Count < capacity) opts.Add(Empty());
            
            if (candidates.Count > 5)
            {
                opts.Add(new UIOption("次へ", () => {
                   _targetSelectPageIndex++;
                   if (_targetSelectPageIndex >= totalPages) _targetSelectPageIndex = 0;
                   ShowSkyUI_Render(candidates, false);
                }, true));
            }
            
            ui.SetMenuButtons(opts);
            ui.SetBackButton(() => SwitchState(BattleState.Root), "戻る", true);
        }

        private void ShowTargetSelect(bool resetTab = true)
        {
            if (_currentIntent == null) { SwitchState(BattleState.Root); return; }

            // Spec 4-1: Default Tab Reset
            if (resetTab) 
            {
                // Packet 008.1: Smart Default based on Intent Side
                if (_currentIntent.targetSide == TargetSide.Ally) _currentTargetTab = TargetTab.Self;
                else _currentTargetTab = TargetTab.Enemy;
                
                _targetSelectPageIndex = 0; 
            }

            var opts = new List<UIOption>();
            // ... (Rest of logic is same, just need to preserve it)
            // Need to copy-paste the rest of the function or rely on "Replace" carefully.
            // Since "ReplacementContent" must be exact, I will reproduce the rest of ShowTargetSelect logic here.
            
            // Slot 1: Tab Toggle
            string tabName = _currentTargetTab switch
            {
                TargetTab.Enemy => "敵",
                TargetTab.Self => "自分",
                TargetTab.Ally => "味方",
                _ => _currentTargetTab.ToString()
            };
            string startLabel = $"対象：{tabName}（切替）";
            
            opts.Add(new UIOption(startLabel, () => {
                // Unit-H v1.1.1: Enemy(0) -> Self(1) -> Ally(2) -> Enemy(0)
                int next = (int)_currentTargetTab + 1;
                if (next > 2) next = 0;
                _currentTargetTab = (TargetTab)next;
                _targetSelectPageIndex = 0; // Reset Page on Tab Switch
                ShowTargetSelect(false);
            }, true));

            // Candidate Collection
            var candidates = new List<CharacterRuntime>();
            if (battle != null)
            {
                foreach(var actor in battle.ActiveActors) {
                    if (actor.name == "Player") {
                        if (_currentTargetTab == TargetTab.Self) candidates.Add(actor);
                    } else {
                        if (_currentTargetTab == TargetTab.Enemy) candidates.Add(actor);
                        else if (_currentTargetTab == TargetTab.Ally) { /* No allies yet */ }
                    }
                }
            }

            // P-1: Paging Logic
            int capacity = (candidates.Count <= 4) ? 4 : 3;
            int totalPages = Mathf.CeilToInt((float)candidates.Count / capacity);
            if (totalPages < 1) totalPages = 1;
            if (_targetSelectPageIndex >= totalPages) _targetSelectPageIndex = 0;

            var pageItems = candidates.Skip(_targetSelectPageIndex * capacity).Take(capacity).ToList();

            foreach(var c in pageItems)
            {
                string k = BattleTimeManager.GetActorKey(c);
                string d = battle.GetDisplayName(c);
                
                opts.Add(new UIOption(d, () => {
                    Debug.Log($"[UI] Clicked Target: {d}");
                    _currentIntent.targetIds.Clear();
                    _currentIntent.targetIds.Add(d);
                    
                    if (_currentIntent.intentType == "Observe") {
                            SwitchState(BattleState.ObserveConfirm);
                    } else {
                            OnCommit();
                    }
                }, true));
            }

            // Pad
            while(opts.Count < (capacity + 1)) opts.Add(Empty()); // +1 for Slot 1

            // Navigation Button (Slot 5) if needed
            if (candidates.Count > 4)
            {
                opts.Add(new UIOption("次へ", () => {
                    _targetSelectPageIndex++;
                    if (_targetSelectPageIndex >= totalPages) _targetSelectPageIndex = 0;
                    ShowTargetSelect(false);
                }, true));
            }
            else
            {
                while(opts.Count < 5) opts.Add(Empty());
            }

            // Safety Clip
            if (opts.Count > 5) opts = opts.GetRange(0, 5);

            ui.SetMenuButtons(opts);
            ui.SetBackButton(() => SwitchState(_returnStateFromTargetSelect), "戻る", true);

            if (resetTab)
            {
                    if (_currentIntent.intentType == "Observe") ui.AppendLog("観察／記録：対象を選択");
                    else ui.AppendLog("対象を選択");
            }
        }

        private void ShowObserveConfirm()
        {
            ui.AppendLog("行動確認：対象情報の記録（時間消費）");
            ui.SetMenuButtons(new List<UIOption> {
                new UIOption("実行", () => OnCommit(), true),
                new UIOption("やめる", () => {
                     // Back to TargetSelect, keeping current Tab
                     ShowTargetSelect(false);
                }, true),
                Empty(), Empty(), Empty()
            });
            ui.SetBackButton(() => ShowTargetSelect(false), "戻る", true); 
        }

        private void ShowRetreatConfirm()
        {
            _returnStateFromRetreatConfirm = _currentState;
            SwitchState(BattleState.RetreatConfirm);
        }

        private void ShowRetreatConfirm_Internal()
        {
            ui.AppendLog("撤退しますか？");
            var opts = new List<UIOption>
            {
                new UIOption("撤退する", () => {
                    if (battle != null)
                    {
                        if (battle.TryRequestRetreat())
                        {
                            SwitchState(BattleState.BattleEnd);
                        }
                        else
                        {
                            _blockResolveClickSkipUntil = Time.time + _resolveClickSkipGuardSeconds; 
                            SwitchState(BattleState.Resolving);
                        }
                    }
                    else SwitchState(BattleState.Root); 
                }, true),
                new UIOption("やめる", () => {
                    SwitchState(_returnStateFromRetreatConfirm);
                }, true),
                Empty(), Empty(), Empty()
            };
            ui.SetMenuButtons(opts);
            ui.SetBackButton(() => SwitchState(_returnStateFromRetreatConfirm), "戻る", true);
        }

        // Hotfix: Packet_008.3.1 - Missing Stub Methods
        private void ShowSpecial_SituationAction()
        {
             Stub("戦況判断");
        }

        private void ShowSpecial_Interpersonal()
        {
             Stub("対話/問合い");
        }

        private void ShowItem()
        {
             Stub("アイテム");
        }

        private void ShowAmmunition()
        {
             Stub("弾薬");
        }

        private void OnCommit()
        {
            Debug.Log("[UI] Commit Action");
            if (_currentIntent != null && battle != null)
            {
                battle.EnqueueIntent(_currentIntent);
            }
            SwitchState(BattleState.Resolving);
        }

        private void ShowResolving()
        {
            var opts = new List<UIOption>();
            for(int i=0; i<5; i++) opts.Add(new UIOption("", null, false));
            ui.SetMenuButtons(opts);
            ui.SetBackButton(null, "", false);
            ResolveNextLine();
        }

        private void ResolveNextLine()
        {
            _lastAdvanceTime = Time.time;
            if (battle == null) return;
            if (!battle.TryDequeue(out float delay, out string text))
            {
                if (battle.BattleEnded)
                {
                    SwitchState(BattleState.BattleEnd);
                    return;
                }
                SwitchState(BattleState.Root);
                return;
            }

            ui.AppendLog(text);
            if (_autoAdvanceCo != null) StopCoroutine(_autoAdvanceCo);
            _autoAdvanceCo = StartCoroutine(AutoAdvanceRoutine(delay));
        }

        private IEnumerator AutoAdvanceRoutine(float duration)
        {
            _isWaitingForAutoAdvance = true;
            if (duration <= 0f)
            {
                while (ui.IsHistoryOpen) yield return null;
                yield return null; 
                while (ui.IsHistoryOpen) yield return null;
            }
            else
            {
                float t = 0f;
                while (t < duration)
                {
                    if (!ui.IsHistoryOpen) t += Time.deltaTime;
                    yield return null;
                }
            }
            
            if (_isWaitingForAutoAdvance)
            {
                _isWaitingForAutoAdvance = false;
                ResolveNextLine();
            }
        }

        private void ForceRecoveryToRoot()
        {
            _isWaitingForAutoAdvance = false;
            if (_autoAdvanceCo != null) StopCoroutine(_autoAdvanceCo);
            ui.AppendLog("[System] 応答なしのため復帰します。");
            SwitchState(BattleState.Root);
        }

        private void ShowBattleEnd()
        {
            _currentState = BattleState.BattleEnd;

            if (!_endScreenShown)
            {
                _endScreenShown = true;
                string msg = "";
                if (battle != null)
                {
                    if (battle.Outcome == BattleTimeManager.BattleOutcome.Retreat) msg = "撤退完了";
                    else if (battle.Outcome == BattleTimeManager.BattleOutcome.Victory) msg = "勝利！";
                    else if (battle.Outcome == BattleTimeManager.BattleOutcome.Death) msg = "死亡";
                    else if (battle.Outcome == BattleTimeManager.BattleOutcome.Down) msg = "戦闘不能"; 
                    else msg = "敗北..."; 
                }
                ui.AppendLog(msg);
            }

            // Unit-S: UnityEvent Hook
            ui.SetMenuButtons(new List<UIOption>
            {
                new UIOption("確認", () => {
                    // Disable multiple clicks
                    ui.SetMenuButtons(new List<UIOption>()); 
                    
                    if (OnBattleEndOk != null) OnBattleEndOk.Invoke();
                    
                    if (OnBattleEndOk == null || OnBattleEndOk.GetPersistentEventCount() == 0)
                    {
                         ui.AppendLog("[System] 終了ハンドラ未設定");
                         // Re-enable button if no handler? Or just leave stuck?
                         // User said "log handler missing". Stuck is fine as it's a dev error state.
                         // But for safety, maybe let them click again?
                         // "まず OKボタンを無効化" implies commit.
                    }
                }, true),
                Empty(), Empty(), Empty(), Empty()
            });
            ui.SetBackButton(null, "", false);
        }
        public void HandleBattleEndOk()
        {
            ui.AppendLog("[BattleEndOk] begin");
            
            if (battle != null)
            {
                battle.DebugResetBattle();
                ui.ClearLog(); // Hotfix: Clear logs to prevent Raycast blocks
                ui.AppendLog("[BattleEndOk] resetting...");
            }
            
            SwitchState(BattleState.Root);
            ui.AppendLog("[BattleEndOk] state=Root");
            ui.AppendLog("[BattleEndOk] done");
        }
    }
}
