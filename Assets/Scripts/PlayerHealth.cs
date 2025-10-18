using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    // >>> Ölüm olayı
    public static event Action OnPlayerDied;

    [Header("Ayarlar")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private string heartTag = "Heart";  // Canvas içindeki Image'ların tag'i

    [Header("Durum")]
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private int currentHearts = 3;

    public List<Image> heartImages = new List<Image>();
    private bool isDead = false;

    private void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        PlayerController.OnTakeDamage += HandleDamage;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        PlayerController.OnTakeDamage -= HandleDamage;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindHeartsInScene();

        if (heartImages.Count > 0)
            maxHearts = currentHearts = heartImages.Count;

        RefreshHeartsUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindHeartsInScene();
        if (heartImages.Count > 0)
            maxHearts = Mathf.Max(maxHearts, heartImages.Count);
        RefreshHeartsUI();
    }

    private void FindHeartsInScene()
    {
        heartImages.Clear();

        var hearts = GameObject.FindGameObjectsWithTag(heartTag);
        foreach (var h in hearts)
        {
            var img = h.GetComponent<Image>();
            if (img != null) heartImages.Add(img);
        }

        heartImages = heartImages
            .OrderBy(h => h.transform.GetSiblingIndex())
            .ToList();
    }

    private void RefreshHeartsUI()
    {
        for (int i = 0; i < heartImages.Count; i++)
            heartImages[i].enabled = i < currentHearts;
    }

    private void HandleDamage(int amount)
    {
        if (isDead) return;

        currentHearts = Mathf.Max(0, currentHearts - amount);

        int indexToHide = currentHearts;
        if (indexToHide >= 0 && indexToHide < heartImages.Count)
            heartImages[indexToHide].enabled = false;

        if (currentHearts <= 0)
        {
            if (!isDead)
            {
                isDead = true;
                Debug.Log("Player öldü (can bitti).");
                OnPlayerDied?.Invoke();  // >>> Ölüm olayı
            }
        }
    }
}
