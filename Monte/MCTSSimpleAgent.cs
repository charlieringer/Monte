using System;
using System.Collections.Generic;

namespace Monte
{
	public class MCTSSimpleAgent : MCTSMasterAgent
	{
		public MCTSSimpleAgent(): base(){}
		public MCTSSimpleAgent(string file):base(file){}
		public MCTSSimpleAgent (double _thinkingTime, double _exploreWeight, int _maxRollout) : base(_thinkingTime, _exploreWeight, _maxRollout){}

		//Main MCTS algortim
	    //TODO: Handle games sttates that can not generate children.
		protected override void mainAlgorithm(AIState initalState)
		{
			//Make the intial children
			initalState.generateChildren ();
			//Get the start time
			double startTime = (DateTime.Now.Ticks)/10000000;
			double latestTick = startTime;
			while (latestTick-startTime < thinkingTime) {
			    //Console.WriteLine("Time elpased: " + (latestTick-startTime));
				//Update the latest tick
				latestTick = (DateTime.Now.Ticks)/10000000;

				//Once done set the best child to this
				AIState bestNode = initalState;
				//And loop through it's child
			    int count = 0;
				while(bestNode.children.Count > 0)
				{
				    count++;
				    if (count > 100)
				    {
				        Console.WriteLine("Problem looping through all children.");
				        break;
				    }

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
						double exploreRating = exploreWeight*Math.Sqrt(Math.Log(initalState.totGames+1)/(games+0.1));

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

			//Once we get to this point we have worked out the best move
			//So just need to return it
			int mostGames = -1;
			int bestMove = -1;
			//Loop through all childern
			for(int i = 0; i < initalState.children.Count; i++)
			{
				//find the one that was played the most (this is the best move)
				int games = initalState.children[i].totGames;
				if(games >= mostGames)
				{
					mostGames = games;
					bestMove = i;
				}
			}
			//Return it.
		    if (initalState.children.Count == 0)
		    {
		        Console.WriteLine("SERIOUS ERROR OCCURED: No children ");
		    }
		    else
		    {
		        next = initalState.children[bestMove];
		    }
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
				if (loopCount >= maxRollout) {
					//or maxroll out is hit
                    //Console.WriteLine("ERROR: Could not find a terminal state.");
					rolloutStart.addDraw ();
					return;
				}
			    if (children.Count == 0)
			    {
			        //Console.WriteLine("ERROR: No childern.");
			        rolloutStart.addDraw ();
			        return;
			    }
				//Get a random child index
				int index = randGen.Next(children.Count);
				//and see if that node is terminal
				int endResult = children[index].getWinner ();
				if(endResult >= 0)
				{
					terminalStateFound = true;
					//If it is a win add a win
					if(endResult == rolloutStart.playerIndex) rolloutStart.addWin();
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
	}
}