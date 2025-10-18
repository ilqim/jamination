using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerDied;

    [Header("Ayarlar")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private string heartTag = "Heart";

    [Header("Durum")]
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private int currentHearts = 3;

    [Header("Ölüm Ekranı / Panel")]
    [SerializeField] private GameObject deathPanel;   // otomatik bulunacak
    private const string GameOverTag = "GameOver";

    public List<Image> heartImages = new List<Image>();
    private bool isDead = false;

    void Awake()
    {
        // Tekil kal
        var all = FindObjectsOfType<PlayerHealth>();
        if (all.Length > 1) { Destroy(gameObject); return; }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // İlk sahnede varsa yakala
        TryBindDeathPanel();
        if (deathPanel) deathPanel.SetActive(false);
    }

    void OnEnable()
    {
        PlayerController.OnTakeDamage += HandleDamage;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        PlayerController.OnTakeDamage -= HandleDamage;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        FindHeartsInScene();
        if (heartImages.Count > 0)
            maxHearts = currentHearts = heartImages.Count;
        RefreshHeartsUI();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahne yüklenir yüklenmez dene…
        TryBindDeathPanel();
        if (deathPanel) deathPanel.SetActive(false);

        // …ve bir frame SONRA tekrar dene (UI instantiate gecikmesi için)
        StartCoroutine(TryBindDeathPanelNextFrame());

        FindHeartsInScene();
        if (heartImages.Count > 0)
            maxHearts = Mathf.Max(maxHearts, heartImages.Count);
        RefreshHeartsUI();
    }

    System.Collections.IEnumerator TryBindDeathPanelNextFrame()
    {
        yield return null; // 1 frame bekle
        if (deathPanel == null)
        {
            TryBindDeathPanel();
            if (deathPanel) deathPanel.SetActive(false);
        }
    }

    void TryBindDeathPanel()
    {
        deathPanel = FindGameOverAnywhereInMemory();
    }

    // --- TÜM objeler arasından (inaktif dahil) bu sahneye ait GameOver tag'lisini bulur ---
    GameObject FindGameOverAnywhereInMemory()
    {
        var current = SceneManager.GetActiveScene();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        // Önce aktif sahnedekini ara
        foreach (var go in all)
        {
            if (!go) continue;
            if (!go.CompareTag(GameOverTag)) continue;
            if (!go.scene.IsValid()) continue;
            if (go.scene == current) return go;
        }
        // Bulunamadıysa DontDestroyOnLoad sahnesine de bak (opsiyonel)
        foreach (var go in all)
        {
            if (!go) continue;
            if (!go.CompareTag(GameOverTag)) continue;
            if (go.scene.IsValid() && go.scene.name == "DontDestroyOnLoad")
                return go;
        }
        return null;
    }

    void FindHeartsInScene()
    {
        heartImages.Clear();
        var hearts = GameObject.FindGameObjectsWithTag(heartTag);
        foreach (var h in hearts)
        {
            var img = h.GetComponent<Image>();
            if (img != null) heartImages.Add(img);
        }
        heartImages = heartImages.OrderBy(h => h.transform.GetSiblingIndex()).ToList();
    }

    void RefreshHeartsUI()
    {
        for (int i = 0; i < heartImages.Count; i++)
            heartImages[i].enabled = i < currentHearts;
    }

    void HandleDamage(int amount)
    {
        if (isDead) return;

        currentHearts = Mathf.Max(0, currentHearts - amount);

        int indexToHide = currentHearts;
        if (indexToHide >= 0 && indexToHide < heartImages.Count)
            heartImages[indexToHide].enabled = false;

        if (currentHearts <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log("Player öldü (can bitti).");

            if (!deathPanel)
                TryBindDeathPanel(); // son bir kez dene

            if (deathPanel)
                deathPanel.SetActive(true);

            OnPlayerDied?.Invoke();
        }
    }
}
