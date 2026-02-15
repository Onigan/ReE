using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeInOnStart : MonoBehaviour
{
    public float duration = 0.5f;
    CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 1f;                 // 開始時は真っ黒
        cg.blocksRaycasts = true;      // フェード中は入力ブロック
        cg.interactable = false;
    }

    void Start() { StartCoroutine(FadeTo(0f)); } // 透明へ

    System.Collections.IEnumerator FadeTo(float target)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        cg.alpha = target;
        cg.blocksRaycasts = false;     // 透明になったら入力開放
    }
}
