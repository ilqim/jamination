using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeOnEvent : MonoBehaviour
{
    [Header("Fade Ayarları")]
    [SerializeField] private float fadeDuration = 1.5f; // saniye
    [SerializeField] private bool blockInputDuringFade = true; // Raycast Target

    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
        // İlk başta görünmez (alpha 0)
        var c = img.color;
        c.a = 0f;
        img.color = c;
        img.raycastTarget = false;
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
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (blockInputDuringFade) img.raycastTarget = true;

        float t = 0f;
        Color start = img.color;
        Color target = start; target.a = 1f; // full kararma

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // oyun dursa bile fade aksın
            float k = Mathf.Clamp01(t / fadeDuration);
            img.color = Color.Lerp(start, target, k);
            yield return null;
        }

        img.color = target;
    }
}
