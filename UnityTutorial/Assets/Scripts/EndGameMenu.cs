using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameMenu : MonoBehaviour
{
    public Text resultText;

    void Start()
    {
        resultText.text = $"Você coletou {GameController.collected} de {GameController.total} triângulos!";
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene("Game");
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
