// LogTest.cs (Unity 6.2 / TMP / ScrollRect 安定版)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 新Input System併用
#endif

[DisallowMultipleComponent]
public class LogTest : MonoBehaviour
{
    [Header("参照 (ドラッグ推奨)")]
    [SerializeField] private TMP_Text logText;          // BattleLog/Viewport/Content/LogText
    [SerializeField] private ScrollRect scrollRect;     // BattleLog(ScrollRect)
    [SerializeField] private TMP_InputField freeInput;  // FreeInput(TMP_InputField)

    [Header("起動時にダミー行を積む数 (任意)")]
    [SerializeField] private int seedLines = 0;

    [Header("見つからない場合に自動で探す (任意)")]
    [SerializeField] private bool autoWireIfNull = true;

    private int counter = 0;

    void Awake()
    {
        // 参照が未設定なら名前で自動探索（任意）
        if (autoWireIfNull)
        {
            if (!logText)
            {
                var t = GameObject.Find("LogText");
                if (t) logText = t.GetComponent<TMP_Text>();
            }
            if (!scrollRect)
            {
                var s = GameObject.Find("BattleLog");
                if (s) scrollRect = s.GetComponent<ScrollRect>();
            }
            if (!freeInput)
            {
                var f = GameObject.Find("FreeInput");
                if (f) freeInput = f.GetComponent<TMP_InputField>();
            }
        }
    }

    void Start()
    {
        // ScrollRect を“硬め”に（後で好みで変更OK）
        if (scrollRect)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
        }

        // 入力欄：Enter送信を確実化
        if (freeInput)
        {
            freeInput.lineType = TMP_InputField.LineType.SingleLine; // UI側でもSingle Lineに
            freeInput.onEndEdit.RemoveAllListeners();
            freeInput.onSubmit?.RemoveAllListeners();                // 環境によりnull
            freeInput.onEndEdit.AddListener(OnFreeInputEndEdit);
            freeInput.onSubmit?.AddListener(OnFreeInputEndEdit);
        }

        // ダミー行（任意）
        for (int i = 0; i < seedLines; i++)
            Append($"初期ログ {i + 1}");
    }

    void Update()
    {
        // 入力欄フォーカス中はスペース追記を抑止
        if (freeInput && freeInput.isFocused) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Append($"追記テスト {++counter}");
#else
        if (Input.GetKeyDown(KeyCode.Space))
            Append($"追記テスト {++counter}");
#endif
    }

    // ====== 外部/イベントから呼ぶAPI ======

    public void OnFreeInputEndEdit(string text)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 0) return;

        Append($"自由入力: {t}");

        if (freeInput)
        {
            freeInput.text = "";
            freeInput.ActivateInputField(); // 連続入力しやすく
            freeInput.caretPosition = 0;
            freeInput.selectionStringAnchorPosition = 0;
            freeInput.selectionStringFocusPosition = 0;
        }
    }

    public void Append(string msg)
    {
        if (!logText) return;

        if (string.IsNullOrEmpty(logText.text)) logText.text = msg;
        else logText.text += "\n" + msg;

        StopAllCoroutines();
        StartCoroutine(ScrollBottomReliable());
    }

    public void ClearLog()
    {
        if (!logText) return;
        logText.text = "";
        StopAllCoroutines();
        StartCoroutine(ScrollBottomReliable());
    }

    // ====== 末尾を“確実”に見せるスクロール ======

    IEnumerator ScrollBottomReliable()
    {
        if (!logText || !scrollRect) yield break;

        // ① TMPメッシュ&レイアウト即時更新
        logText.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(logText.rectTransform);
        if (scrollRect.content)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        // ② 最終反映を1フレーム待つ（必要ならもう1行追加）
        yield return null;
        // yield return null; // それでも遅れる場合

        Canvas.ForceUpdateCanvases();

        // ③ 慣性の影響を避けて瞬時に最下部へ
        bool prev = scrollRect.inertia;
        scrollRect.inertia = false;
        scrollRect.verticalNormalizedPosition = 0f; // 0=最下
        Canvas.ForceUpdateCanvases();
        scrollRect.inertia = prev;
    }
}
