using System;
using System.Threading;
using System.Collections.Generic;

namespace Monte
{
    public class RandomAgent : AIAgent
    {
        protected override void mainAlgorithm(AIState initialState)
        {
            List<AIState> children = initialState.generateChildren();
            for (int i = 0; i < children.Count; i++)
            {
                int childWinner = children[i].getWinner();
                int otherPlayer = (initialState.playerIndex + 1) % 2;
                if (childWinner == otherPlayer)
                {
                    next = children[i];
                    done = true;
                    return;
                }
            }

            int index = randGen.Next(children.Count);
            next = children.Count > 0 ? children[index] : null;
            done = true;
        }
    }
}