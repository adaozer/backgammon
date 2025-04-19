using UnityEngine;
using System.Collections.Generic;

namespace Backgammon.Core
{
    // Create slots and assign them the initial order of pieces.

    public class Board : MonoBehaviour
    {
        private const float UP_POS = 5.43f;

        [SerializeField] private Slot slotPrefab;
        [SerializeField] private Pawn pawnPrefab;
        [SerializeField] private Transform slotsContainer;

        [HideInInspector] public bool isClientWhite;        // Is the client white or red?

        public static Board Instance { get; set; }
        public static bool GameOver { get; set; }

        private void Awake()
        {
            Instance = this;
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            CreateSlots();
            CreatePawns();
        }

        private void CreateSlots()
        {
            Slot.slots = new List<Slot>();
            Vector3 slotPos = new Vector3(0, UP_POS, -0.2f);
            Quaternion slotRot = Quaternion.identity;
            CreateSlot(0, Color.clear, slotPos, slotRot);              // prison slot for white

            for (int i = 1; i <= 24; i++)
            {
                float xDelta = (i < 13) ? -1.125f : 1.125f;             // increments on the x-axis of slot positions
                float xOffset = (((i - 1) / 6) % 3 == 0) ? 0 : -1.25f;  // jumping over the middle gang
                float iOffset = (i < 13) ? 1 : 24;
                float ySign = (i < 13) ? 1 : -1;

                Color color = (i % 2 == 0) ? new Color(0, 0.6f, 1) : new Color(0.5f, 0.7f, 0.8f);

                slotPos = new Vector3(6.81f + (i - iOffset) * xDelta + xOffset, ySign * UP_POS, -0.2f);
                slotRot = (i < 13) ? Quaternion.identity : Quaternion.Euler(new Vector3(0, 0, 180));

                CreateSlot(i, color, slotPos, slotRot);
            }

            slotPos = new Vector3(0, -UP_POS, -0.2f);
            slotRot = Quaternion.Euler(new Vector3(0, 0, 180));
            CreateSlot(25, Color.clear, slotPos, slotRot);             // prison slot for reds
        }

        private void CreateSlot(int slotNo, Color color, Vector3 slotPos, Quaternion slotRot)
        {
            if (slotPrefab == null)
            {
                Debug.LogError("[CreateSlot] slotPrefab is null! Assign it in the Inspector.");
                return;
            }

            Slot slot = Instantiate(slotPrefab, slotPos, slotRot, slotsContainer);
            if (slot == null)
            {
                Debug.LogError("[CreateSlot] Instantiation failed — result was null.");
                return;
            }

            slot.name = "slot" + slotNo;
            slot.slotNo = slotNo;
            slot.spriteRenderer.color = color;

            Slot.slots.Add(slot);
        }


        private void CreatePawns()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i < 2) CreatePawn(1, 0);   // slot  1
                if (i < 2) CreatePawn(24, 1);   // slot 24            

                if (i < 3) CreatePawn(8, 1);   // slot  8
                if (i < 3) CreatePawn(17, 0);   // slot 17

                CreatePawn(6, 1);               // slot  6
                CreatePawn(12, 0);              // slot 12
                CreatePawn(13, 1);              // slot 13
                CreatePawn(19, 0);              // slot 19
            }

            /*
            for (int i = 0; i < 5; i++)
            {
                if (i < 3) CreatePawn(4, 1);   // slot 21
                if (i < 4) CreatePawn(3, 1);   // slot 22
                if (i < 3) CreatePawn(2, 1);   // slot 23            
                if (i < 1) CreatePawn(8, 1);	// slot 15
                if (i < 4) CreatePawn(5, 1);   // slot 19

                if (i < 2) CreatePawn(1, 0);   // slot 24
                if (i < 1) CreatePawn(23, 0);	// slot  2
                if (i < 1) CreatePawn(21, 0);	// slot  4
                if (i < 1) CreatePawn(12, 0);	// slot 18
                if (i < 3) CreatePawn(18, 0);    // slot  6
                if (i < 3) CreatePawn(23, 0);	// slot  1
                if (i < 4) CreatePawn(21, 0);    // slot  3            
            }
            */
            /*
            for (int i = 0; i < 5; i++)                         // Bug 2
            {
                if (i < 1) CreatePawn(7, 1);	// slot  7
                CreatePawn(6, 1);			    // slot  6      // red
                if (i < 2) CreatePawn(8, 1);	// slot  8
                CreatePawn(13, 1);	            // slot 13
                if (i < 2) CreatePawn(15, 1);   // slot 15

                if (i < 1) CreatePawn(2, 0);	// slot  2
                if (i < 2) CreatePawn(14, 0);	// slot 14
                if (i < 4) CreatePawn(16, 0);   // slot 16      // white
                if (i < 3) CreatePawn(17, 0);	// slot 17
                CreatePawn(19, 0);			    // slot 19
            }*//*
            for (int i = 0; i < 5; i++)
            {
                if (i < 2) CreatePawn(1, 1);   // slot  1
                if (i < 2) CreatePawn(24, 0);   // slot 24

                if (i < 3) CreatePawn(23, 0);   // slot  8
                if (i < 3) CreatePawn(2, 1);   // slot 17

                CreatePawn(22, 0);               // slot  6
                CreatePawn(3, 1);              // slot 12
                CreatePawn(21, 0);              // slot 13
                CreatePawn(4, 1);              // slot 19
            }*/
        }

        private void CreatePawn(int slotNo, int isWhite)        // assign a pawn to the appropriate slot
        {
            Pawn pawn = Instantiate(pawnPrefab);
            Slot.slots[slotNo].PlacePawn(pawn, isWhite);
        }
    }
}