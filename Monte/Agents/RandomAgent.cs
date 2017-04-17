using System.Collections.Generic;

namespace Monte
{
    //Agent which selects it's moves at random
    public class RandomAgent : AIAgent
    {
        protected override void mainAlgorithm(AIState initialState)
        {
            //Generate all possible moves
            List<AIState> children = initialState.generateChildren();
            //Select a random one
            int index = randGen.Next(children.Count);
            //And set next to it(unless no children were generated
            next = children.Count > 0 ? children[index] : null;
            done = true;
        }
    }
}