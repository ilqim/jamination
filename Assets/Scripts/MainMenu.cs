using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Belirtilen sahneyi yükler
    public void Play(string sahne)
    {
        SceneManager.LoadScene(sahne);
    }

    // Oyunu kapatır
    public void Quit()
    {
        // Build edilmiş oyunda çalışır
        Application.Quit();

        // Eğer Unity Editor'de test ediyorsan, play modunu kapatır
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
