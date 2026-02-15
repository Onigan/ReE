using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;   // Fade の CanvasGroup
    [SerializeField] float duration = 0.5f;     // フェード時間
    [SerializeField] bool fadeInOnStart = false;// 新シーンで自動フェードイン

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (fadeInOnStart) canvasGroup.alpha = 1f; // フェードイン開始用（真っ黒で開始）
    }

    void Start()
    {
        if (fadeInOnStart) StartCoroutine(Fade(0f)); // フェードイン（1→0）
    }

    public void FadeToVillage() => StartCoroutine(FadeAndLoad("Village"));
    public void FadeToForest() => StartCoroutine(FadeAndLoad("Forest"));
    public void FadeToScene(string sceneName) => StartCoroutine(FadeAndLoad(sceneName));

    System.Collections.IEnumerator FadeAndLoad(string sceneName)
    {
        canvasGroup.blocksRaycasts = true;     // 入力をブロック
        yield return Fade(1f);                 // 0→1 フェードアウト
        SceneManager.LoadScene(sceneName);     // シーン切替
        // 切替後は新シーンの Fade オブジェクトが Start でフェードイン
    }

    System.Collections.IEnumerator Fade(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        canvasGroup.alpha = target;
        canvasGroup.blocksRaycasts = target > 0.99f; // 真っ黒の時だけブロック
    }
}
