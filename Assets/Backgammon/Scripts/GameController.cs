using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Broniek.Stuff.Sounds;

namespace Backgammon.Core
{
    public enum BotType { Greedy, Random }

    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; set; }

        [SerializeField] private Button newGameButton;
        //[SerializeField] private Button diceButton; // (removed manual dice–button)
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Image[] turnImages;
        [SerializeField] private Text[] diceTexts;

        public static int[] dices = new int[2];     // recently drawn numbers

        public static bool isDublet;                // whether a doublet was thrown
        public static bool dragEnable;              // is it possible to drag the pieces
        public static int turn;                     // indicates whose turn it is now

        private static bool diceEnable = true;       // permission to roll the dice

        [HideInInspector] public int sidesAgreed;   // the current number of players agreeing to continue the game

        // Bot settings – here both players are set as bots; you can change these later.
        public bool whiteIsBot = true;
        public bool redIsBot = true;
        public BotType whiteBotType = BotType.Greedy;
        public BotType redBotType = BotType.Greedy;

        // A simple structure to hold a legal move
        public struct Move
        {
            public int fromSlot;
            public int diceIndex;
            public int targetSlot;
            public Pawn pawn;
        }

        private void Awake()
        {
            Instance = this;

            Pawn.OnCompleteTurn += Pawn_OnCompleteTurn;
            Pawn.OnGameOver += Pawn_OnGameOver;
            TimeController.OnTimeLimitEnd += Pawn_OnGameOver;

            newGameButton.onClick.AddListener(NewGame);
            // Remove manual dice–button listeners if any:
            // diceButton.onClick.RemoveAllListeners();

            diceTexts[0].text = diceTexts[1].text = "";

            turn = 0;

            turnImages[0].gameObject.SetActive(turn == 0);
            turnImages[1].gameObject.SetActive(turn == 1);

            // Start the game automatically by rolling the dice after a short delay.
            StartCoroutine(AutoRollDice());
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

        // Coroutine to wait a moment and then roll dice automatically.
        private IEnumerator AutoRollDice()
        {
            yield return new WaitForSeconds(1f);
            RollDice();
            if (IsBotTurn())
            {
                yield return StartCoroutine(BotTurn());
            }
            else
            {
                dragEnable = true; // Allow human control
            }
        }


        // Automatically roll the dice and update the UI.
        private void RollDice()
        {
            dragEnable = true;
            diceEnable = false;
            SoundManager.GetSoundEffect(4, 0.25f);
            int d0 = UnityEngine.Random.Range(1, 7);
            int d1 = UnityEngine.Random.Range(1, 7);
            dices[0] = d0;
            dices[1] = d1;
            diceTexts[0].text = d0.ToString();
            diceTexts[1].text = d1.ToString();
            isDublet = (d0 == d1);

            if (!CanMove(2))
                StartCoroutine(ChangeTurn());
        }

        private IEnumerator ChangeTurn()
        {
            yield return new WaitForSeconds(2f);
            Pawn_OnCompleteTurn(turn);
        }

        // When a turn ends (whether by bot moves or otherwise), update the UI and then start the next turn.
        private void Pawn_OnCompleteTurn(int isWhiteColor)
        {
            diceEnable = true;
            dragEnable = false;

            turn = 1 - turn; // switch turn

            turnImages[0].gameObject.SetActive(turn == 0);
            turnImages[1].gameObject.SetActive(turn == 1);

            diceTexts[0].text = diceTexts[1].text = "";

            if (!Board.GameOver)
            {
                if (IsBotTurn())  // Only start auto-play if it's a bot
                {
                    StartCoroutine(AutoRollDice());
                }
                else
                {
                    dragEnable = true; // Let human play
                }
            }
        }


        private void Pawn_OnGameOver(bool isWhite)
        {
            Board.GameOver = true;
            gameOverPanel.SetActive(true);
            gameOverPanel.GetComponentInChildren<Text>().text = isWhite ? "Winner: white" : "Winner: red";
        }

        private void NewGame()
        {
            LoadGameScene();
        }

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

        // --- Methods for checking whether a move is possible (original code) ---

        public static bool CanMove(int amount)
        {
            int count = 0;
            int sign = turn == 0 ? 1 : -1;
            int value = turn == 0 ? 24 : -1;

            if (Pawn.imprisonedSide[turn] > 0)
                return CanMoveFromJail(amount);
            else
            {
                if (Pawn.shelterSide[turn])
                    return CanMoveInShelter();
                else if (CanMoveFree())
                    return true;
            }

            return false;
        }

        private static bool CanMoveFromJail(int amount)
        {
            int sign = turn == 0 ? 1 : -1;
            int val = turn == 0 ? -1 : 24;
            int count = 0;
            for (int i = 0; i < 2; i++)
                if (dices[i] != 0)
                    if (Slot.slots[(val + 1) + sign * dices[i]].Height() > 1 && Slot.slots[(val + 1) + sign * dices[i]].IsWhite() != turn)
                        count++;
            return !(count == amount);
        }

        private static bool CanMoveFree()
        {
            int sign = turn == 0 ? 1 : -1;
            int value = turn == 0 ? 24 : -1;

            for (int i = 1; i <= 24; i++)
            {
                if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == turn)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (dices[j] != 0)
                        {
                            int target = i + sign * dices[j];

                            if (target > 24 || target < 1)
                                continue; // Skip move if out of range

                            if (Slot.slots[target].Height() < 2)
                                return true;
                            else if (Slot.slots[target].Height() > 1 && Slot.slots[target].IsWhite() == turn)
                                return true;
                        }
                    }
                }
            }
            return false;
        }


        private static bool CanMoveInShelter()
        {
            int sign = turn == 0 ? 1 : -1;
            int endSlotNo = turn == 0 ? 19 : 6;
            int value = turn == 0 ? 24 : -1;
            int first = 0;
            for (int j = 0; j < 6; j++)
            {
                if (endSlotNo + sign * j >= 0)
                {
                    if (Slot.slots[endSlotNo + sign * j].Height() > 0)
                    {
                        if (Slot.slots[endSlotNo + sign * j].IsWhite() == turn)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (dices[i] > 0)
                                {
                                    int ind = endSlotNo + sign * (j + dices[i]);
                                    if (ind == value + 1)
                                        return true;
                                    if (first == 0)
                                    {
                                        if (value == 24)
                                        {
                                            if (ind > value + 1)
                                                return true;
                                        }
                                        if (value == -1)
                                        {
                                            if (ind < value + 1)
                                                return true;
                                        }
                                    }
                                    if (ind >= 0 && ind < Slot.slots.Count)
                                    {
                                        if (Slot.slots[ind].Height() > 0)
                                        {
                                            if (Slot.slots[ind].IsWhite() != turn)
                                            {
                                                if (Slot.slots[ind].Height() < 2)
                                                    return true;
                                            }
                                            if (Slot.slots[ind].IsWhite() == turn)
                                                return true;
                                        }
                                        else
                                            return true;
                                    }
                                }
                            }
                            first++;
                        }
                    }
                }
            }
            return false;
        }

        // --- Bot play methods ---

        // Returns true if the current player is controlled by a bot.
        // --- Bot play methods ---

        // Returns true if the current player is controlled by a bot.
        private bool IsBotTurn()
        {
            return (turn == 0 && whiteIsBot) || (turn == 1 && redIsBot);
        }

        // Returns the bot type for the given player.
        private BotType GetBotType(int playerTurn)
        {
            return (playerTurn == 0) ? whiteBotType : redBotType;
        }

        // The bot turn routine: repeatedly get legal moves and execute one move at a time.
        private IEnumerator BotTurn()
        {
            int movesMade = 0;
            int maxMoves = isDublet ? 4 : 2;

            while (movesMade < maxMoves)
            {
                List<Move> legalMoves = GetLegalMoves();

                // **Fix: If there are no moves, pass the turn and exit**
                if (legalMoves.Count == 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    Pawn_OnCompleteTurn(turn);  // Ends the bot's turn
                    yield break;
                }

                Move chosenMove;

                if (GetBotType(turn) == BotType.Greedy)
                {
                    // Greedy agent selects the move that advances the farthest
                    Move bestMove = legalMoves[0]; // Default move
                    int maxDistance = -1;

                    foreach (var move in legalMoves)
                    {
                        int distance = Mathf.Abs(move.targetSlot - move.fromSlot); // Absolute movement distance

                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            bestMove = move;
                        }
                    }

                    chosenMove = bestMove;
                }
                else // Random bot
                {
                    int idx = UnityEngine.Random.Range(0, legalMoves.Count);
                    chosenMove = legalMoves[idx];
                }

                bool success = ExecuteMove(chosenMove);
                if (success)
                    movesMade++;

                yield return new WaitForSeconds(0.5f);
            }

            Pawn_OnCompleteTurn(turn); // End turn when all moves are made
        }




        public List<Move> GetLegalMoves()
        {
            List<Move> movesList = new List<Move>();
            int sign = (turn == 0) ? 1 : -1;
            bool canBearOff = Pawn.shelterSide[turn];

            Debug.Log($"[BOT] Checking legal moves for turn {turn} (White = 0, Red = 1).");

            // ---- 1️⃣ PRIORITIZE MOVING FROM JAIL ----
            if (Pawn.imprisonedSide[turn] > 0) // If there's a checker in jail
            {
                int jailSlot = (turn == 0) ? 0 : 25;  // Jail location for white (0) and red (25)

                // ✅ **Fix: Get the pawn directly from Pawn.imprisonedSide**
                Pawn jailedPawn = null;
                if (Slot.slots[jailSlot].Height() > 0)
                {
                    jailedPawn = Slot.slots[jailSlot].GetTopPawn(false);
                }

                if (jailedPawn == null)
                {
                    Debug.LogError($"[BOT] ERROR: Player {turn} has a checker in jail but no pawn found in slot {jailSlot}!");
                    return new List<Move>(); // Fail-safe, force passing
                }

                List<Move> jailMoves = new List<Move>();

                for (int j = 0; j < 2; j++) // Loop through dice values
                {
                    int die = dices[j];
                    if (die == 0) continue; // Ignore used dice

                    int entrySlot = (turn == 0) ? die : (25 - die); // Compute correct entry slot

                    if (entrySlot >= 1 && entrySlot <= 24)
                    {
                        bool isBlocked = Slot.slots[entrySlot].Height() > 1 && Slot.slots[entrySlot].IsWhite() != turn;

                        if (!isBlocked)
                        {
                            jailMoves.Add(new Move
                            {
                                fromSlot = jailSlot,
                                diceIndex = j,
                                targetSlot = entrySlot,
                                pawn = jailedPawn
                            });

                            Debug.Log($"[BOT] Jail move to {entrySlot} is VALID.");
                        }
                        else
                        {
                            Debug.Log($"[BOT] Jail move to {entrySlot} is BLOCKED.");
                        }
                    }
                }

                if (jailMoves.Count > 0)
                {
                    Debug.Log($"[BOT] Found {jailMoves.Count} jail moves. Bot MUST move from jail.");
                    return jailMoves;
                }

                Debug.Log($"[BOT] No valid jail moves detected. Bot WILL PASS.");
                return new List<Move>(); // Force pass if no moves found
            }




            Debug.Log($"[BOT] No checkers in jail, checking normal board moves.");

            // ---- 2️⃣ NORMAL BOARD MOVES ----
            for (int i = 1; i <= 24; i++)
            {
                if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == turn)
                {
                    Pawn pawn = Slot.slots[i].GetTopPawn(false);
                    for (int j = 0; j < 2; j++)
                    {
                        int die = dices[j];
                        if (die == 0) continue;

                        int target = i + sign * die;
                        if (target >= 1 && target <= 24)
                        {
                            if (!(Slot.slots[target].Height() > 1 && Slot.slots[target].IsWhite() != turn))
                            {
                                movesList.Add(new Move
                                {
                                    fromSlot = i,
                                    diceIndex = j,
                                    targetSlot = target,
                                    pawn = pawn
                                });

                                Debug.Log($"[BOT] Normal move found: {i} -> {target} using dice {die}");
                            }
                            else
                            {
                                Debug.Log($"[BOT] Move from {i} to {target} blocked.");
                            }
                        }
                    }
                }
            }

            // ---- 3️⃣ BEARING OFF MOVES ----
            if (canBearOff)
            {
                Debug.Log($"[BOT] Checking bearing off moves for Player {turn}");

                int homeStart = (turn == 0) ? 19 : 6;
                int homeEnd = (turn == 0) ? 24 : 1;
                List<Move> bearOffMoves = new List<Move>();

                for (int i = homeStart; i <= homeEnd; i++)
                {
                    if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == turn)
                    {
                        Pawn pawn = Slot.slots[i].GetTopPawn(false);

                        for (int j = 0; j < 2; j++)
                        {
                            int die = dices[j];
                            if (die == 0) continue;

                            int target = i + sign * die;
                            Debug.Log($"[BOT] Checking if checker at {i} can bear off with dice {die}");

                            // ✅ **Fix: Ensure that bearing off is allowed even if the die exceeds the board edge**
                            if ((turn == 0 && (target >= 25 || i == 24)) || (turn == 1 && (target <= 0 || i == 1)))
                            {
                                Debug.Log($"[BOT] Bearing off checker from {i} using dice {die}");
                                bearOffMoves.Add(new Move
                                {
                                    fromSlot = i,
                                    diceIndex = j,
                                    targetSlot = -1, // Bearing off
                                    pawn = pawn
                                });
                            }
                            else
                            {
                                Debug.Log($"[BOT] Move {i} -> {target} is not bearing off yet.");
                            }
                        }
                    }
                }

                if (bearOffMoves.Count > 0)
                {
                    Debug.Log($"[BOT] Found {bearOffMoves.Count} bearing off moves.");
                    return bearOffMoves;
                }

                Debug.Log($"[BOT] No bearing off moves found.");
            }



            Debug.Log($"[BOT] Total legal moves found: {movesList.Count}");

            return movesList; // Return all legal moves
        }




        // Execute the chosen move.
        private bool ExecuteMove(Move move)
        {
            Slot fromSlot = Slot.slots[move.fromSlot];

            // ---- 1️⃣ HANDLE BEARING OFF ----
            if (move.targetSlot == -1)
            {
                fromSlot.GetTopPawn(true); // Remove the piece
                if (!isDublet)
                    dices[move.diceIndex] = 0;
                SoundManager.GetSoundEffect(3, 0.5f);

                // 🔥 CHECK IF GAME IS OVER
                if (Pawn.CountRemainingCheckers(turn) == 0)
                {
                    Pawn_OnGameOver(turn == 0);
                }

                return true;
            }

            // ---- 2️⃣ NORMAL MOVE ----
            Slot targetSlot = Slot.slots[move.targetSlot];

            // If the target slot has one opponent checker, capture it
            if (targetSlot.Height() == 1 && targetSlot.IsWhite() != turn)
            {
                Pawn captured = targetSlot.GetTopPawn(true);
                int jailSlot = (turn == 0) ? 0 : 25;
                Slot.slots[jailSlot].PlacePawn(captured, captured.pawnColor);
                Pawn.imprisonedSide[captured.pawnColor]++;
                Pawn.shelterSide[captured.pawnColor] = false;
                SoundManager.GetSoundEffect(2, 0.8f);
            }

            Pawn pawn = fromSlot.GetTopPawn(true);
            if (pawn == null)
                return false;

            targetSlot.PlacePawn(pawn, pawn.pawnColor);

            if (!isDublet)
                dices[move.diceIndex] = 0;

            SoundManager.GetSoundEffect(1, 0.2f);
            return true;
        }




    }

}