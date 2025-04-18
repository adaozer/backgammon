using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Backgammon.Core;

public class BackgammonAgent : Agent
{
    public int playerIndex; // 0 = white, 1 = red

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log($"[Agent {playerIndex}] CollectObservations called");

        for (int i = 1; i <= 24; i++)
        {
            var slot = Slot.slots[i];
            sensor.AddObservation(slot.Height());
            sensor.AddObservation(slot.Height() > 0 ? slot.IsWhite() : -1);
        }

        sensor.AddObservation(GameController.dices[0]);
        sensor.AddObservation(GameController.dices[1]);
        sensor.AddObservation(GameController.turn == playerIndex ? 1f : 0f);
        sensor.AddObservation(Pawn.shelterSide[playerIndex] ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log($"[Agent {playerIndex}] Action received: fromSlot={actions.DiscreteActions[0]}, diceIndex={actions.DiscreteActions[1]}");

        int fromSlot = actions.DiscreteActions[0];
        int diceIndex = actions.DiscreteActions[1];

        if (GameController.turn != playerIndex || Board.GameOver) return;
        if (fromSlot < 1 || fromSlot > 24 || diceIndex > 1) return;

        var pawn = Slot.slots[fromSlot].GetTopPawn(false);
        if (pawn == null || pawn.pawnColor != playerIndex) return;

        int dir = (playerIndex == 0) ? 1 : -1;
        int diceValue = GameController.dices[diceIndex];
        int target = pawn.slotNo + dir * diceValue;

        if (target >= 1 && target <= 24)
        {
            bool success = GameController.Instance.BotTryMove(pawn, target, diceIndex);
            if (success)
            {
                AddReward(0.01f);
            }
        }

        // Let GameController continue the turn after this move
        GameController.Instance.OnAgentMoveComplete();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 1;
        d[1] = 0;
    }
}
