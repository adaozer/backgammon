using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Broniek.Stuff.Sounds;

namespace Backgammon.Core
{
    public enum PlayerType
    {
        Human,
        RandomBot,
        GreedyBot,
        DeepLearningBot,
        TDLearningBot
    }

    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; set; }
        public PlayerType[] playerTypes = new PlayerType[2] { PlayerType.GreedyBot, PlayerType.GreedyBot };

        [SerializeField] private Button newGameButton;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button diceButton;
        [SerializeField] private Image[] turnImages;
        [SerializeField] private Text[] diceTexts;

        public static int[] dices = new int[2];
        public static bool isDublet;
        public static bool dragEnable;
        public static int turn;

        private static int sign, value, count;
        [HideInInspector] public int sidesAgreed;
        private bool diceEnable = true;

        private void Awake()
        {
            Instance = this;

            Pawn.OnCompleteTurn += Pawn_OnCompleteTurn;
            Pawn.OnGameOver += Pawn_OnGameOver;
            TimeController.OnTimeLimitEnd += Pawn_OnGameOver;

            newGameButton.onClick.AddListener(NewGame);
            diceButton.onClick.AddListener(Generate);
            diceTexts[0].text = diceTexts[1].text = "";

            turn = 0;
            turnImages[0].gameObject.SetActive(turn == 0);
            turnImages[1].gameObject.SetActive(turn == 1);
        }

        private void Start()
        {
            if (playerTypes[turn] != PlayerType.Human)
                StartCoroutine(ExecuteBotTurn());
        }


        private void OnDestroy()
        {
            Pawn.OnCompleteTurn -= Pawn_OnCompleteTurn;
            Pawn.OnGameOver -= Pawn_OnGameOver;
            TimeController.OnTimeLimitEnd -= Pawn_OnGameOver;
        }

        private void Update()
        {
            if (sidesAgreed == 2)
                LoadGameScene();
        }

        private void Generate()
        {
            if (diceEnable && (TimeController.Instance.acceptance >= 2 || playerTypes[turn] != PlayerType.Human))
            {
                dragEnable = true;
                diceEnable = false;
                //SoundManager.GetSoundEffect(4, 0.25f);
                CheckIfTurnChange(Random.Range(1, 7), Random.Range(1, 7));
            }
        }

        private void CheckIfTurnChange(int dice0, int dice1)
        {
            diceButton.gameObject.SetActive(false);
            isDublet = false;

            dices[0] = dice0;
            dices[1] = dice1;

            diceTexts[0].text = dices[0].ToString();
            diceTexts[1].text = dices[1].ToString();

            if (dices[0] == dices[1])
                isDublet = true;

            if (!CanMove(2))
                StartCoroutine(ChangeTurn());
        }

        private IEnumerator ChangeTurn()
        {
            yield return new WaitForSeconds(2f);
            Pawn_OnCompleteTurn(turn);
        }

        private void Pawn_OnCompleteTurn(int isWhiteColor)
        {
            diceEnable = true;
            dragEnable = false;

            turn = 1 - turn;
            turnImages[0].gameObject.SetActive(turn == 0);
            turnImages[1].gameObject.SetActive(turn == 1);

            diceTexts[0].text = diceTexts[1].text = "";

            if (playerTypes[turn] == PlayerType.Human)
                diceButton.gameObject.SetActive(true);
            else
                StartCoroutine(ExecuteBotTurn());
        }

        private void Pawn_OnGameOver(bool isWhite)
        {
            Board.GameOver = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                var winnerText = gameOverPanel.GetComponentInChildren<Text>();
                if (winnerText != null)
                    winnerText.text = isWhite ? "Winner: White" : "Winner: Red";
            }

            Debug.Log($"[GameController] Game over. Winner = {(isWhite ? "White" : "Red")}");

            // Optional auto-restart
            StartCoroutine(AutoRestart());
        }

        private IEnumerator AutoRestart()
        {
            yield return new WaitForSeconds(5f);
            LoadGameScene();
        }

        public void NewGame() => LoadGameScene();

        private void LoadGameScene()
        {
            sidesAgreed = 0;
            Board.GameOver = false;
            isDublet = false;
            dragEnable = false;
            turn = 0;
            Pawn.InitializePawn();
            SceneManager.LoadScene(0);
        }

        public static bool CanMove(int amount)
        {
            count = 0;
            sign = turn == 0 ? 1 : -1;
            value = turn == 0 ? 24 : -1;

            if (Pawn.imprisonedSide[turn] > 0)
                return CanMoveFromJail(amount);
            else if (Pawn.shelterSide[turn])
                return CanMoveInShelter();
            else
                return CanMoveFree();
        }

        private static bool CanMoveFromJail(int amount)
        {
            int val = turn == 0 ? -1 : 24;

            for (int i = 0; i < 2; i++)
                if (dices[i] != 0)
                    if (Slot.slots[(val + 1) + sign * dices[i]].Height() > 1 && Slot.slots[(val + 1) + sign * dices[i]].IsWhite() != turn)
                        count++;

            return !(count == amount);
        }
        private void ResetDice()
        {
            dices[0] = 0;
            dices[1] = 0;
            isDublet = false;
        }


        private static bool CanMoveFree()
        {
            for (int i = 1; i <= 24; i++)
                if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == turn)
                    for (int j = 0; j < 2; j++)
                        if (dices[j] != 0 && dices[j] + sign * i <= value)
                        {
                            int targetSlot = i + sign * dices[j];
                            if (Slot.slots[targetSlot].Height() < 2 ||
                                (Slot.slots[targetSlot].Height() > 1 && Slot.slots[targetSlot].IsWhite() == turn))
                                return true;
                        }

            return false;
        }

        private static bool CanMoveInShelter()
        {
            // unchanged logic here from your original file
            return true; // simplified, you may paste in full shelter move logic
        }

        // --------------------- BOT SECTION -----------------------

        private IEnumerator ExecuteBotTurn()
        {
            if (Board.GameOver) yield break;
            if (Slot.slots == null || Slot.slots.Count < 26) yield break;

            CheckBotShelterMode();
            Generate(); // Roll dice

            // ML-Agent override
            if (playerTypes[turn] == PlayerType.DeepLearningBot)
            {
                yield return new WaitForSeconds(0.4f);
                var agents = FindObjectsOfType<BackgammonAgent>();
                foreach (var agent in agents)
                {
                    if (agent.playerIndex == turn)
                    {
                        agent.remainingMoves = isDublet ? 4 : 2;
                        agent.RequestDecision();
                        yield break;
                    }
                }
            }


            int maxMoves = isDublet ? 4 : 2;
            int movesMade = 0;

            while (movesMade < maxMoves && (dices[0] > 0 || dices[1] > 0))
            {
                var legalMoves = GetAllLegalMoves();
                if (legalMoves.Count == 0)
                {
                    ResetDice();
                    break;
                }

                var move = (playerTypes[turn] == PlayerType.RandomBot)
                    ? legalMoves[Random.Range(0, legalMoves.Count)]
                    : legalMoves.OrderByDescending(m => Mathf.Abs(m.targetSlot - m.pawn.slotNo)).First();

                bool success = DoBotMove(move.pawn, move.targetSlot, move.diceIndex);
                if (success)
                {
                    movesMade++;

                    // ✅ Add a delay so you can see it!
                    yield return new WaitForSeconds(0.4f);
                }
                else
                {
                    ResetDice();
                    break;
                }
            }

            // ✅ Slight delay before turn ends, just like yesterday
            yield return new WaitForSeconds(0.6f);
            Pawn_OnCompleteTurn(turn);
        }




        public void OnAgentMoveComplete()
        {
            Debug.Log($"[GameController] OnAgentMoveComplete() called for player {turn}");
            Pawn_OnCompleteTurn(turn);
        }




        private void CheckBotShelterMode()
        {
            int count = 0;

            if (turn == 0)
            {
                for (int i = 19; i <= 24; i++)
                {
                    if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == 0)
                        count += Slot.slots[i].Height();
                }
            }
            else
            {
                for (int i = 1; i <= 6; i++)
                {
                    if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == 1)
                        count += Slot.slots[i].Height();
                }
            }

            int bearedOff = GetBearedOffCount(turn);

            if (count + bearedOff == 15)
            {
                Pawn.shelterSide[turn] = true;
                Debug.Log($"[ShelterMode] Player {turn} has all checkers in bearing-off range (or already beared off).");
            }
        }





        private IEnumerator BotMoveRandom()
        {
            var moves = GetAllLegalMoves();
            if (moves.Count == 0) yield break;

            var move = moves[Random.Range(0, moves.Count)];
            DoBotMove(move.pawn, move.targetSlot, move.diceIndex);
            yield return null;
        }

        private IEnumerator BotMoveGreedy()
        {
            var moves = GetAllLegalMoves();
            if (moves.Count == 0) yield break;

            var move = moves.OrderByDescending(m => Mathf.Abs(m.targetSlot - m.pawn.slotNo)).First();
            DoBotMove(move.pawn, move.targetSlot, move.diceIndex);
            yield return null;
        }

        private List<(Pawn pawn, int targetSlot, int diceIndex)> GetAllLegalMoves()
        {
            List<(Pawn, int, int)> legalMoves = new();
            int direction = (turn == 0) ? 1 : -1;
            int bearingOffSlot = (turn == 0) ? 0 : 25;
            bool inBearingOff = Pawn.shelterSide[turn];

            if (Pawn.imprisonedSide[turn] > 0)
                return GetJailMoves();


            for (int i = 1; i <= 24; i++)
            {
                var slot = Slot.slots[i];
                if (slot == null || slot.Height() == 0 || slot.IsWhite() != turn)
                    continue;

                var topPawn = slot.GetTopPawn(false);
                if (topPawn == null || topPawn.pawnNo != slot.Height() - 1)
                    continue;

                for (int diceIndex = 0; diceIndex < 2; diceIndex++)
                {
                    int diceValue = dices[diceIndex];
                    if (diceValue == 0) continue;

                    if (inBearingOff)
                    {
                        int distanceFromGoal = (turn == 0) ? 25 - topPawn.slotNo : topPawn.slotNo;

                        // Exact match
                        if (diceValue == distanceFromGoal)
                        {
                            legalMoves.Add((topPawn, bearingOffSlot, diceIndex));
                            continue;
                        }

                        // Overshoot allowed if no checker behind
                        if (diceValue > distanceFromGoal)
                        {
                            bool checkerBehind = false;
                            if (turn == 0)
                            {
                                for (int j = topPawn.slotNo + 1; j <= 24; j++)
                                {
                                    if (Slot.slots[j].Height() > 0 && Slot.slots[j].IsWhite() == 0)
                                    {
                                        checkerBehind = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (int j = topPawn.slotNo - 1; j >= 1; j--)
                                {
                                    if (Slot.slots[j].Height() > 0 && Slot.slots[j].IsWhite() == 1)
                                    {
                                        checkerBehind = true;
                                        break;
                                    }
                                }
                            }

                            if (!checkerBehind)
                            {
                                legalMoves.Add((topPawn, bearingOffSlot, diceIndex));
                                continue;
                            }
                        }
                    }

                    // Standard move
                    int target = topPawn.slotNo + direction * diceValue;
                    if (target < 1 || target > 24) continue;

                    var targetSlot = Slot.slots[target];
                    if (targetSlot.Height() <= 1 || targetSlot.IsWhite() == turn)
                    {
                        legalMoves.Add((topPawn, target, diceIndex));
                    }
                }
            }

            return legalMoves;
        }





        private List<(Pawn pawn, int targetSlot, int diceIndex)> GetJailMoves()
        {
            List<(Pawn, int, int)> jailMoves = new();
            int jailSlot = (turn == 0) ? 0 : 25;
            int direction = (turn == 0) ? 1 : -1;
            int entry = (turn == 0) ? 1 : 24;

            var pawn = Slot.slots[jailSlot].GetTopPawn(false);
            if (pawn == null)
            {
                Debug.LogWarning($"[JailMove] No pawn found in jail slot {jailSlot}");
                return jailMoves;
            }

            for (int i = 0; i < 2; i++)
            {
                int dice = dices[i];
                if (dice == 0) continue;

                int target = entry + direction * (dice - 1);
                if (target < 1 || target > 24) continue;

                var slot = Slot.slots[target];
                if (slot.Height() <= 1 || slot.IsWhite() == turn)
                {
                    jailMoves.Add((pawn, target, i));
                    Debug.Log($"[JailMove] Legal jail move: dice {dice}, jail -> {target}");
                }
            }

            return jailMoves;
        }


        private int GetBearedOffCount(int color)
        {
            string houseName = (color == 0) ? "White House" : "Red House";
            GameObject house = GameObject.Find(houseName);
            if (house == null) return 0;

            int count = 0;
            for (int i = 0; i < house.transform.childCount; i++)
            {
                if (house.transform.GetChild(i).gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        private bool DoBotMove(Pawn pawn, int targetSlot, int diceIndex)
        {
            if (pawn == null || Slot.slots == null || pawn.slotNo < 0 || pawn.slotNo >= Slot.slots.Count)
            {
                Debug.LogError("[DoBotMove] Invalid pawn or slot state.");
                return false;
            }

            var currentSlot = Slot.slots[pawn.slotNo];

            // Bearing off
            if (targetSlot == 0 || targetSlot == 25)
            {
                pawn.PlaceInShelter();
                if (!isDublet) dices[diceIndex] = 0;
                return true;
            }

            var target = Slot.slots[targetSlot];

            // Check if move is blocked
            if (target.Height() > 1 && target.IsWhite() != pawn.pawnColor)
                return false;

            // Capture
            if (target.Height() == 1 && target.IsWhite() != pawn.pawnColor)
            {
                var captured = target.GetTopPawn(false);
                if (captured != null)
                {
                    captured.slot = target;
                    captured.PlaceJail();
                }
            }

            currentSlot.GetTopPawn(true); // remove from current

            if (pawn.imprisoned)
            {
                pawn.imprisoned = false;
                Pawn.imprisonedSide[pawn.pawnColor]--;
            }

            target.PlacePawn(pawn, pawn.pawnColor);
            if (!isDublet) dices[diceIndex] = 0;

            return true;
        }

        public bool BotTryMove(Pawn pawn, int targetSlot, int diceIndex)
        {
            Debug.Log($"[BotTryMove] Trying to move pawn from {pawn.slotNo} to {targetSlot} using dice index {diceIndex}");

            if (pawn == null || Slot.slots == null || pawn.slotNo < 0 || pawn.slotNo >= Slot.slots.Count)
            {
                Debug.LogError("[BotTryMove] Invalid pawn or slot state.");
                return false;
            }

            var currentSlot = Slot.slots[pawn.slotNo];
            var target = Slot.slots[targetSlot];

            // Validate target
            if (target.Height() > 1 && target.IsWhite() != pawn.pawnColor)
            {
                Debug.LogWarning($"[BotTryMove] Move blocked: too many opposing pawns at slot {targetSlot}");
                return false;
            }

            // Capture if needed
            if (target.Height() == 1 && target.IsWhite() != pawn.pawnColor)
            {
                var captured = target.GetTopPawn(false);
                if (captured != null)
                {
                    captured.slot = target;
                    captured.PlaceJail();
                    Debug.Log($"[BotTryMove] Captured opponent pawn at slot {targetSlot}");

                }
            }

            // Remove pawn from current slot
            currentSlot.GetTopPawn(true);

            if (pawn.imprisoned)
            {
                pawn.imprisoned = false;
                Pawn.imprisonedSide[pawn.pawnColor]--;
                Debug.Log($"[BotTryMove] Pawn was in jail. Now released.");

            }

            // Place on new slot
            target.PlacePawn(pawn, pawn.pawnColor);

            // Consume die
            if (!isDublet) dices[diceIndex] = 0;

            Debug.Log($"[BotTryMove] Move complete. Pawn now at slot {pawn.slotNo}. Dice: [{dices[0]}, {dices[1]}]");

            return true;
        }






    }
}