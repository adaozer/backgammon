using UnityEngine;
using UnityEngine.UI;
using System;

namespace Backgammon.Core
{
    public class TimeController : MonoBehaviour
    {
        public static event Action<bool> OnTimeLimitEnd = delegate { };

        [SerializeField] private Text timeDisplay;
        [SerializeField] private Button[] submitBtns;
        [SerializeField] private GameObject acceptPanel;

        public static TimeController Instance { get; set; }
        public float[] timeLapse = new float[2];
        [HideInInspector] public int acceptance;

        private float timeRange = 600;
        private float timeInterval = 1f;
        private float timeElapsed;

        private void Awake()
        {
            Instance = this;
            timeElapsed = timeInterval;

            if (submitBtns != null && submitBtns.Length >= 2)
            {
                submitBtns[0].onClick.AddListener(delegate { Accept(0); });
                submitBtns[1].onClick.AddListener(delegate { Accept(1); });
            }
            else
            {
                acceptance = 2; // fallback for full bot mode
            }
        }


        private void Start()
        {
            if (GameController.Instance.playerTypes[0] != PlayerType.Human &&
                GameController.Instance.playerTypes[1] != PlayerType.Human)
            {
                acceptance = 2; // auto-accept if both bots
                if (acceptPanel != null) acceptPanel.SetActive(false);
            }
        }


        private void Accept(int no)
        {
            acceptance++;
            submitBtns[no].gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!Board.GameOver && acceptance >= 2)
            {
                timeLapse[GameController.turn] += Time.deltaTime;

                if ((timeElapsed += Time.deltaTime) >= timeInterval)
                {
                    timeElapsed = 0;
                    UpdateDisplay(timeLapse[GameController.turn]);
                }
            }
        }

        private void UpdateDisplay(float time)
        {
            if (timeDisplay.text == "0:00")
            {
                OnTimeLimitEnd(GameController.turn != 0);
                Destroy(this);
                return;
            }

            time = timeRange + 1 - time;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timeDisplay.text = minutes + ":" + seconds.ToString("00");
        }
    }
}
