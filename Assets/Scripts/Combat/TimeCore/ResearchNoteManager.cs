using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ReE.Combat.TimeCore
{
    [Serializable]
    public class ObservationToken
    {
        public string TargetKey;          // Unique Species ID (e.g. "Goblin")
        public string TargetDisplayName;  // Display text (e.g. "Goblin A")
        public long ObservedAt;
    }

    public class ResearchNoteManager : MonoBehaviour
    {
        private static ResearchNoteManager _instance;
        public static ResearchNoteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResearchNoteManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ResearchNoteManager");
                        _instance = go.AddComponent<ResearchNoteManager>();
                        // DontDestroyOnLoad is handled in Awake, but good to ensure here too if created runtime
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private List<ObservationToken> _storedTokens = new List<ObservationToken>();
        private string SavePath => Path.Combine(Application.persistentDataPath, "research_notes.json");

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
            Debug.Log($"[ResearchNote] SavePath = {SavePath}");
        }

        public void AppendTokens(List<ObservationToken> newTokens)
        {
            if (newTokens == null || newTokens.Count == 0) return;

            bool changed = false;
            foreach (var t in newTokens)
            {
                // Hotfix-O1: Deduplicate by TargetKey (Species ID)
                if (!_storedTokens.Any(x => x.TargetKey == t.TargetKey))
                {
                    _storedTokens.Add(t);
                    changed = true;
                    Debug.Log($"[ResearchNote] New Token Acquired: {t.TargetDisplayName} (Key: {t.TargetKey})");
                }
            }

            if (changed)
            {
                Save();
            }
        }

        public bool HasToken(string targetKey)
        {
            return _storedTokens.Any(x => x.TargetKey == targetKey);
        }

        // Packet_003.2: Read-Only Observation Level Inference
        public bool TryGetObservationLevel(string targetKey, out int level)
        {
            level = 0;
            if (string.IsNullOrEmpty(targetKey)) return false;
            
            // Logic: Token Exists = Lv1 (Basic), No Token = Lv0 (Unobserved)
            if (HasToken(targetKey))
            {
                level = 1;
            }
            return true;
        }

        public void ConsumeTokens()
        {
            // Placeholder for Town/Exploration usage
            _storedTokens.Clear();
            Save();
            Debug.Log("[ResearchNote] Logic: Tokens consumed");
        }

        private void Save()
        {
            try
            {
                Wrapper wrapper = new Wrapper { Tokens = _storedTokens };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[ResearchNote] Saved {_storedTokens.Count} tokens to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResearchNote] Save Failed: {e.Message}");
            }
        }

        private void Load()
        {
            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
                if (wrapper != null && wrapper.Tokens != null)
                {
                    _storedTokens = wrapper.Tokens;
                }
                Debug.Log($"[ResearchNote] Loaded {_storedTokens.Count} tokens");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResearchNote] Load Failed: {e.Message}");
            }
        }

        [Serializable]
        private class Wrapper
        {
            public List<ObservationToken> Tokens;
        }
    }
}
