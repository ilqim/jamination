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
    [SerializeField] private GameObject deathPanel;
    private const string GameOverTag = "GameOver";

    public List<Image> heartImages = new List<Image>();
    private bool isDead = false;

    [SerializeField] private int checkpointHearts;
    private bool firstGameplayCheckpointSet = false;

    void Awake()
    {
        var all = FindObjectsOfType<PlayerHealth>();
        if (all.Length > 1) { Destroy(gameObject); return; }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        checkpointHearts = maxHearts;

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
        // === HER SAHNEDE DEATH PANELİ TEKRAR ARA ===
        StartCoroutine(RebindDeathPanelRoutine(scene));

        FindHeartsInScene();
        if (heartImages.Count > 0)
            maxHearts = Mathf.Max(maxHearts, heartImages.Count);
        RefreshHeartsUI();

        Debug.Log($"[PlayerHealth] SceneLoaded: '{scene.name}' | currentHearts={currentHearts}, checkpointHearts={checkpointHearts}");

        // === CHECKPOINT MANTIĞI ===
        if (isDead)
        {
            currentHearts = Mathf.Clamp(checkpointHearts, 0, maxHearts);
            isDead = false;
            RefreshHeartsUI();

            Debug.Log($"[PlayerHealth] SceneLoaded-AfterDeath: '{scene.name}' | currentHearts RESET to checkpoint={currentHearts}");
        }
        else
        {
            if (!firstGameplayCheckpointSet)
            {
                checkpointHearts = maxHearts;
                firstGameplayCheckpointSet = true;
                Debug.Log($"[PlayerHealth] First gameplay checkpoint set: {checkpointHearts}");
            }
            else
            {
                checkpointHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
                Debug.Log($"[PlayerHealth] Checkpoint updated on scene enter: '{scene.name}' | checkpointHearts={checkpointHearts}");
            }
        }
    }

    // 🔁 Paneli güvenli şekilde yeniden bulma (birkaç frame bekleyerek)
    private System.Collections.IEnumerator RebindDeathPanelRoutine(Scene scene)
    {
        for (int i = 0; i < 5; i++) // 5 frame boyunca tekrar dene
        {
            TryBindDeathPanel();
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
                Debug.Log($"[PlayerHealth] DeathPanel bound successfully on scene '{scene.name}' at frame {i}.");
                yield break;
            }
            yield return null; // 1 frame bekle
        }

        Debug.LogWarning($"[PlayerHealth] Could NOT find DeathPanel in scene '{scene.name}' after multiple tries!");
    }

    void TryBindDeathPanel()
    {
        deathPanel = FindGameOverAnywhereInMemory();
    }

    GameObject FindGameOverAnywhereInMemory()
    {
        var current = SceneManager.GetActiveScene();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();

        // Önce aktif sahnede ara
        foreach (var go in all)
        {
            if (!go) continue;
            if (!go.CompareTag(GameOverTag)) continue;
            if (!go.scene.IsValid()) continue;
            if (go.scene == current)
                return go;
        }

        // Eğer bulamazsa DontDestroyOnLoad sahnesine bak
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

        int prevHearts = currentHearts;
        currentHearts = Mathf.Max(0, currentHearts - amount);

        Debug.Log($"[PlayerHealth] Damage taken: {amount} | {prevHearts} -> {currentHearts} (checkpoint={checkpointHearts})");

        int indexToHide = currentHearts;
        if (indexToHide >= 0 && indexToHide < heartImages.Count)
            heartImages[indexToHide].enabled = false;

        if (currentHearts <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log("[PlayerHealth] Player died. Opening deathPanel.");

            TryBindDeathPanel();

            if (deathPanel)
                deathPanel.SetActive(true);
            else
                Debug.LogWarning("[PlayerHealth] DeathPanel not found!");

            OnPlayerDied?.Invoke();
        }
    }

    public void ResetToCheckpointHearts()
    {
        currentHearts = Mathf.Clamp(checkpointHearts, 0, maxHearts);
        RefreshHeartsUI();
    }

    public int GetCurrentHearts() => currentHearts;
    public int GetCheckpointHearts() => checkpointHearts;
}
