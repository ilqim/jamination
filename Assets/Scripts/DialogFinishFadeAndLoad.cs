using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogFinishFadeAndLoad : MonoBehaviour
{
    [Header("Sahne Değişimi")]
    [SerializeField] private string sceneToLoad = "NextScene";

    [Header("Zamanlama")]
    [Tooltip("Diyalog biter bitmez, fade başlamadan önce beklenecek süre (opsiyonel).")]
    [SerializeField] private float delayBeforeFade = 0f;

    [Tooltip("FadeOnEvent içindeki fadeDuration ile aynı olmalı!")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("Zamanı timeScale'den bağımsız say (pause vs. için).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("DontDestroyOnLoad Temizliği (Opsiyonel)")]
    [SerializeField] private bool clearDontDestroyOnLoad = false;
    [Tooltip("Bu isimdeki DDOL objelerine dokunma (tam ad eşleşir).")]
    [SerializeField] private string[] ddolWhitelistNames;

    private void OnEnable()
    {
        DialogTypewriter.OnDialogFinished += HandleDialogFinished;
    }

    private void OnDisable()
    {
        DialogTypewriter.OnDialogFinished -= HandleDialogFinished;
    }

    private void HandleDialogFinished()
    {
        StartCoroutine(FadeAndLoadRoutine());
    }

    private IEnumerator FadeAndLoadRoutine()
    {
        // 1) İsteğe bağlı ön bekleme
        if (delayBeforeFade > 0f)
            yield return Wait(delayBeforeFade);

        // 2) Fade’i tetikle
        PlayerController.RequestFadeOut();

        // 3) Fade bitene kadar bekle
        yield return Wait(fadeDuration);

        // 4) İsteniyorsa DDOL temizle
        if (clearDontDestroyOnLoad)
            ClearDontDestroyOnLoadScene(ddolWhitelistNames);

        // 5) Sahneyi yükle
        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
        else
            Debug.LogWarning("[DialogFinishFadeAndLoad] 'sceneToLoad' boş. Lütfen Inspector’dan sahne adını verin.");
    }

    private IEnumerator Wait(float seconds)
    {
        if (!useUnscaledTime)
        {
            yield return new WaitForSeconds(seconds);
        }
        else
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    // --- DDOL Temizleyici ---
    private static void ClearDontDestroyOnLoadScene(string[] whitelistNames = null)
    {
        // DDOL sahnesine erişmek için geçici bir obje numarası:
        var temp = new GameObject("__DDOL_Finder");
        DontDestroyOnLoad(temp);

        var ddolScene = temp.scene; // DontDestroyOnLoad sahnesi
        var roots = ddolScene.GetRootGameObjects();

        foreach (var go in roots)
        {
            if (go == temp) continue; // kendimizi atla

            // Beyaz liste kontrolü (isim bazlı)
            if (IsWhitelisted(go, whitelistNames))
                continue;

            // Güvenlik: yok et
#if UNITY_EDITOR
            // Editörde anında silmek için:
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }

        // Finder'ı da kaldır
#if UNITY_EDITOR
        Object.DestroyImmediate(temp);
#else
        Object.Destroy(temp);
#endif
    }

    private static bool IsWhitelisted(GameObject go, string[] whitelistNames)
    {
        if (whitelistNames == null || whitelistNames.Length == 0) return false;
        foreach (var n in whitelistNames)
        {
            if (!string.IsNullOrEmpty(n) && go.name == n) return true;
        }
        return false;
    }
}
