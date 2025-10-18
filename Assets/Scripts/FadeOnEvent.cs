using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeOnEvent : MonoBehaviour
{
    [Header("Fade Ayarları")]
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private bool blockInputDuringFade = true;
    [SerializeField] private Color fadeColor = Color.black; // <-- EKLENDİ

    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();

        // Başlangıç: şeffaf siyah
        Color c = fadeColor; 
        c.a = 0f;
        img.color = c;
        img.raycastTarget = false;

        // (İsteğe bağlı) Tam ekran olduğundan emin ol
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnEnable()
    {
        PlayerController.OnRequestFadeOut += StartFade;
    }

    private void OnDisable()
    {
        PlayerController.OnRequestFadeOut -= StartFade;
    }

    private void StartFade()
    {
        // Debug.Log("FadeOnEvent: StartFade çağrıldı"); // İstersen izle
        // Debug.Log("Kararmali2");
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (blockInputDuringFade) img.raycastTarget = true;

        float t = 0f;
        Color start = img.color;               // şeffaf siyah
        Color target = fadeColor; target.a = 1f; // opak siyah

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            img.color = Color.Lerp(start, target, k);
            yield return null;
        }

        img.color = target;
    }
}
