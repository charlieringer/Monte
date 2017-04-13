using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Monte
{
    public class MCTSWithSoftPruning : MCTSMasterAgent
    {
        private readonly Model model;
        private double softPruneWeight;

        public MCTSWithSoftPruning (int _numbSimulations, double _exploreWeight, int _maxRollout, Model _model, double _softPruneWeight, double _drawScore)
            : base( _numbSimulations, _exploreWeight, _maxRollout, _drawScore)
        {
            model = _model;
            softPruneWeight = _softPruneWeight;
        }

        public MCTSWithSoftPruning (Model _model)
        {
            model = _model;
            parseXML("Assets/Monte/DefaultSettings.xml");
        }

        public MCTSWithSoftPruning (Model _model, String settingsFile) : base (settingsFile)
        {
            model = _model;
            parseXML(settingsFile);
        }

        void parseXML(String settingsFile)
        {

            try
            {
                XmlDocument settings = new XmlDocument();
                settings.Load(settingsFile);
                XmlNode node = settings.SelectSingleNode("descendant::PruningSettings");
                softPruneWeight = Double.Parse(node.Attributes.GetNamedItem("SoftPruneWeight").Value);
            }
            catch
            {
                softPruneWeight = 0.1;
                Console.WriteLine("Error reading settings file when constructing MCTSWithSoftPruning. Default settings values used (SoftPruneWeight = 0.1).");
                Console.WriteLine("File:" + settingsFile);
            }

        }

        //Main MCTS algortim
        protected override void mainAlgorithm(AIState initialState)
        {
            //Make the intial children
            initialState.generateChildren ();
            foreach (var child in initialState.children)
            {
                if (child.getWinner() == (initialState.playerIndex+1)%2)
                {
                    next = child;
                    done = true;
                    return;

                }
            }
            int count = 0;
            while(count < numbSimulations){
                //Once done set the best child to this
                AIState bestNode = initialState;
                //And loop through it's child
                count++;
                while(bestNode.children.Count > 0)
                {
                    double bestScore = -1;
                    int bestIndex = -1;

                    for(int i = 0; i < bestNode.children.Count; i++)
                    {
                        if(bestNode.children[i].stateScore == null) bestNode.children[i].stateScore = model.evaluate(bestNode.children[i]);
                        //Scores as per the previous part
                        double wins = bestNode.children[i].wins;
                        double games = bestNode.children[i].totGames;

                        double score = 1.0;
                        if (games > 0) {
                            score = wins / games;
                        }

                        //UBT (Upper Confidence Bound 1 applied to trees) function for determining
                        //How much we want to explore vs exploit.
                        //Because we want to change things the constant is configurable.
                        double exploreScore = exploreWeight * Math.Sqrt((2* Math.Log(initialState.totGames + 1) / (games + 0.1)));
                        //soft pruning
                        //double stateScoreValue = softPruneWeight * (bestNode.children[i].stateScore.Value);
                        double totalScore = score + exploreScore;//+stateScoreValue ;
                        //Again if the score is better updae
                        if (!(totalScore > bestScore)) continue;
                        bestScore = totalScore;
                        bestIndex = i;
                    }
                    //And set the best child for the next iteration
                    bestNode = bestNode.children[bestIndex];
                }
                //Then roll out that child.
                rollout(bestNode);
            }

            //Once we get to this point we have worked out the best move
            //So just need to return it
            double mostGames = -1;
            int bestMove = -1;
            //Loop through all childern
            for(int i = 0; i < initialState.children.Count; i++)
            {
                //find the one that was played the most (this is the best move)
                int games = initialState.children[i].totGames;
                //double games = initialState.children[i].wins/initialState.children[i].totGames;
                if(games >= mostGames)
                {
                    mostGames = games;
                    bestMove = i;
                }
            }
            //Return it.
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



