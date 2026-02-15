using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace ReE.CommonUI
{
    [Serializable]
    public struct UIOption
    {
        public string label;
        public Action onClick;
        public bool interactable;

        public UIOption(string label, Action onClick, bool interactable = true)
        {
            this.label = label;
            this.onClick = onClick;
            this.interactable = interactable;
        }
    }

    public class GameUIManager : MonoBehaviour
    {
        [Header("Log")]
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private TMP_Text logText;

        [Header("Buttons")]
        [SerializeField] private List<Button> optionButtons = new List<Button>();
        [SerializeField] private Button backButton;

        [Header("Free Input")]
        [SerializeField] private TMP_InputField freeInput;

        public bool IsFreeInputFocused => freeInput != null && freeInput.isFocused;

        // Unit-A: Overlay State (Placeholder)
        public bool IsHistoryOpen { get; set; } = false;

        public void FocusFreeInput()
        {
            if (freeInput == null) return;
            freeInput.ActivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(freeInput.gameObject);
        }

        public string ReadInputText() => freeInput != null ? freeInput.text : "";
        public void ClearInput() { if (freeInput != null) freeInput.text = ""; }

        public void AppendLog(string line)
        {
            if (logText == null) return;

            logText.text += line + "\n";

            // 下端スクロール（崩れにくい最小セット）
            Canvas.ForceUpdateCanvases();
            if (logScrollRect != null)
                logScrollRect.verticalNormalizedPosition = 0f;
        }

        public void ClearLog()
        {
            if (logText != null) logText.text = "";
        }

        public void SetMenuButtons(IReadOnlyList<UIOption> options)
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                var btn = optionButtons[i];
                if (btn == null) continue;

                if (options != null && i < options.Count && !string.IsNullOrEmpty(options[i].label))
                {
                    var opt = options[i];

                    btn.gameObject.SetActive(true);
                    btn.interactable = opt.interactable;

                    var t = btn.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = opt.label;

                    btn.onClick.RemoveAllListeners();
                    if (opt.onClick != null)
                        btn.onClick.AddListener(() => opt.onClick.Invoke());
                }
                else
                {
                    btn.gameObject.SetActive(true);
                    btn.interactable = false;

                    var t = btn.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = "";

                    btn.onClick.RemoveAllListeners();
                }
            }
        }

        // ★ここが重要：第三引数は「bool」で受ける。名前付き引数は使わなくても動く。
        public void SetBackButton(Action onClick, string label = "戻る", bool show = true)
        {
            if (backButton == null) return;

            backButton.gameObject.SetActive(show);

            var t = backButton.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = label;

            backButton.onClick.RemoveAllListeners();
            if (onClick != null)
                backButton.onClick.AddListener(() => onClick.Invoke());
        }
    }
}
