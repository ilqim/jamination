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

    [Header("İlk Satır Gecikmesi")]
    [SerializeField] private float firstLineDelay = 1.0f; // panel açıldıktan sonra ilk satıra başlamadan önceki bekleme

    [Header("Diyalog Bittiğinde Kapıyı Aç (Opsiyonel)")]
    [SerializeField] private string doorToUnlock = "";

    [Header("Typing Sesi")]
    [Tooltip("Typing sesi çalacak AudioSource (UI Canvas altında olabilir).")]
    [SerializeField] private AudioSource typingAudioSource;
    [Tooltip("Loop edilecek typing AudioClip'i.")]
    [SerializeField] private AudioClip typingClip;
    [Tooltip("Satır yazılırken sesi loop et.")]
    [SerializeField] private bool loopTypingAudio = true;
    [Range(0f, 1f)]
    [SerializeField] private float typingVolume = 0.6f;

    // Başlatma olayı
    public static event Action OnDialogStartRequested;
    public static void RequestStart() => OnDialogStartRequested?.Invoke();

    // Bitti olayı
    public static event Action OnDialogFinished;

    private int index = 0;
    private Coroutine typingRoutine;
    private Coroutine firstDelayRoutine;
    private bool isTyping = false;
    private bool isActive = false;
    private bool waitingFirstDelay = false;

    private void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
        if (continueIcon) continueIcon.gameObject.SetActive(false);
        if (textUI) textUI.text = string.Empty;

        // AudioSource varsa temel ayarları güvene al
        if (typingAudioSource != null)
        {
            typingAudioSource.loop = loopTypingAudio;
            typingAudioSource.playOnAwake = false;
            typingAudioSource.volume = typingVolume;
            if (typingClip != null) typingAudioSource.clip = typingClip;
        }
    }

    private void OnEnable()
    {
        OnDialogStartRequested += HandleStartRequested;
    }

    private void OnDisable()
    {
        OnDialogStartRequested -= HandleStartRequested;
        StopTypingSound();
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
        isTyping = false;
        StopTypingSound(); // güvenlik

        if (panelRoot) panelRoot.SetActive(true);
        if (continueIcon) continueIcon.gameObject.SetActive(false);
        textUI.text = string.Empty;

        // İlk satır gecikmesi
        if (firstDelayRoutine != null) StopCoroutine(firstDelayRoutine);
        waitingFirstDelay = true;
        firstDelayRoutine = StartCoroutine(FirstLineDelayRoutine());
    }

    private IEnumerator FirstLineDelayRoutine()
    {
        yield return new WaitForSeconds(firstLineDelay);
        waitingFirstDelay = false;
        StartTypingCurrent();
    }

    private void StartTypingCurrent()
    {
        if (waitingFirstDelay) return; // güvenlik
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        // satır yazımı başlarken typing sesini başlat
        StartTypingSound();

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

        // satır bitti → typing sesini durdur
        StopTypingSound();

        if (continueIcon) continueIcon.gameObject.SetActive(true);

        if (linePause > 0f)
            yield return new WaitForSeconds(linePause);
    }

    private void Update()
    {
        if (!isActive) return;

        // İlk gecikme sürerken hiçbir şey yapma (istersen buraya E ile skip koyabilirsin)
        if (waitingFirstDelay) return;

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

        // satır anında tamamlandı → typing sesini kes
        StopTypingSound();

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
            StartTypingCurrent(); // yeni satır → typing sesi tekrar başlar
        }
        else
        {
            // Diyalog bitti
            isActive = false;

            StopTypingSound(); // güvenlik

            if (closePanelWhenFinished && panelRoot) panelRoot.SetActive(false);

            if (!string.IsNullOrEmpty(doorToUnlock))
            {
                Debug.Log($"[DialogTypewriter] Diyalog bitti, kapı açılıyor: {doorToUnlock}");
                Door.UnlockDoor(doorToUnlock);
            }

            OnDialogFinished?.Invoke();
        }
    }

    // --- Typing ses kontrolü ---
    private void StartTypingSound()
    {
        if (typingAudioSource == null) return;

        typingAudioSource.loop = loopTypingAudio;
        typingAudioSource.volume = typingVolume;

        if (typingAudioSource.clip == null && typingClip != null)
            typingAudioSource.clip = typingClip;

        if (!typingAudioSource.isPlaying && typingAudioSource.clip != null)
            typingAudioSource.Play();
    }

    private void StopTypingSound()
    {
        if (typingAudioSource == null) return;
        if (typingAudioSource.isPlaying)
            typingAudioSource.Stop();
    }
}
