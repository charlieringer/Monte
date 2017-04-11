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

		public MCTSWithPruning (int _numbSimulations, double _exploreWeight, int _maxRollout, Model _model, double _pruningFactor, double _drawScore)
			: base( _numbSimulations, _exploreWeight, _maxRollout, _drawScore)
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
		    //Console.WriteLine("Start Count = : " + initialState.children.Count);
		    int count = 0;
			while(count < numbSimulations){
				//Once done set the best child to this
				AIState bestNode = initialState;
				//And loop through it's child
			    count++;
				while(bestNode.children.Count > 0)
				{
					//Prune the children
				    if (bestNode.unpruned) prune(bestNode);
				    //Set the best scores and index
				    double bestScore = -1;
				    int bestIndex = -1;

					for(int i = 0; i < bestNode.children.Count; i++)
					{
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
					    double exploreScore = exploreWeight * Math.Sqrt(Math.Log(initialState.totGames + 1 / (games + 0.1)));
						double totalScore = score+ exploreScore;
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
		    //Console.WriteLine("MCTS Pruning: Number of Simulations = " + count);
			//Return it.
		    //Console.WriteLine("End Count = : " + initialState.children.Count);
			next = initialState.children[bestMove];
			done = true;
		}

	    //Rollout function (plays random moves till it hits a termination)
	    protected override void rollout(AIState rolloutStart)
	    {
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

		private void prune(AIState initState)
		{
		    if (pruningFactor == 0) return;

		    double startTime = DateTime.Now.Ticks;
		    List<AIState> children = initState.children;
		    if (children.Count < 10) return;
		    //evaluate
		    foreach (AIState state in children) state.stateScore = model.evaluate(state);
		    //Console.WriteLine("Eval took :" + (DateTime.Now.Ticks - startTime)/10000000);
            //Sort the children
			children = AIState.mergeSort(children);
		    //Console.WriteLine("Sort took :" + (DateTime.Now.Ticks - startTime)/10000000);
		    //Work out how many nodes to remove
			int numbNodesToRemove = (int)Math.Floor(children.Count * pruningFactor);
		    //Remove them
			children.RemoveRange(children.Count-numbNodesToRemove-1, numbNodesToRemove);
		    //children.RemoveRange(0, numbNodesToRemove);
		    //Update the children and set unpruned to false.
		    initState.children = children;
		    initState.unpruned = false;
		    double timeCost = DateTime.Now.Ticks - startTime;
		    //Console.WriteLine("Pruning took :" + timeCost/10000000);
		}
	}
}



