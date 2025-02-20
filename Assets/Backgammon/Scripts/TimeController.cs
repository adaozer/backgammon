using UnityEngine;
using UnityEngine.UI;
using System;

namespace Backgammon.Core
{
    public class TimeController : MonoBehaviour
    {
        public static event Action<bool> OnTimeLimitEnd = delegate { };

        [SerializeField] private Text timeDisplay;

        private float timeRange = 600;              // each player's playing time in seconds
        private float timeInterval = 1f;            // how often the clock is updated
        private float timeElapsed;                  // elapsed time since the last update
        public float[] timeLapse = new float[2];

        public static TimeController Instance { get; set; }

        [SerializeField] private Button[] submitBtns;
        [HideInInspector] public int acceptance;   // both players confirm they want to start the game

        private void Awake()
        {
            Instance = this;
            timeElapsed = timeInterval;
            acceptance = 2;   // automatically accept so the game starts immediately

            // Optionally disable the acceptance buttons
            foreach (var btn in submitBtns)
            {
                btn.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!Board.GameOver && acceptance >= 2)
            {
                float time;
                timeLapse[GameController.turn] += Time.deltaTime;
                time = timeLapse[GameController.turn];

                if ((timeElapsed += Time.deltaTime) >= timeInterval)
                {
                    timeElapsed = 0;
                    UpdateDisplay(time);
                }
            }
        }

        private void UpdateDisplay(float time)
        {
            if (timeDisplay.text == "0:00")             // player has timed out
            {
                OnTimeLimitEnd(GameController.turn != 0);
                Destroy(this);
                return;
            }

            time = timeRange + 1 - time;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timeDisplay.text = minutes.ToString() + ":" + seconds.ToString("00");
        }
    }
}
