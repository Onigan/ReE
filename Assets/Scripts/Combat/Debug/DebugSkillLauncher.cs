using UnityEngine;
using UnityEngine.UI;
using ReE.Combat.TimeCore;
using ReE.Combat.Data;
using System.Collections.Generic;

namespace ReE.Combat.DebugTools
{
    /// <summary>
    /// Packet_008: Debug Skill Launcher (Integration Test Harness)
    /// Provides a minimal UI to fire skills directly into BattleTimeManager,
    /// bypassing the currently unimplemented main battle UI.
    /// </summary>
    public class DebugSkillLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleTimeManager _battleManager;
        [SerializeField] private ReE.Stats.CharacterRuntime _player;
        [SerializeField] private ReE.Stats.CharacterRuntime _enemy;
        [SerializeField] private SkillDatabase _skillDB;
        [SerializeField] private Canvas _targetCanvas;

        [Header("Config")]
        [SerializeField] private Vector2 _startPos = new Vector2(50, -50);
        [SerializeField] private float _ySpacing = 40f;

        private void Start()
        {
            if (_targetCanvas == null)
            {
                _targetCanvas = FindObjectOfType<Canvas>();
                if (_targetCanvas == null)
                {
                    Debug.LogError("[DebugSkillLauncher] No Canvas found!");
                    return;
                }
            }

            // Packet_008.4: Dump DB
            if (_skillDB != null)
            {
                var sb = new System.Text.StringBuilder($"[DebugSkillLauncher] DB Loaded ({_skillDB.skills.Count} items):\n");
                foreach (var s in _skillDB.skills)
                {
                    if (s == null)
                    {
                        sb.AppendLine("- [NULL]");
                        continue;
                    }
                    string eff = !string.IsNullOrEmpty(s.skillId) ? s.skillId : s.name;
                    sb.AppendLine($"- ID: {eff} | Name: {s.displayName} | Kind: {s.kind}");
                }
                Debug.Log(sb.ToString());
            }

            CreateButtons();
        }

        private void CreateButtons()
        {
            // Create a panel or just direct buttons for simplicity
            GameObject panel = new GameObject("DebugSkillPanel");
            panel.transform.SetParent(_targetCanvas.transform, false);
            
            // Align Top-Left
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = _startPos;
            
            int btnIndex = 0;

            // Helper to safe create
            void TryAdd(string label, string skillId, TargetSide side)
            {
                if (_skillDB != null)
                {
                    var validation = _skillDB.FindById(skillId);
                    if (validation == null)
                    {
                        Debug.LogWarning($"[DebugSkillLauncher] Skip Button '{label}': SkillID '{skillId}' not found in DB.");
                        return;
                    }
                }
                CreateButton(panel, label, btnIndex, () => ExecuteSkill(skillId, side));
                btnIndex++;
            }

            // 1. DAMAGE (Enemy)
            TryAdd("[TEST] DMG (Enemy)", "SK_DMG_01", TargetSide.Enemy);

            // 2. REINFORCE (Self)
            TryAdd("[TEST] REINFORCE (Self)", "SK_REINFORCE", TargetSide.Ally);

            // 3. HEAL (Self)
            TryAdd("[TEST] HEAL (Self)", "SK_HEAL_01", TargetSide.Ally);

            // 4. NULLIFY (Self)
            TryAdd("[TEST] NULLIFY (Self)", "SK_NULLIFY", TargetSide.Ally);

            // 5. INVERT (Self) - Optional but useful
            TryAdd("[TEST] INVERT (Self)", "SK_INVERT", TargetSide.Ally);
                
            // 6. COATING (Self) - 007.4 Check
            TryAdd("[TEST] COATING (Self)", "SK_COATING_FIRE", TargetSide.Ally);
        }

        private void CreateButton(GameObject parent, string label, int index, UnityEngine.Events.UnityAction action)
        {
            // Create Button GO
            GameObject result = new GameObject($"Btn_{label}");
            result.transform.SetParent(parent.transform, false);

            // Image
            Image img = result.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Button
            Button btn = result.AddComponent<Button>();
            btn.onClick.AddListener(action);
            btn.onClick.AddListener(() => Debug.Log($"[DebugSkillLauncher] Clicked: {label}"));

            // Rect
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 35); // Hotfix: Wider
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, -index * _ySpacing);

            // Text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(result.transform, false);
            Text txt = textGO.AddComponent<Text>();
            txt.text = label;
            
            // Hotfix-P0: Unity 6 Font
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
            {
                Debug.LogError($"[DebugSkillLauncher] Failed to load 'LegacyRuntime.ttf' for {label}. Trying Arial fallback.");
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void ExecuteSkill(string skillId, TargetSide side)
        {
            if (_battleManager == null)
            {
                Debug.LogError("[DebugSkillLauncher] BattleManager is null!");
                return;
            }

            // Packet_008: Verify Skill Existence
            if (_skillDB != null)
            {
                var skill = _skillDB.FindById(skillId);
                if (skill == null)
                {
                    Debug.LogWarning($"[DebugSkillLauncher] Skill '{skillId}' not found in DB. Enqueueing anyway to test error handling.");
                }
            }

            Debug.Log($"[DebugSkillLauncher] Enqueueing: {skillId} -> Side: {side}");

            // Create Intent manually (Logic Scaffolding)
            var intent = new ActionIntent();
            intent.intentType = "Attack"; // Using 'Attack' as generic carrier for now, 007.2 checks libraryId
            intent.actorId = "Player"; // Force Player
            intent.libraryId = skillId;
            intent.targetSide = side; 
            
            // Resolve Target ID for consistency (though TargetSide logic in BTM handles fallback)
            // Even if we leave targetIds empty, 007.3 ResolveTargetStatus will use TargetSide+Fallback logic.
            // But let's try to be helpful if possible.
            intent.targetIds = new List<string>();
            // Don't populate targetIds explicitly here to TEST the Fallback/TargetSide logic in BTM!
            // Wait, if it's TargetSide.Enemy, BTM 007.3 logic might default to Enemy?
            // "EnqueueIntent" -> "string t = (intent.targetIds.Count > 0) ? intent.targetIds[0] : "Enemy";"
            // This happens at the bottom of EnqueueIntent in the Fallthrough block.
            // But Packet 007.2 intercepts BEFORE that if Heal/Buff.
            // So for Heal/Buff, targetIds might be empty.
            // Packet 007.3 ResolveTargetStatus handles empty targets for Ally (Self fallback).
            // For Enemy?
            // "if (intent.targetSide == Enemy)... string t = (intent.targetIds... "
            // We should arguably set targetIds for Enemy to ensure robustness, or rely on BTM.
            // Let's set it if Enemy for safety, or leave empty to test robustness?
            // User requested "Mimic actual pipeline".
            // Actual UI sets targetIds.
            
            if (side == TargetSide.Enemy && _enemy != null)
            {
                // We don't have Enemy ID easily available unless we check Runtime.
                // Assuming "Enemy" string literals are used in current hacks.
                intent.targetIds.Add("Enemy"); 
            }
            else if (side == TargetSide.Ally && _player != null)
            {
                // intent.targetIds.Add("Player"); 
                // Let's explicitly LEAVE EMPTY for Ally to verify 007.3 Self-Fallback logic!
            }

            _battleManager.EnqueueIntent(intent);

            // Packet_008.3: Auto-Resolve for Debug
            _battleManager.DebugProcessUntilIdle();
        }
    }
}
