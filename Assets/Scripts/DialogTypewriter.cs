using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class DialogTypewriter : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI textUI;
    [SerializeField] private TextMeshProUGUI continueIcon;

    [Header("İçerik")]
    [TextArea(2, 6)]
    [SerializeField] private string[] lines;

    [Header("Ayarlar")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private float linePause = 0.0f;
    [SerializeField] private bool closePanelWhenFinished = true;

    [Header("Diyalog Bittiğinde Kapıyı Aç (Opsiyonel)")]
    [SerializeField] private string doorToUnlock = "";

    // Başlatma olayı (zaten vardı)
    public static event Action OnDialogStartRequested;
    public static void RequestStart() => OnDialogStartRequested?.Invoke();

    // ✅ Yeni: Diyalog Bitti olayı
    public static event Action OnDialogFinished;

    private int index = 0;
    private Coroutine typingRoutine;
    private bool isTyping = false;
    private bool isActive = false;

    private void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
        if (continueIcon) continueIcon.gameObject.SetActive(false);
        if (textUI) textUI.text = string.Empty;
    }

    private void OnEnable()
    {
        OnDialogStartRequested += HandleStartRequested;
    }

    private void OnDisable()
    {
        OnDialogStartRequested -= HandleStartRequested;
    }

    private void HandleStartRequested()
    {
        if (isActive) return;
        if (lines == null || lines.Length == 0 || textUI == null || panelRoot == null)
        {
            Debug.LogWarning("[DialogTypewriter] Eksik referans ya da boş içerik.");
            return;
        }

        index = 0;
        isActive = true;
        if (panelRoot) panelRoot.SetActive(true);
        if (continueIcon) continueIcon.gameObject.SetActive(false);
        textUI.text = string.Empty;

        StartTypingCurrent();
    }

    private void StartTypingCurrent()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeRoutine(lines[index]));
    }

    private IEnumerator TypeRoutine(string content)
    {
        isTyping = true;
        textUI.text = string.Empty;

        foreach (char ch in content)
        {
            textUI.text += ch;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingRoutine = null;

        if (continueIcon) continueIcon.gameObject.SetActive(true);

        if (linePause > 0f)
            yield return new WaitForSeconds(linePause);
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping) CompleteCurrentLineInstant();
            else GoNextOrFinish();
        }
    }

    private void CompleteCurrentLineInstant()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
        textUI.text = lines[index];
        isTyping = false;
        if (continueIcon) continueIcon.gameObject.SetActive(true);
    }

    private void GoNextOrFinish()
    {
        if (continueIcon) continueIcon.gameObject.SetActive(false);

        if (index < lines.Length - 1)
        {
            index++;
            StartTypingCurrent();
        }
        else
        {
            // Diyalog bitti
            isActive = false;
            if (closePanelWhenFinished && panelRoot) panelRoot.SetActive(false);

            if (!string.IsNullOrEmpty(doorToUnlock))
            {
                Debug.Log($"[DialogTypewriter] Diyalog bitti, kapı açılıyor: {doorToUnlock}");
                Door.UnlockDoor(doorToUnlock);
            }

            // ✅ Bitti event'ini yayınla
            OnDialogFinished?.Invoke();
        }
    }
}
