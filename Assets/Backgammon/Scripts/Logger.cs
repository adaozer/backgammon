using UnityEngine;
using System.IO;

public static class Logger
{
    private static string filePath;
    private static string counterFilePath;
    private static int gameCounter;
    private const int maxGames = 1000;

    private static int whiteWins = 0;
    private static int redWins = 0;

    public static void Initialize(string logFileName)
    {
        string logDir = Path.Combine(Application.dataPath, "..", "BackgammonLogs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        filePath = Path.Combine(logDir, logFileName);
        counterFilePath = Path.Combine(logDir, logFileName + ".count");

        if (File.Exists(counterFilePath))
        {
            string countText = File.ReadAllText(counterFilePath);
            int.TryParse(countText, out gameCounter);
        }
        else
        {
            gameCounter = 0;
        }

        if (!File.Exists(filePath))
        {
            File.AppendAllText(filePath, "\"GameID\",\"Winner\"\n");
        }

        Debug.Log($"[Logger] Logging to: {filePath}");
    }

    public static void LogWinnerOnly(bool whiteWon)
    {
        if (gameCounter >= maxGames)
        {
            Debug.Log("[Logger] Max games already reached. Terminating.");
            Terminate();
            return;
        }

        string winner = whiteWon ? "White" : "Red";
        if (whiteWon) whiteWins++;
        else redWins++;

        string line = $"\"{++gameCounter}\",\"{winner}\"";
        File.AppendAllText(filePath, line + "\n");
        File.WriteAllText(counterFilePath, gameCounter.ToString());

        Debug.Log($"[Logger] Logged game #{gameCounter} | Winner: {winner}");

        if (gameCounter >= maxGames)
        {
            WriteSummaryRow();
            Debug.Log("[Logger] Max games reached. Writing summary and terminating.");
            Terminate();
        }
    }

    public static void WriteSummaryRow()
    {
        int total = whiteWins + redWins;
        float whiteRate = total > 0 ? (float)whiteWins / total * 100f : 0f;
        float redRate = total > 0 ? (float)redWins / total * 100f : 0f;

        string summary = $"\n\"SUMMARY\"\n\"Total Games\",\"{total}\"\n\"White Wins\",\"{whiteWins} ({whiteRate:F2}%)\"\n\"Red Wins\",\"{redWins} ({redRate:F2}%)\"\n";
        File.AppendAllText(filePath, summary);
    }

    private static void Terminate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static bool ReachedLimit() => gameCounter >= maxGames;
    public static int GetGameCount() => gameCounter;
}
