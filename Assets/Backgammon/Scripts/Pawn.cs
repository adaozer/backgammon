using UnityEngine;
using System;
using Broniek.Stuff.Sounds;

namespace Backgammon.Core
{
    // Implementation of dragging the pieces.

    public class Pawn : MonoBehaviour
    {
        public static event Action<int> OnCompleteTurn = delegate { };
        public static event Action<bool> OnGameOver = delegate { };

        public static int[] imprisonedSide = new int[2];        // Is the mode of taking the pieces of given side out of prison?
        public static bool[] shelterSide = new bool[2];         // Is the mode of introducing the pieces of given side into the shelter?

        public static int endSlotNo;                            // sloty ostatniej ćwiartki najdalsze od schronienia
        private static int moves;

        public int pawnColor;                                   // The color of this pawn.
        public int slotNo;                                      // slot to which this pawn is currently assigned
        public int pawnNo;                                      // the position taken by a pawn in a slot

        private Slot slot;                                      // the slot over which the piece being drawn is currently located
        private Vector3 startPos;
        private GameObject go;
        private bool isDown;                                    // Is the mouse button pressed?
        private bool imprisoned;                                // a given pawn is in prison
        private bool shelter;                                   // whether the piece is above the shelter area
        private int rescuedPawns;                               // the number of pieces of a given color in the shelter
        private int maxMoves;

        public void SetColor(int color)
        {
            GetComponent<SpriteRenderer>().color = color == 0 ? Color.white : Color.red;
            pawnColor = color;
        }

        //-------- events that carry out dragging a piece

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Slot"))
                slot = other.GetComponent<Slot>();
            else if (other.CompareTag("Shelter"))
                if (shelterSide[0] || shelterSide[1])
                    shelter = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Slot"))
                slot = Slot.slots[slotNo];         // when we are not in the area of ​​any of the slots (in the context of OnTriggerStay2D)
            else if (other.CompareTag("Shelter"))
                shelter = false;
        }

        private void OnMouseDown()
        {
            if (!Board.GameOver)
            {
                if (!imprisoned && ((imprisonedSide[0] > 0 && pawnColor == 0) || (imprisonedSide[1] > 0 && pawnColor == 1)))
                    return;             // in a situation of imprisonment, do not allow unrestricted pieces to be dragged

                TrySelectPawn();
            }
        }

        private void TrySelectPawn()
        {
            if (GameController.dragEnable && GameController.turn == pawnColor)
                if (Slot.slots[slotNo].Height() - 1 == pawnNo)  // only the highest pawn in the slot can be moved
                {
                    startPos = transform.position;
                    isDown = true;
                    TryHighlight(true);     // we turn on the highlighting of the appropriate slots
                }
        }

        private void OnMouseDrag()
        {
            if (isDown)                     // you need to convert the cursor positions to world positions
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
                TryHighlight(false);        // we turn off the highlighting of the appropriate slots
                isDown = false;

                if (IsPatology())
                    return;                 // impossible moves (against the rules of the game)
                                            //------------ a mechanism that guarantees the correct movement of the pieces
                CheckShelterStage();

                if (TryPlace())            // prison mode support
                    CheckShelterAndMore();

                CheckIfNextTurn();
            }
        }

        private void CheckIfNextTurn()
        {
            if (moves == maxMoves && !Board.GameOver)           // all moves have been made
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
            if (slotNo != 0) TryRemovePawnFromJail();
            if (slotNo != 25) TryRemovePawnFromJail();

            if (CheckShelterStage())                //---- detection of the mode of introducing the pieces into the shelter
                shelterSide[pawnColor] = true;

            if (++moves < maxMoves)      // after each move
            {
                if (!GameController.CanMove(1))        // when a move cannot be made
                {
                    moves = 0;
                    OnCompleteTurn(pawnColor);
                }
            }
        }

        private bool IsPatology()
        {
            if (slot.slotNo == 0 || slot.slotNo == 25)                  // prison slots
            {
                transform.position = startPos;
                return true;
            }

            if (slot.Height() > 1 && slot.IsWhite() != pawnColor)     // there is more than one opponent's piece in a slot
            {
                transform.position = startPos;
                return true;
            }

            return false;
        }

        private bool TryPlace()
        {
            if (shelter)                                // only when the piece being drawn is above the shelter area
            {
                if (shelterSide[pawnColor])  // we additionally take into account dragging a piece to the field symbolizing their home
                    if (CanPlaceShelter())
                        return true;

                transform.position = startPos;
                return false;
            }
            else
            {
                if (slot.slotNo == slotNo)              // same slot
                {
                    transform.position = startPos;
                    return false;
                }

                int sign = pawnColor == 0 ? 1 : -1;

                if (slot.slotNo == slotNo + sign * GameController.dices[0])   // that it matches the values ​​on the dice
                    DoCorrectMove(0);
                else if (slot.slotNo == slotNo + sign * GameController.dices[1])
                    DoCorrectMove(1);
                else                                    // does not agree with the values ​​thrown out
                {
                    transform.position = startPos;
                    return false;
                }

                return true;
            }
        }

        private void TryHighlight(bool state)           // highlighting the appropriate slots
        {
            int sign = pawnColor == 0 ? 1 : -1;

            int slot0 = slotNo + sign * GameController.dices[0];
            int slot1 = slotNo + sign * GameController.dices[1];

            if (slot0 > 0 && slot0 < 25 && slot0 != slotNo)
            {
                if (!(Slot.slots[slot0].Height() > 1 && Slot.slots[slot0].IsWhite() != pawnColor))    // there is no more than one piece of a different color in a slot
                {
                    Slot.slots[slot0].HightlightMe(state);
                }
            }

            if (slot1 > 0 && slot1 < 25 && slot1 != slotNo)
            {
                if (!(Slot.slots[slot1].Height() > 1 && Slot.slots[slot1].IsWhite() != pawnColor))    // there is no more than one piece of a different color in a slot
                {
                    Slot.slots[slot1].HightlightMe(state);
                }
            }
        }

        private void DoCorrectMove(int diceNo)
        {
            if (slot.Height() == 1 && slot.IsWhite() != pawnColor)   // a slot with one opponent's piece
                PlaceJail();

            Slot.slots[slotNo].GetTopPawn(true);                      // we remove the piece from the slot that has been occupied so far
            slot.PlacePawn(this, pawnColor);                          // put a piece in the new slot

            if (!GameController.isDublet)
                GameController.dices[diceNo] = 0;

            SoundManager.GetSoundEffect(1, 0.2f);
        }

        private void PlaceJail()                   // placing a whipped piece in prison (suspension of introduction to the shelter)
        {
            Pawn pawn = slot.GetTopPawn(true);                              // get a whipped piece
            pawn.imprisoned = true;

            Slot.slots[pawn.pawnColor == 0 ? 0 : 25].PlacePawn(pawn, pawn.pawnColor); // put the piece in the prison slot
            imprisonedSide[pawn.pawnColor]++;
            shelterSide[pawn.pawnColor] = false;                            // a piece in prison, therefore no entry into the shelter

            SoundManager.GetSoundEffect(2, 0.8f);
        }

        //-------- private methods related to shelter mode support

        private bool CanPlaceShelter()
        {
            int value = pawnColor == 0 ? 25 : 0;

            if (slotNo == endSlotNo)                    // the white or red pawn from the farthest slot
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

        private void PlaceInShelter()
        {
            go.transform.GetChild(rescuedPawns++).gameObject.SetActive(true);
            SoundManager.GetSoundEffect(0, 0.3f);

            if (rescuedPawns == 15)
            {
                OnGameOver(pawnColor == 0);
                Board.GameOver = true;
            }

            Slot.slots[slotNo].GetTopPawn(true);            // remove from current slot
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }

        private bool CheckShelterStage()                   // check if it is possible to bring a given player's pieces into the shelter
        {
            maxMoves = GameController.isDublet ? 4 : 2;    // four the same movements or two different movements

            go = GameObject.Find((pawnColor == 0 ? "White" : "Red") + " House");
            rescuedPawns = go.GetComponentsInChildren<SpriteRenderer>().Length - 1;

            int count = 0;
            int offset = pawnColor == 0 ? 18 : 0;
            int b = pawnColor == 0 ? -1 : 1;

            for (int i = 1 + offset; i <= 6 + offset; i++)
                if (Slot.slots[7 * pawnColor - b * i].Height() > 0 && Slot.slots[7 * pawnColor - b * i].IsWhite() == pawnColor)
                {
                    if (count == 0)
                        endSlotNo = 7 * pawnColor - b * i;

                    count += Slot.slots[7 * pawnColor - b * i].Height();
                }

            return (count == 15 - rescuedPawns);   // if all the pieces of a given color, remaining on the board, are in the last quadrant
        }

        public static void InitializePawn()
        {
            Board.GameOver = false;
            moves = 0;
            imprisonedSide = new int[2];
            shelterSide = new bool[2];
        }
        public static int CountRemainingCheckers(int playerColor)
        {
            int count = 0;

            // Count all pawns on the board (Slots 1 to 24)
            for (int i = 1; i <= 24; i++)
            {
                if (Slot.slots[i].Height() > 0 && Slot.slots[i].IsWhite() == playerColor)
                {
                    count += Slot.slots[i].Height();
                }
            }

            // Add imprisoned checkers (Jail slots: 0 for White, 25 for Red)
            count += imprisonedSide[playerColor];

            return count;
        }

    }
}