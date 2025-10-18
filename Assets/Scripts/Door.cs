using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

[DisallowMultipleComponent]
public class Door : MonoBehaviour
{
    [Header("Hedef Sahne Adı (Build Settings'e ekli olmalı)")]
    [SerializeField] private string sceneName;

    [Header("Geçiş Ayarları")]
    [SerializeField] private float doorOpenDelay = 1f;
    [SerializeField] private float fadeDelayAfterOpen = 0.5f;
    [SerializeField] private float sceneLoadDelayAfterFade = 1.5f;

    [Header("Kapı Görseli (opsiyonel)")]
    [SerializeField] private Animator doorAnimator;

    private bool isTransitioning = false;
    private bool isUnlocked = false; // 🔒 açılabilirlik durumu (başta kilitli)

    public string SceneName => sceneName;

    // 📢 Bu event başka bir yerden kapının açılabilirliğini değiştirmek için kullanılacak
    public static event Action<string> OnUnlockDoor; // parametre: sceneName ya da kapı adı

    private void OnEnable()
    {
        OnUnlockDoor += HandleUnlockDoor;
    }

    private void OnDisable()
    {
        OnUnlockDoor -= HandleUnlockDoor;
    }

    private void HandleUnlockDoor(string doorName)
    {
        // Eğer bu kapının ismi (veya sahnesi) eşleşiyorsa açılabilirliği aktif et
        if (doorName == this.name || doorName == sceneName)
        {
            isUnlocked = true;
            Debug.Log($"[Door] {name} artık açılabilir durumda!");
        }
    }

    /// <summary>
    /// Oyuncu tarafından çağrılır — E'ye basıldığında tetiklenir.
    /// </summary>
    public void TryOpen()
    {
        if (!isUnlocked)
        {
            Debug.Log($"[Door] {name} şu anda kilitli.");
            return;
        }

        if (isTransitioning) return;
        isTransitioning = true;

        StartCoroutine(ChangeSceneRoutine(sceneName));
    }

    private IEnumerator ChangeSceneRoutine(string target)
    {
        // 1) Kapı açılma efekti
        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");

        // 2) Kapı açılma süresi
        if (doorOpenDelay > 0f)
            yield return new WaitForSeconds(doorOpenDelay);

        // 3) Fade başlat
        PlayerController.RequestFadeOut();

        // 4) Fade’in başlamasını bekle
        if (fadeDelayAfterOpen > 0f)
            yield return new WaitForSeconds(fadeDelayAfterOpen);

        // 5) Fade bitene kadar bekle
        yield return new WaitForSeconds(sceneLoadDelayAfterFade);

        // 6) Sahneyi yükle
        SceneManager.LoadScene(target, LoadSceneMode.Single);
    }

    // 🎯 Bu fonksiyon başka bir script'ten kapıyı açılabilir yapmak için çağrılabilir:
    public static void UnlockDoor(string doorName)
    {
        OnUnlockDoor?.Invoke(doorName);
    }
}
