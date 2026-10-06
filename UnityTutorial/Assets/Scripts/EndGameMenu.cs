using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameMenu : MonoBehaviour
{
    public Text titleText;
    public Text reasonText;
    public Text statsText;
    public GameObject firstButton;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        switch (GameController.result)
        {
            case GameResult.Victory:
                titleText.text = "Você escapou!";
                titleText.color = new Color(0.4f, 0.9f, 0.4f);
                reasonText.text = "Tanque cheio e a polícia comendo poeira.";
                break;
            case GameResult.OutOfFuel:
                titleText.text = "Sem gasolina!";
                titleText.color = new Color(1f, 0.75f, 0.2f);
                reasonText.text = "O carro parou no meio da rua...";
                break;
            default:
                titleText.text = "Preso!";
                titleText.color = new Color(0.95f, 0.3f, 0.3f);
                reasonText.text = "A polícia te pegou.";
                break;
        }

        string stats = $"Galões coletados: {GameController.collected}/{GameController.GasGoal}\n" +
                       $"Tempo final: {GameController.FormatTime(GameController.time)}";
        if (GameController.newRecord) stats += "\nNOVO RECORDE!";
        else if (GameController.BestTime > 0f) stats += $"\nRecorde: {GameController.FormatTime(GameController.BestTime)}";
        statsText.text = stats;

        SelectionKeeper.SetDefault(firstButton);
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
