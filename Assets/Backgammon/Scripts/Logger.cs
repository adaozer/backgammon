using UnityEngine;
using System.IO;

public static class Logger
{
    private static string filePath = Path.Combine(Application.dataPath, "GameLogs.csv");
    private static int gameCounter = 0;

    static Logger()
    {
        if (!File.Exists(filePath))
        {
            File.AppendAllText(filePath, "GameID,Winner,WhiteMoves,RedMoves,WhiteType,RedType,TimeStamp,DiceRolls,MoveLog\n");
        }
    }

    public static void LogGameResult(bool whiteWon, int whiteMoves, int redMoves, string whiteType, string redType, string diceHistory, string moveLog)
    {
        string winner = whiteWon ? "White" : "Red";
        string line = $"{++gameCounter},{winner},{whiteMoves},{redMoves},{whiteType},{redType},{System.DateTime.Now},{diceHistory},\"{moveLog}\"";
        File.AppendAllText(filePath, line + "\n");
        Debug.Log($"[Logger] Logged game #{gameCounter}");
    }
}
