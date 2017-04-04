using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Monte
{
	public class MCTSWithPruning : MCTSMasterAgent
	{
		private readonly Model model;
		private double pruningFactor;

		public MCTSWithPruning (double _thinkingTime, double _exploreWeight, int _maxRollout, Model _model, double _pruningFactor, double _drawScore)
			: base( _thinkingTime, _exploreWeight, _maxRollout, _drawScore)
		{
			model = _model;
			pruningFactor = _pruningFactor;
		}


		public MCTSWithPruning (Model _model)
		{
		    model = _model;
		    parseXML("Assets/Monte/DefaultSettings.xml");
		}

		public MCTSWithPruning (Model _model, String settingsFile) : base (settingsFile)
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
	            pruningFactor = Double.Parse(node.Attributes.GetNamedItem("PruneWorsePercent").Value);
	        }
	        catch
	        {
	            pruningFactor = 0.2;
	            Console.WriteLine("Error reading settings file when constructing MCTSWithPruning. Default settings values used (PruneWorstPercent = 0.2).");
	            Console.WriteLine("File:" + settingsFile);
	        }

	    }

		//Main MCTS algortim
		protected override void mainAlgorithm(AIState initalState)
		{
			//Make the intial children
			List<AIState> children = initalState.generateChildren ();
			//children = prune (children);

			//Get the start time
			double startTime = DateTime.Now.Ticks;
			double latestTick = startTime;
			while (latestTick-startTime < thinkingTime) {
				//Update the latest tick
				latestTick = DateTime.Now.Ticks;
				//Once done set the best child to this
				AIState bestNode = initalState;
				//And loop through it's child
				while(bestNode.children.Count > 0)
				{
					//Prune the children
				    if (bestNode.unpruned) prune(bestNode);
				    //Set the best scores and index
				    double bestScore = -1;
				    int bestIndex = -1;

					for(int i = 0; i < bestNode.children.Count; i++){
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
						double exploreRating = exploreWeight*Math.Sqrt(Math.Log(initalState.totGames+1)/(games+0.1));

						double totalScore = score+exploreRating;
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
			int mostGames = -1;
			int bestMove = -1;
			//Loop through all childern
			for(int i = 0; i < children.Count; i++)
			{
				//find the one that was played the most (this is the best move)
				int games = children[i].totGames;
				if(games >= mostGames)
				{
					mostGames = games;
					bestMove = i;
				}
			}
			//Return it.
			next = children[bestMove];
			done = true;
		}

        //Rollout function (plays random moves till it hits a termination)
	    protected override void rollout(AIState rolloutStart)
	    {
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

	            double totalScore = 0.0f;
	            List<double> scores = new List<double> ();

	            foreach(AIState child in children)
	            {
	                if (child.stateScore == null) {
	                    child.stateScore = model.evaluate(child);
	                }
	                totalScore += child.stateScore.Value;
	                scores.Add (child.stateScore.Value);
	            }
	            double randomPoint = randGen.NextDouble() * totalScore;
	            double runningTotal = 0.0f;
	            int index = 0;
	            for (int i = 0; i < scores.Count; i++) {
	                runningTotal += scores [i];
	                if (runningTotal >= randomPoint) {
	                    index = i;
	                    break;
	                }
	            }
	            //and see if that node is terminal
	            int endResult = children[index].getWinner ();
	            if(endResult >= 0)
	            {
	                terminalStateFound = true;
	                //If it is a win add a win
	                if(endResult == rolloutStart.playerIndex)
	                    rolloutStart.addWin();
	                //Else add a loss
	                else rolloutStart.addLoss();
	            } else {
	                //Otherwise select that nodes as the childern and continue
	                children = children [index].generateChildren();
	                if (children.Count == 0) {
	                    break;
	                }
	            }
	        }
	        //Reset the children as these are not 'real' children but just ones for the roll out.
	        foreach( AIState child in rolloutStart.children)
	        {
	            child.children = new List<AIState>();
	        }
	    }

		private void prune(AIState initState)
		{
		    List<AIState> children = initState.children;
		    //evaluate
		    foreach (AIState state in children) state.stateScore = model.evaluate(state);
            //Sort the children
			children = AIState.mergeSort(children);
		    //Work out how many nodes to remove
			int numbNodesToRemove = (int)Math.Floor(children.Count * pruningFactor);
		    //Remove them
			children.RemoveRange(0, numbNodesToRemove);
		    //Update the children and set unpruned to false.
		    initState.children = children;
		    initState.unpruned = false;
		}
	}
}



