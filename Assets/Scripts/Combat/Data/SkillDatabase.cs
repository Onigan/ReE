using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ReE.Combat.Data
{
    [CreateAssetMenu(fileName = "NewSkillDB", menuName = "ReE/Combat/SkillDatabase")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillData> skills = new List<SkillData>();

        public SkillData FindById(string id)
        {
            if (skills == null || string.IsNullOrEmpty(id)) return null;

            // 1. Exact Match
            var exact = skills.FirstOrDefault(s => s != null && s.skillId == id);
            if (exact != null) return exact;

            // 2. Fallback Match (Name)
            var fallback = skills.FirstOrDefault(s => s != null && s.name == id);
            if (fallback != null) return fallback;

            // 3. Not Found -> Warning with List
            Debug.LogWarning($"[SkillDatabase] Skill Missing: '{id}' requested but not found.");
            LogAvailableSkills();
            
            return null;
        }

        private void LogAvailableSkills()
        {
            if (skills == null) return;
            var list = new System.Text.StringBuilder("[SkillDatabase] Available IDs:\n");
            foreach (var s in skills)
            {
                if (s == null) continue;
                string eff = !string.IsNullOrEmpty(s.skillId) ? s.skillId : s.name;
                list.AppendLine($"- {eff} (Kind={s.kind})");
            }
            Debug.LogWarning(list.ToString());
        }
    }
}
