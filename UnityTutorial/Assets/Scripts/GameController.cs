using UnityEngine;

public enum GameResult { None, Victory, OutOfFuel, Busted }

// Classe estática: os dados sobrevivem à troca de cena (Game -> EndGame)
public static class GameController
{
    public const int GasGoal = 15;
    public const int MaxLives = 3;
    public const float MaxFuel = 100f;
    public const float FuelPerCan = 35f;

    public static int collected;
    public static int lives;
    public static float fuel;
    public static float time;
    public static GameResult result;
    public static bool newRecord;

    public static bool IsPlaying => result == GameResult.None;

    public static float BestTime
    {
        get => PlayerPrefs.GetFloat("BestTime", 0f);
        set { PlayerPrefs.SetFloat("BestTime", value); PlayerPrefs.Save(); }
    }

    public static void Init()
    {
        collected = 0;
        lives = MaxLives;
        fuel = MaxFuel;
        time = 0f;
        result = GameResult.None;
        newRecord = false;
    }

    public static void Collect()
    {
        collected++;
        fuel = Mathf.Min(MaxFuel, fuel + FuelPerCan);
    }

    public static string FormatTime(float t)
    {
        int minutes = (int)(t / 60);
        return $"{minutes:00}:{t - minutes * 60:00.0}";
    }
}
