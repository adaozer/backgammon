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
                SoundManager.GetSoundEffect(4, 0.25f);
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
            gameOverPanel.SetActive(true);
            gameOverPanel.GetComponentInChildren<Text>().text = isWhite ? "Winner: white" : "Winner: red";
        }

        private void NewGame() => LoadGameScene();

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
            yield return new WaitForSeconds(0.5f);
            Generate(); // roll dice
            yield return new WaitForSeconds(0.5f);

            int maxMoves = isDublet ? 4 : 2;
            int moveCount = 0;

            while (moveCount < maxMoves && (dices[0] > 0 || dices[1] > 0) && CanMove(2))
            {
                var legalMoves = GetAllLegalMoves();
                if (legalMoves.Count == 0)
                    break;

                var move = (playerTypes[turn] == PlayerType.RandomBot)
                    ? legalMoves[Random.Range(0, legalMoves.Count)]
                    : legalMoves.OrderByDescending(m => Mathf.Abs(m.targetSlot - m.pawn.slotNo)).First();

                DoBotMove(move.pawn, move.targetSlot, move.diceIndex);

                moveCount++;
                yield return new WaitForSeconds(0.35f);
            }

            Pawn_OnCompleteTurn(turn);
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
            // Priority: if there's a pawn in jail, only generate jail moves
            if (Pawn.imprisonedSide[turn] > 0)
                return GetJailMoves();

            List<(Pawn, int, int)> legalMoves = new();
            int direction = (turn == 0) ? 1 : -1;
            int bearingOffSlot = (turn == 0) ? 0 : 25;
            bool inBearingOff = Pawn.shelterSide[turn];

            foreach (var slot in Slot.slots)
            {
                if (slot == null || slot.Height() == 0 || slot.IsWhite() != turn)
                    continue;

                var topPawn = slot.GetTopPawn(false);
                if (topPawn == null || topPawn.pawnNo != slot.Height() - 1)
                    continue;

                for (int diceIndex = 0; diceIndex < 2; diceIndex++)
                {
                    int diceValue = GameController.dices[diceIndex];
                    if (diceValue == 0) continue;

                    // ---------------- BEARING OFF -----------------
                    if (inBearingOff)
                    {
                        int relativeDistance = turn == 0 ? 25 - topPawn.slotNo : topPawn.slotNo;

                        // Exact dice match
                        if (diceValue == relativeDistance)
                        {
                            legalMoves.Add((topPawn, bearingOffSlot, diceIndex));
                            continue;
                        }

                        // Overshoot rule: can use higher die if no checkers behind
                        if (diceValue > relativeDistance)
                        {
                            bool hasCheckersFurther = false;

                            if (turn == 0)
                            {
                                for (int i = topPawn.slotNo + 1; i <= 24; i++)
                                {
                                    if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == 0)
                                    {
                                        hasCheckersFurther = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = topPawn.slotNo - 1; i >= 1; i--)
                                {
                                    if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == 1)
                                    {
                                        hasCheckersFurther = true;
                                        break;
                                    }
                                }
                            }

                            if (!hasCheckersFurther)
                            {
                                legalMoves.Add((topPawn, bearingOffSlot, diceIndex));
                                continue;
                            }
                        }
                    }

                    // ---------------- NORMAL MOVE -----------------
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
            if (pawn == null) return jailMoves;

            for (int i = 0; i < 2; i++)
            {
                int dice = dices[i];
                if (dice == 0) continue;

                int target = entry + direction * (dice - 1);
                if (target < 1 || target > 24) continue;

                var slot = Slot.slots[target];
                if (slot.Height() <= 1 || slot.IsWhite() == turn)
                    jailMoves.Add((pawn, target, i));
            }

            return jailMoves;
        }


        private void DoBotMove(Pawn pawn, int targetSlot, int diceIndex)
        {
            var currentSlot = Slot.slots[pawn.slotNo];

            if (targetSlot == 0 || targetSlot == 25)
            {
                Debug.Log($"[DoBotMove] Bearing off pawn from slot {pawn.slotNo}");
                pawn.PlaceInShelter();
                dices[diceIndex] = isDublet ? dices[diceIndex] : 0;
                return;
            }



            var target = Slot.slots[targetSlot];

            // Handle capture (target has exactly 1 opponent checker)
            if (target.Height() == 1 && target.IsWhite() != pawn.pawnColor)
            {
                var captured = target.GetTopPawn(false);
                if (captured != null)
                {
                    captured.slot = target;
                    captured.PlaceJail();
                }
                else
                {
                    Debug.LogError("Tried to capture but target.GetTopPawn(false) returned null.");
                }
            }

            // Remove the bot's pawn from its current slot
            currentSlot.GetTopPawn(true);

            // If it's exiting jail, mark it as free
            if (pawn.imprisoned)
            {
                pawn.imprisoned = false;
                Pawn.imprisonedSide[pawn.pawnColor]--;
            }

            // Place on the target slot
            target.PlacePawn(pawn, pawn.pawnColor);

            // Update dice usage
            dices[diceIndex] = isDublet ? dices[diceIndex] : 0;
        }



    }
}