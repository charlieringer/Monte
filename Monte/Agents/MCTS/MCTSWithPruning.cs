using System;
using System.Collections.Generic;
using System.Xml;

namespace Monte
{
    //A MCTS Agent which used "Hard Pruning". Hard pruning is done by removing
    //bad nodes entirely from consideration and therefore allowing more consideration for the good nodes
	public class MCTSWithPruning : MCTSMasterAgent
	{
		public readonly Model model;
		private double pruningFactor;
	    private int stopPruningAt;

	    public MCTSWithPruning(Model _model)
	    {
	        model = _model == null ? new Model() : _model;
	        parseXML("Assets/Monte/DeafaultSettings.xml");
	    }

		public MCTSWithPruning (int _numbSimulations, double _exploreWeight, int _maxRollout, Model _model, double _pruningFactor, int _stopPruneAt, double _drawScore)
			: base( _numbSimulations, _exploreWeight, _maxRollout, _drawScore)
		{
		    model = _model == null ? new Model() : _model;
			pruningFactor = _pruningFactor;
		    stopPruningAt = _stopPruneAt;
		}

		public MCTSWithPruning (Model _model, String settingsFile) : base (settingsFile)
		{
		    model = _model == null ? new Model() : _model;
		    parseXML(settingsFile);
		}

	    void parseXML(String settingsFile)
	    {
            //Try to parse the xml
	        try
	        {
	            XmlDocument settings = new XmlDocument();
	            settings.Load(settingsFile);
	            XmlNode node = settings.SelectSingleNode("descendant::PruningSettings");
	            pruningFactor = Double.Parse(node.Attributes.GetNamedItem("PruneWorsePercent").Value);
	        }
	        catch
	        {
	            //If there are any issues that use a hard coded default.
	            pruningFactor = 0.2;
	            stopPruningAt = 10;
	            Console.WriteLine("Error reading settings file when constructing MCTSWithPruning. Default settings values used (PruneWorstPercent = 0.2, StopAt = 10). File:" + settingsFile);
	        }

	    }

		//Main MCTS algortim
		protected override void mainAlgorithm(AIState initialState)
		{
		    //Make the intial children
		    initialState.generateChildren ();
		    //Loop through all of them
		    foreach (var child in initialState.children)
		    {
		        //If any of them are winning moves
		        if (child.getWinner() == child.playerIndex)
		        {
		            //Just make that move and save on all of the comuptation
		            next = child;
		            done = true;
		            return;
		        }
		    }
		    //If no childern are generated
		    if (initialState.children.Count == 0)
		    {
		        //Report this error and return.
		        Console.WriteLine("Monte Error: State supplied has no children.");
		        next = null;
		        done = true;
		        return;
		    }
		    //Start a count
		    int count = 0;
		    //Whilst time allows
			while(count < numbSimulations)
			{
                //Increment the count
			    count++;
			    //Start at the inital state
			    AIState bestNode = initialState;
			    //And loop through it's child
				while(bestNode.children.Count > 0)
				{
					//Prune the children
				    if (bestNode.unpruned) prune(bestNode);
				    //Set the scores as a base line
				    double bestScore = -1;
				    int bestIndex = -1;
					for(int i = 0; i < bestNode.children.Count; i++)
					{
					    //win score is basically just wins/games unless no games have been played, then it is 1
					    double wins = bestNode.children[i].wins;
					    double games = bestNode.children[i].totGames;
					    double score = (games > 0) ? wins / games : 1.0;

					    //UBT (Upper Confidence Bound 1 applied to trees) function balances explore vs exploit.
					    //Because we want to change things the constant is configurable.
					    double exploreRating = exploreWeight*Math.Sqrt((2* Math.Log(initialState.totGames + 1) / (games + 0.1)));
					    //Total score is win score + explore socre
					    double totalScore = score+exploreRating;
					    //If the score is better update
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
	                //Record a draw
	                rolloutStart.addDraw (drawScore);
	                break;
	            }
	            //Get a random child index
	            int index = randGen.Next(children.Count);
	            //and see if that node is terminal
	            int endResult = children[index].getWinner ();
	            if(endResult >= 0)
	            {
	                terminalStateFound = true;
	                if (endResult == 2) rolloutStart.addDraw(drawScore);
	                //If it is a win add a win
	                else if(endResult == rolloutStart.playerIndex) rolloutStart.addWin();
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

	    //Function to prune the children of a state
		private void prune(AIState initState)
		{
		    //If we are pruning nothing then return
		    if (pruningFactor == 0) return;
            //Get the children
		    List<AIState> children = initState.children;
		    if (children.Count < stopPruningAt) return;
		    //evaluate
		    foreach (AIState state in children) state.stateScore = model.evaluate(state);
            //Sort the children
			children = AIState.mergeSort(children);
		    //Work out how many nodes to remove
			int numbNodesToRemove = (int)Math.Floor(children.Count * pruningFactor);
		    //Remove them from 0 onwards (the worse end)
			children.RemoveRange(0, numbNodesToRemove);
		    //Update the children and set unpruned to false.
		    initState.children = children;
		    initState.unpruned = false;
		}
	}
}



