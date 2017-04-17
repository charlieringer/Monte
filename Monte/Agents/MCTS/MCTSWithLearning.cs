using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Monte
{
	public class MCTSWithLearning : MCTSMasterAgent
	{
		private readonly Model model;
	    private double epsilon;


	    public MCTSWithLearning (int _numbSimulations, double _exploreWeight, int _maxRollout, Model _model, double _epsilon, double _drawScore)
	        : base( _numbSimulations, _exploreWeight, _maxRollout, _drawScore)
	    {
	        model = _model;
	        epsilon = _epsilon;
	    }

	    public MCTSWithLearning (Model _model)
	    {
	        model = _model;
	        parseXML("Assets/Monte/DefaultSettings.xml");
	    }

	    public MCTSWithLearning (Model _model, String settingsFile) : base (settingsFile)
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
	            XmlNode node = settings.SelectSingleNode("descendant::RolloutSettins");
	            epsilon = Double.Parse(node.Attributes.GetNamedItem("Epsilon").Value);
	        }
	        catch
	        {
	            epsilon = 0.2;
	            Console.WriteLine("Error reading settings file when constructing MCTSWithLearning. Default settings values used (Epsilon = 0.2).");
	            Console.WriteLine("File:" + settingsFile);
	        }

	    }

		//Main MCTS algortim
	    protected override void mainAlgorithm(AIState initialState)
	    {
	        //Make the intial children
	        initialState.generateChildren ();
	        initialState.treeNode = true;
	        foreach (var child in initialState.children)
	        {
	            child.treeNode = true;
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
	            Console.WriteLine("Error: State supplied has no childern.");
	            next = null;
	            done = true;
	            return;
	        }

	        int count = 0;
	        //while (latestTick-startTime < thinkingTime)
	        while (count < numbSimulations)
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
	                    double score = (games > 0) ? score = wins / games : 1.0;

	                    //UBT (Upper Confidence Bound 1 applied to trees) function for determining
	                    //How much we want to explore vs exploit.
	                    //Because we want to change things the constant is configurable.
	                    double exploreRating = exploreWeight * Math.Sqrt((2* Math.Log(initialState.totGames + 1) / (games + 0.1)));

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
	        //Console.WriteLine("MCTS Learnt: Number of Simulations = " + count);
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

            int count = 0;
                while(!terminalStateFound)
            {
                //Loop through till a terminal state is found
                count++;
                //If max roll out is hit or no childern were generated
                if (count >= maxRollout || children.Count == 0) {
                    //record a draw
                    rolloutStart.addDraw (drawScore);
                    return;
                }

                int selectedChild = children.Count-1;

                //epsilon greedy move selection.
                if (randGen.NextDouble() < epsilon)
                {
                    //choose best move (which is pos 0 in the sorted list)
                    foreach(AIState child in children) if(child.stateScore == null)child.stateScore = model.evaluate(child);
                    children = AIState.mergeSort(children);
                }
                else
                {
                    selectedChild = randGen.Next(children.Count);
                }

                //and see if that node is terminal
                int endResult = children[selectedChild].getWinner ();
                if(endResult >= 0)
                {
                    terminalStateFound = true;
                    //If it is a win add a win
                    if(endResult == rolloutStart.playerIndex) rolloutStart.addWin();
                    //Else add a loss
                    else rolloutStart.addLoss();
                } else {
                    //Otherwise select that nodes as the childern and continue
                    children = children [selectedChild].generateChildren();
                    if (children.Count == 0) {
                        break;
                    }
                }
            }
            //This node is now expanded so set tree node to true
            foreach( AIState child in rolloutStart.children)
            {
                child.treeNode = true;
            }
        }
    }
}