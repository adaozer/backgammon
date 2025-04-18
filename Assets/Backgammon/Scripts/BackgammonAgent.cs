using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Backgammon.Core;

public class BackgammonAgent : Agent
{
    public int playerIndex; // 0 = white, 1 = red
    private int retryCount = 0;
    private const int maxRetries = 3;
    public int remainingMoves = 0;

    private int lastFromSlot = -1;
    private int lastDiceIndex = -1;

    public override void CollectObservations(VectorSensor sensor)
    {
        for (int i = 1; i <= 24; i++)
        {
            var slot = Slot.slots[i];
            float height = slot.Height() / 5f;
            float color = slot.Height() > 0 ? (slot.IsWhite() == 0 ? 1f : 0f) : -1f;
            sensor.AddObservation(height);
            sensor.AddObservation(color);
        }

        sensor.AddObservation(GameController.dices[0] / 6f);
        sensor.AddObservation(GameController.dices[1] / 6f);
        sensor.AddObservation(GameController.turn == playerIndex ? 1f : 0f);
        sensor.AddObservation(Pawn.shelterSide[playerIndex] ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int fromSlot = actions.DiscreteActions[0];
        int diceIndex = actions.DiscreteActions[1];

        if (GameController.turn != playerIndex || Board.GameOver)
            return;

        if (fromSlot == lastFromSlot && diceIndex == lastDiceIndex)
        {
            GameController.Instance.OnAgentMoveComplete();
            return;
        }

        lastFromSlot = fromSlot;
        lastDiceIndex = diceIndex;

        if (fromSlot < 0 || fromSlot > 25 || diceIndex > 1)
        {
            RetryOrEnd();
            return;
        }

        var pawn = Slot.slots[fromSlot].GetTopPawn(false);
        if (pawn == null || pawn.pawnColor != playerIndex)
        {
            RetryOrEnd();
            return;
        }

        int dir = playerIndex == 0 ? 1 : -1;
        int diceValue = GameController.dices[diceIndex];
        int target = pawn.slotNo + dir * diceValue;
        bool inRange = target >= 1 && target <= 24;
        bool bearingOff = false;

        if (Pawn.shelterSide[playerIndex])
        {
            int distanceFromGoal = (playerIndex == 0) ? 25 - pawn.slotNo : pawn.slotNo;

            if (diceValue == distanceFromGoal)
            {
                target = (playerIndex == 0) ? 0 : 25;
                bearingOff = true;
            }
            else if (diceValue > distanceFromGoal)
            {
                bool checkerBehind = false;
                if (playerIndex == 0)
                {
                    for (int j = pawn.slotNo + 1; j <= 24; j++)
                        if (Slot.slots[j].Height() > 0 && Slot.slots[j].IsWhite() == playerIndex)
                        {
                            checkerBehind = true;
                            break;
                        }
                }
                else
                {
                    for (int j = pawn.slotNo - 1; j >= 1; j--)
                        if (Slot.slots[j].Height() > 0 && Slot.slots[j].IsWhite() == playerIndex)
                        {
                            checkerBehind = true;
                            break;
                        }
                }

                if (!checkerBehind)
                {
                    target = (playerIndex == 0) ? 0 : 25;
                    bearingOff = true;
                }
            }
        }

        if (target == fromSlot)
        {
            RetryOrEnd();
            return;
        }

        if (inRange)
        {
            var targetSlot = Slot.slots[target];
            if (targetSlot.Height() > 1 && targetSlot.IsWhite() != playerIndex)
            {
                RetryOrEnd();
                return;
            }
        }

        if (inRange || bearingOff)
        {
            bool success = GameController.Instance.BotTryMove(pawn, target, diceIndex);
            if (success)
            {
                AddReward(bearingOff ? 0.5f : 0.05f);
                retryCount = 0;
                lastFromSlot = -1;
                lastDiceIndex = -1;
                remainingMoves--;

                if (remainingMoves > 0 && (GameController.dices[0] > 0 || GameController.dices[1] > 0))
                    RequestDecision();
                else
                    GameController.Instance.OnAgentMoveComplete();

                return;
            }
            else
            {
                RetryOrEnd();
            }
        }
        else
        {
            RetryOrEnd();
        }
    }

    private void RetryOrEnd()
    {
        retryCount++;
        AddReward(-0.05f);

        if (retryCount >= maxRetries)
        {
            retryCount = 0;
            lastFromSlot = -1;
            lastDiceIndex = -1;
            GameController.Instance.OnAgentMoveComplete();
        }
        else
        {
            RequestDecision();
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        bool hasJail = Pawn.imprisonedSide[playerIndex] > 0;
        int jailSlot = playerIndex == 0 ? 0 : 25;

        for (int i = 0; i <= 25; i++)
        {
            bool enable = false;
            if (hasJail)
            {
                enable = (i == jailSlot);
            }
            else if (i >= 1 && i <= 24)
            {
                var slot = Slot.slots[i];
                enable = (slot.Height() > 0 && slot.IsWhite() == playerIndex);
            }

            if (!enable)
                actionMask.SetActionEnabled(0, i, false);
        }

        for (int i = 0; i < 2; i++)
        {
            if (GameController.dices[i] == 0)
                actionMask.SetActionEnabled(1, i, false);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 1;
        d[1] = 0;
    }
}
