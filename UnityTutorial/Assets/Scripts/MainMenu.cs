using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject howToPanel;
    public GameObject playButton;
    public GameObject howToBackButton;
    public GameObject quitButton;
    public Text bestTimeText;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_WEBGL
        // Application.Quit não faz nada no navegador
        if (quitButton != null) quitButton.SetActive(false);
#endif
        float best = GameController.BestTime;
        bestTimeText.text = best > 0f ? $"Recorde: {GameController.FormatTime(best)}" : "";
        ShowMain();
    }

    public void Play()
    {
        SceneManager.LoadScene("Game");
    }

    public void ShowHowTo()
    {
        mainPanel.SetActive(false);
        howToPanel.SetActive(true);
        SelectionKeeper.SetDefault(howToBackButton);
    }

    public void ShowMain()
    {
        howToPanel.SetActive(false);
        mainPanel.SetActive(true);
        SelectionKeeper.SetDefault(playButton);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
