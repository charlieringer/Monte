﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Monte
{
	public class BasicMCTS : MCTSMaster
	{
		public BasicMCTS(): base(){}
		public BasicMCTS(string file):base(file){}
		public BasicMCTS (double _thinkingTime, double _exploreWeight, int _maxRollout) : base(_thinkingTime, _exploreWeight, _maxRollout){}

		//Main MCTS algortim
		protected override void mainAlgorithm(AIState initalState)
		{
			//Make the intial children
			List<AIState> children = initalState.generateChildren ();
			//Get the start time
			double startTime = (DateTime.Now.Ticks)/10000000;
			double latestTick = startTime;
			while (latestTick-startTime < thinkingTime) {
				//Update the latest tick
				latestTick = (DateTime.Now.Ticks)/10000000;
				//Set the best scores and index
				double bestScore = -1;
				int bestIndex = -1;
				//Once done set the best child to this
				AIState bestNode = initalState;
				//And loop through it's child
				while(bestNode.children.Count > 0)
				{
					//Set the scores as a base line
					bestScore = -1;
					bestIndex = -1;

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
				if (count >= maxRollout) {
					//or maxroll out is hit

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


