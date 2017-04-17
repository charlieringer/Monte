using System;
using System.Collections.Generic;

namespace Monte
{
    public class MCTSSimpleAgent : MCTSMasterAgent
    {
        public MCTSSimpleAgent(string file):base(file){}
        public MCTSSimpleAgent (int _numbSimulations, double _exploreWeight, int _maxRollout, double _drawScore) : base(_numbSimulations, _exploreWeight, _maxRollout, _drawScore){}

        //Main MCTS algortim
        protected override void mainAlgorithm(AIState initialState)
        {
            //Make the intial children
            initialState.generateChildren ();
             foreach (var child in initialState.children)
             {
                 if (child.getWinner() == child.playerIndex)
                 {
                     next = child;
                     done = true;
                     return;
                 }
             }
            //if no childern are generated
            if (initialState.children.Count == 0)
            {
                //Report this error and return.
                Console.WriteLine("Error: State supplied has no children.");
                next = null;
                done = true;
                return;
            }

            int count = 0;
            while(count < numbSimulations)
            {
                count++;
                //Once done set the best child to this
                AIState bestNode = initialState;
                //And loop through it's child
                while(bestNode.children.Count > 0)
                {
                    //Set the scores as a base line
                    double bestScore = -1;
                    int bestIndex = -1;

                    for(int i = 0; i < bestNode.children.Count; i++){
                        //Scores as per the previous part
                        double wins = bestNode.children[i].wins;
                        double games = bestNode.children[i].totGames;
                        double score = (games > 0) ? wins / games : 1.0;

                        //UBT (Upper Confidence Bound 1 applied to trees) function for determining
                        //How much we want to explore vs exploit.
                        //Because we want to change things the constant is configurable.
                        double exploreRating = exploreWeight*Math.Sqrt((2* Math.Log(initialState.totGames + 1) / (games + 0.1)));

                        double totalScore = score+exploreRating;
                        //Again if the score is better updae
                        if (totalScore > bestScore){
                            bestScore = totalScore;
                            bestIndex = i;
                        }
                    }
                    //And set the best child for the next iteration
                    bestNode = bestNode.children[bestIndex];
                }
                //Then roll out that child.
                rollout(bestNode);
            }

            //Once we get to this point we have worked out the best move so just need to return it
            int mostGames = -1;
            int bestMove = -1;
            //Loop through all childern
            for(int i = 0; i < initialState.children.Count; i++)
            {
                //find the one that was played the most (this is the best move)
                int games = initialState.children[i].totGames;
                if(games >= mostGames)
                {
                    mostGames = games;
                    bestMove = i;
                }
            }
            next = initialState.children[bestMove];
            done = true;
        }

        //Rollout function (plays random moves till it hits a termination)
        protected override void rollout(AIState rolloutStart)
        {
            int rolloutStartResult = rolloutStart.getWinner();
            if (rolloutStartResult >= 0)
            {
                if(rolloutStartResult == rolloutStart.playerIndex) rolloutStart.addWin();
                else if(rolloutStartResult == (rolloutStart.playerIndex+1)%2) rolloutStart.addLoss();
                else rolloutStart.addDraw (drawScore);
            }
            bool terminalStateFound = false;
            //Get the children
            List<AIState> children = rolloutStart.generateChildren();

            int loopCount = 0;
            while(!terminalStateFound)
            {
                //Loop through till a terminal state is found
                loopCount++;
                //If max roll out is hit or no childern were generated
                if (loopCount >= maxRollout || children.Count == 0) {
                    //Record a draw
                    rolloutStart.addDraw (drawScore);
                    return;
                }
                //Get a random child index
                int index = randGen.Next(children.Count);
                //and see if that node is terminal
                int endResult = children[index].getWinner ();
                if(endResult >= 0)
                {
                    terminalStateFound = true;
                    //If it is a win add a win0
                    if(endResult == rolloutStart.playerIndex) rolloutStart.addWin();
                    //Else add a loss
                    else rolloutStart.addLoss();
                } else {
                    //Otherwise select that nodes as the childern and continue
                    children = children [index].generateChildren();
                }
            }
            //Reset the children as these are not 'real' children but just ones for the roll out.
            foreach( AIState child in rolloutStart.children)
            {
                child.children = new List<AIState>();
            }
        }
    }
}