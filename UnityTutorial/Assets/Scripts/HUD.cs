using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    public Image[] hearts;
    public Image fuelFill;
    public Text gasText;
    public Text timeText;
    public Text messageText;

    static readonly Color HeartFull = new Color(0.95f, 0.2f, 0.25f);
    static readonly Color HeartEmpty = new Color(0.25f, 0.25f, 0.28f);

    float messageTimer;

    void Awake()
    {
        Instance = this;
        messageText.text = "";
    }

    void Update()
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < GameController.lives ? HeartFull : HeartEmpty;

        float fuel = GameController.fuel / GameController.MaxFuel;
        fuelFill.fillAmount = fuel;
        Color fuelColor = Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(0.3f, 0.85f, 0.3f), Mathf.InverseLerp(0.15f, 0.6f, fuel));
        if (fuel < 0.25f && Mathf.Repeat(Time.time, 0.5f) < 0.25f) fuelColor = Color.white;
        fuelFill.color = fuelColor;

        gasText.text = $"Galões: {GameController.collected}/{GameController.GasGoal}";
        timeText.text = $"Tempo: {GameController.FormatTime(GameController.time)}";

        // Esconde a mensagem atrás do painel de pausa
        messageText.enabled = Time.timeScale > 0f;
        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }
    }

    public void ShowMessage(string message, float duration)
    {
        messageText.text = message;
        messageTimer = duration;
    }
}
