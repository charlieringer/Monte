using System;
using System.Collections.Generic;
using System.Xml;

namespace Monte
{
	public class MCTSWithLearning : MCTSMasterAgent
	{
	    //Model used during rollouts
		public readonly Model model;
	    //epsilon value used in rollout
	    private double epsilon;


	    //Constructors
	    public MCTSWithLearning (int _numbSimulations, double _exploreWeight, int _maxRollout, Model _model, double _epsilon, double _drawScore)
	        : base( _numbSimulations, _exploreWeight, _maxRollout, _drawScore)
	    {
	        model = _model == null ? new Model() : _model;
	        epsilon = _epsilon;
	    }

	    public MCTSWithLearning(Model _model)
	    {
	        model = _model == null ? new Model() : _model;
	        parseXML("Assets/Monte/DeafaultSettings.xml");
	    }

	    public MCTSWithLearning (Model _model, String settingsFile) : base (settingsFile)
	    {
	        model = (_model == null) ? new Model() : _model;
	        parseXML(settingsFile);
	    }

	    void parseXML(String settingsFile)
	    {
            //Try to parse the file
	        try
	        {
	            XmlDocument settings = new XmlDocument();
	            settings.Load(settingsFile);
	            XmlNode node = settings.SelectSingleNode("descendant::RolloutSettings");
	            epsilon = Double.Parse(node.Attributes.GetNamedItem("Epsilon").Value);
	        }
	        //It it fails
	        catch
	        {
	            //Use default values
	            epsilon = 0.2;
	            //And Error
	            Console.WriteLine("Monte: Error reading settings file when constructing MCTSWithLearning. Default settings values used (Epsilon = 0.2). File:" + settingsFile);
	        }
	    }

		//Main MCTS algortim
	    protected override void mainAlgorithm(AIState initialState)
	    {
	        //Make the intial children
	        initialState.generateChildren ();
	        //And set the root as a tree node
	        initialState.treeNode = true;
	        //Loop through all the children
	        foreach (var child in initialState.children)
	        {
	            //Set them as a tree node
	            child.treeNode = true;
	            //If any of them are winning moves
	            if (child.getWinner() == child.playerIndex)
	            {
	                //Just make that move and save on all of the comuptation
	                next = child;
	                done = true;
	                return;
	            }
	        }
	        //if no childern are generated
	        if (initialState.children.Count == 0)
	        {
	            //Report this error and return.
	            Console.WriteLine("Monte: Error: State supplied has no childern.");
	            next = null;
	            done = true;
	            return;
	        }
            //Start a count
	        int count = 0;
	        //Whilst time allows
	        while (count < numbSimulations)
	        {
	            //Increment the count
	            count++;
	            //Start at the inital state
	            AIState bestNode = initialState;
	            //And loop through it's child
	            while(bestNode.children.Count > 0)
	            {
	                //Set the scores as a base line
	                double bestScore = -1;
	                int bestIndex = -1;
                    //Loop thorugh all of the children
	                for(int i = 0; i < bestNode.children.Count; i++)
	                {
	                    //win score is basically just wins/games unless no games have been played, then it is 1
	                    double wins = bestNode.children[i].wins;
	                    double games = bestNode.children[i].totGames;
	                    double score = (games > 0) ? wins / games : 1.0;

	                    //UBT (Upper Confidence Bound 1 applied to trees) function balances explore vs exploit.
	                    //Because we want to change things the constant is configurable.
	                    double exploreRating = exploreWeight * Math.Sqrt((2* Math.Log(initialState.totGames + 1) / (games + 0.1)));
                        //Total score is win score + explore socre
	                    double totalScore = score+exploreRating;
	                    //If the score is better update
	                    if (!(totalScore > bestScore)) continue;
	                    bestScore = totalScore;
	                    bestIndex = i;
	                }
	                //Set the best child for the next iteration
	                bestNode = bestNode.children[bestIndex];
	            }
	            //Finally roll out this node.
	            rollout(bestNode);
	        }

	        //Onces all the simulations have taken place we select the best move...
	        int mostGames = -1;
	        int bestMove = -1;
	        //Loop through all childern
	        for(int i = 0; i < initialState.children.Count; i++)
	        {
	            //Find the one that was played the most (this is the best move as we are selecting the robust child)
	            int games = initialState.children[i].totGames;
	            if(games >= mostGames)
	            {
	                mostGames = games;
	                bestMove = i;
	            }
	        }
	        //Set that child to the next move
	        next = initialState.children[bestMove];
	        //And we are done
	        done = true;
	    }

        //Rollout function (plays random moves till it hits a termination)
        protected override void rollout(AIState rolloutStart)
        {
            //If the rollout start is a terminal state
            int rolloutStartResult = rolloutStart.getWinner();
            if (rolloutStartResult >= 0)
            {
                //Add a win is it is a win, or a loss is a loss or otherwise a draw
                if(rolloutStartResult == rolloutStart.playerIndex) rolloutStart.addWin();
                else if(rolloutStartResult == (rolloutStart.playerIndex+1)%2) rolloutStart.addLoss();
                else rolloutStart.addDraw (drawScore);
                return;
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
                    //record a draw
                    rolloutStart.addDraw (drawScore);
                    break;
                }
                //Default is the end of the array (because that will be the best move in a sorted list)
                int selectedChild = children.Count-1;

                //epsilon greedy move selection.
                if (randGen.NextDouble() < epsilon)
                {
                    //Sort the array (we have all ready selected the most move indx above
                    foreach(AIState child in children) if(child.stateScore == null)child.stateScore = model.evaluate(child);
                    children = AIState.mergeSort(children);
                }
                else
                {
                    //Just select a random move
                    selectedChild = randGen.Next(children.Count);
                }
                //and see if that node is terminal
                int endResult = children[selectedChild].getWinner ();
                if(endResult >= 0)
                {
                    terminalStateFound = true;
                    if(endResult == 2) rolloutStart.addDraw(drawScore);
                    //If it is a win add a win
                    else if(endResult == rolloutStart.playerIndex) rolloutStart.addWin();
                    //Else add a loss
                    else rolloutStart.addLoss();
                } else {
                    //Otherwise select that nodes as the childern and continue
                    children = children [selectedChild].generateChildren();
                }
            }
            //Reset the children as these are not 'real' children but just ones for the roll out.
            foreach( AIState child in rolloutStart.children)
            {
                child.treeNode = true;
            }
        }
    }
}