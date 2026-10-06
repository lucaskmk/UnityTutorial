using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject quitButton;

    void Start()
    {
#if UNITY_WEBGL
        // Application.Quit não faz nada no navegador
        if (quitButton != null) quitButton.SetActive(false);
#endif
    }

    public void Play()
    {
        SceneManager.LoadScene("Game");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
