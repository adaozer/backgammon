using UnityEngine;
using System;
using Broniek.Stuff.Sounds;

namespace Backgammon.Core
{
    public class Pawn : MonoBehaviour
    {
        public static event Action<int> OnCompleteTurn = delegate { };
        public static event Action<bool> OnGameOver = delegate { };

        public static int[] imprisonedSide = new int[2];
        public static bool[] shelterSide = new bool[2];
        public static int endSlotNo;
        private static int moves;

        public int pawnColor;
        public int slotNo;
        public int pawnNo;

        public Slot slot;
        private Vector3 startPos;
        private GameObject go;
        private bool isDown;
        public bool imprisoned; // made public for bot logic
        private bool shelter;
        private int rescuedPawns;
        private int maxMoves;

        public void SetColor(int color)
        {
            GetComponent<SpriteRenderer>().color = color == 0 ? Color.white : Color.red;
            pawnColor = color;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Slot")) slot = other.GetComponent<Slot>();
            else if (other.CompareTag("Shelter"))
                if (shelterSide[0] || shelterSide[1])
                    shelter = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Slot")) slot = Slot.slots[slotNo];
            else if (other.CompareTag("Shelter")) shelter = false;
        }

        private void OnMouseDown()
        {
            if (!Board.GameOver)
            {
                if (!imprisoned && ((imprisonedSide[0] > 0 && pawnColor == 0) || (imprisonedSide[1] > 0 && pawnColor == 1)))
                    return;

                TrySelectPawn();
            }
        }

        private void TrySelectPawn()
        {
            if (GameController.dragEnable && GameController.turn == pawnColor)
                if (Slot.slots[slotNo].Height() - 1 == pawnNo)
                {
                    startPos = transform.position;
                    isDown = true;
                    TryHighlight(true);
                }
        }

        private void OnMouseDrag()
        {
            if (isDown)
            {
                Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                transform.position = new Vector3(worldPos.x, worldPos.y, -1);
            }
        }

        private void OnMouseUp()
        {
            if (isDown)
            {
                TryHighlight(false);
                isDown = false;

                if (IsPatology()) return;
                CheckShelterStage();

                if (TryPlace()) CheckShelterAndMore();

                CheckIfNextTurn();
            }
        }

        private void CheckIfNextTurn()
        {
            if (moves == maxMoves && !Board.GameOver)
            {
                moves = 0;
                OnCompleteTurn(pawnColor);
            }
        }

        private void TryRemovePawnFromJail()
        {
            if (imprisonedSide[pawnColor] > 0 && imprisoned)
            {
                imprisoned = false;
                imprisonedSide[pawnColor]--;
            }
        }

        private void CheckShelterAndMore()
        {
            if (slotNo != 0 && slotNo != 25) TryRemovePawnFromJail();

            if (CheckShelterStage())
                shelterSide[pawnColor] = true;

            if (++moves < maxMoves)
            {
                if (!GameController.CanMove(1))
                {
                    moves = 0;
                    OnCompleteTurn(pawnColor);
                }
            }
        }

        private bool IsPatology()
        {
            if (slot.slotNo == 0 || slot.slotNo == 25 ||
                (slot.Height() > 1 && slot.IsWhite() != pawnColor))
            {
                transform.position = startPos;
                return true;
            }

            return false;
        }

        private bool TryPlace()
        {
            if (shelter)
            {
                if (shelterSide[pawnColor] && CanPlaceShelter())
                    return true;

                transform.position = startPos;
                return false;
            }
            else
            {
                if (slot.slotNo == slotNo)
                {
                    transform.position = startPos;
                    return false;
                }

                int sign = pawnColor == 0 ? 1 : -1;

                if (slot.slotNo == slotNo + sign * GameController.dices[0]) DoCorrectMove(0);
                else if (slot.slotNo == slotNo + sign * GameController.dices[1]) DoCorrectMove(1);
                else
                {
                    transform.position = startPos;
                    return false;
                }

                return true;
            }
        }

        private void TryHighlight(bool state)
        {
            int sign = pawnColor == 0 ? 1 : -1;
            int slot0 = slotNo + sign * GameController.dices[0];
            int slot1 = slotNo + sign * GameController.dices[1];

            if (slot0 > 0 && slot0 < 25 && slot0 != slotNo)
                if (!(Slot.slots[slot0].Height() > 1 && Slot.slots[slot0].IsWhite() != pawnColor))
                    Slot.slots[slot0].HightlightMe(state);

            if (slot1 > 0 && slot1 < 25 && slot1 != slotNo)
                if (!(Slot.slots[slot1].Height() > 1 && Slot.slots[slot1].IsWhite() != pawnColor))
                    Slot.slots[slot1].HightlightMe(state);
        }

        private void DoCorrectMove(int diceNo)
        {
            if (slot.Height() == 1 && slot.IsWhite() != pawnColor)
                PlaceJail();

            Slot.slots[slotNo].GetTopPawn(true);
            slot.PlacePawn(this, pawnColor);

            if (!GameController.isDublet)
                GameController.dices[diceNo] = 0;

            SoundManager.GetSoundEffect(1, 0.2f);
        }

        public void PlaceJail()
        {
            if (slot == null)
            {
                Debug.LogError("Slot is null when trying to place pawn in jail.");
                return;
            }

            Pawn pawn = slot.GetTopPawn(true);  // Safely pop from slot
            if (pawn == null)
            {
                Debug.LogError("No pawn found in slot when trying to place in jail.");
                return;
            }

            pawn.imprisoned = true;
            int jailSlot = pawn.pawnColor == 0 ? 0 : 25;

            Slot.slots[jailSlot].PlacePawn(pawn, pawn.pawnColor);  // Place into jail slot
            Pawn.imprisonedSide[pawn.pawnColor]++;
            Pawn.shelterSide[pawn.pawnColor] = false;

            SoundManager.GetSoundEffect(2, 0.8f);
        }


        private bool CanPlaceShelter()
        {
            int value = pawnColor == 0 ? 25 : 0;

            if (slotNo == endSlotNo)
            {
                if (CanPlaceShelter(0, value, true) || CanPlaceShelter(1, value, true))
                    return true;
            }
            else if (CanPlaceShelter(0, value, false) || CanPlaceShelter(1, value, false))
                return true;

            return false;
        }

        private bool CanPlaceShelter(int ind, int value, bool lastOrNearer)
        {
            int sign = pawnColor == 0 ? -1 : 1;
            int val = value + sign * slotNo;
            int diceVal = GameController.dices[ind];
            bool result = lastOrNearer ? diceVal >= val : diceVal == val;

            if (result)
            {
                GameController.dices[ind] = GameController.isDublet ? GameController.dices[ind] : 0;
                PlaceInShelter();
            }

            return result;
        }
        public void PlaceInShelter()
        {
            Debug.Log($"[PlaceInShelter] Attempting to bear off pawn color {pawnColor} from slot {slotNo}");

            if (go == null)
            {
                go = GameObject.Find((pawnColor == 0 ? "White" : "Red") + " House");
                Debug.Log($"[PlaceInShelter] go initialized: {(go != null ? "success" : "FAIL")}");
            }

            if (go == null)
            {
                Debug.LogError("[PlaceInShelter] House GameObject not found!");
                return;
            }

            int visibleCount = 0;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (go.transform.GetChild(i).gameObject.activeSelf)
                    visibleCount++;
            }

            Debug.Log($"[PlaceInShelter] visibleCount before = {visibleCount}");

            if (visibleCount < go.transform.childCount)
            {
                go.transform.GetChild(visibleCount).gameObject.SetActive(true);
                Debug.Log($"[PlaceInShelter] Activated pawn {visibleCount} in the house UI");
            }

            SoundManager.GetSoundEffect(0, 0.3f);

            if (visibleCount + 1 == 15)
            {
                Debug.Log("[PlaceInShelter] Win condition reached! Triggering Game Over.");
                OnGameOver(pawnColor == 0);
                Board.GameOver = true;
            }

            Slot.slots[slotNo].GetTopPawn(true);
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }






        public void PlaceInShelterBot()
        {
            GameObject go = GameObject.Find((pawnColor == 0 ? "White" : "Red") + " House");
            int rescuedPawns = go.GetComponentsInChildren<SpriteRenderer>().Length - 1;

            go.transform.GetChild(rescuedPawns++).gameObject.SetActive(true);
            SoundManager.GetSoundEffect(0, 0.3f);

            if (rescuedPawns == 15)
            {
                OnGameOver(pawnColor == 0);
                Board.GameOver = true;
            }

            Slot.slots[slotNo].GetTopPawn(true);  // Remove from current slot
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }



        private bool CheckShelterStage()
        {
            maxMoves = GameController.isDublet ? 4 : 2;

            go = GameObject.Find((pawnColor == 0 ? "White" : "Red") + " House");
            rescuedPawns = go.GetComponentsInChildren<SpriteRenderer>().Length - 1;

            int count = 0;
            int offset = pawnColor == 0 ? 18 : 0;
            int b = pawnColor == 0 ? -1 : 1;

            for (int i = 1 + offset; i <= 6 + offset; i++)
            {
                int index = 7 * pawnColor - b * i;
                if (Slot.slots[index].Height() > 0 && Slot.slots[index].IsWhite() == pawnColor)
                {
                    if (count == 0) endSlotNo = index;
                    count += Slot.slots[index].Height();
                }
            }

            return (count == 15 - rescuedPawns);
        }

        public static void InitializePawn()
        {
            Board.GameOver = false;
            moves = 0;
            imprisonedSide = new int[2];
            shelterSide = new bool[2];
        }
        public static void TriggerGameOver(bool isWhite)
        {
            OnGameOver(isWhite);
        }

    }
}
