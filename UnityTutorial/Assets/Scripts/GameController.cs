using UnityEngine;

// Classe estática: os dados sobrevivem à troca de cena (Game -> EndGame)
public static class GameController
{
    public static int collected;
    public static int total;

    public static bool IsGameOver => total > 0 && collected >= total;

    public static void Init()
    {
        collected = 0;
        total = GameObject.FindGameObjectsWithTag("Coletavel").Length;
    }

    public static void Collect()
    {
        collected++;
    }
}
