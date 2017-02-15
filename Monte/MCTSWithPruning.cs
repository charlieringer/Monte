﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace Monte
{
	public class MCTSWithPruning : MCTSMaster
	{
		private DLModel model;
		private double pruningFactor;

		public MCTSWithPruning (double _thinkingTime, float _exploreWeight, int _maxRollout, DLModel _model, double _pruningFactor)
			: base( _thinkingTime, _exploreWeight, _maxRollout)
		{
			model = _model;
			pruningFactor = _pruningFactor;
		}


		public MCTSWithPruning (String modelName) : base ()
		{
			model = new DLModel (modelName);
			XmlDocument settings = new XmlDocument ();
			settings.Load("Monte/DefaultSettings.xml"); 

			XmlNode node = settings.SelectSingleNode("descendant::PruningSettings");
			pruningFactor = Double.Parse(node.Attributes.GetNamedItem("PruneWorsePercent").Value);

		}

		public MCTSWithPruning (String modelName, String settingsFile) : base (settingsFile)
		{
			model = new DLModel (modelName);
			XmlDocument settings = new XmlDocument ();
			settings.Load("settingsFile"); 

			XmlNode root = settings.DocumentElement;
			XmlNode node = root.SelectSingleNode("PruningSettings");
			pruningFactor = Double.Parse(node.Attributes.GetNamedItem("PruneWorsePercent").Value);
		}

		//Main MCTS algortim
		protected override void mainAlgorithm(AIState initalState)
		{
			//Make the intial children
			List<AIState> children = initalState.generateChildren ();
			children = prune (children);

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
					//Prune the children
					bestNode.children =  prune(bestNode.children);

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
				float totalScore = 0.0f;

				List<float> scores = new List<float> ();
				foreach(AIState child in children)
				{
					if (child.stateScore == null) {
						child.stateScore = model.evaluate(child.stateRep, child.playerIndex);
					}
					totalScore += child.stateScore.Value;
					scores.Add (child.stateScore.Value);
				}
				double randomPoint = randGen.NextDouble() * totalScore;
				float runningTotal = 0.0f;
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

		List<AIState> quickSort(List<AIState> startList)
		{
			if (startList.Count == 0) {
				return null;
			}
			foreach(AIState state in startList)
				state.stateScore = model.evaluate(state.stateRep, state.playerIndex);
			AIState pivot = startList [0];

			List<AIState> left = null;
			List<AIState> right = null;
			List<AIState> returnList = new List<AIState>();
			returnList.AddRange (left);
			returnList.Add (pivot);
			returnList.AddRange (right);
			return returnList;
		}

		private List<AIState> prune(List<AIState> list)
		{
			//Sort the list
			foreach(AIState state in list)
				state.stateScore = model.evaluate(state.stateRep, state.playerIndex);
			list = mergeSort(list);

			int numbNodesToRemove = (int)(list.Count * pruningFactor);
			list.RemoveRange(0, numbNodesToRemove);
			return list;


		}

		private static List<AIState> mergeSort(List<AIState> startList)
		{
			if (startList.Count <= 1)
			{
				return startList;
			}

			int pivot = startList.Count / 2;
			List<AIState> left = new List<AIState>();
			List<AIState> right = new List<AIState>();

			for (int i = 0; i < pivot; i++)
				left.Add(startList[i]);

			for (int i = pivot; i < startList.Count; i++)
				right.Add(startList[i]);

			left = mergeSort(left); 
			right = mergeSort(right);

			return merge(left, right);
		}

		private static List<AIState> merge(List<AIState> left, List<AIState> right)
		{
			List<AIState> returnList = new List<AIState>();
			while (left.Count > 0 && right.Count > 0 )
			{
				if (left[0].stateScore < right[0].stateScore)
				{
					returnList.Add(left[0]);
					left.RemoveAt(0);
				}
				else
				{
					returnList.Add(right[0]);
					right.RemoveAt(0);
				}
			}
			if (left.Count > 0) {
				foreach(AIState state in left)
					returnList.Add(state);
			}
			for (int i = 0; i < left.Count; i++) {
				returnList.Add (left [i]);  
			}			
			for (int i = 0; i < right.Count; i++) {
				returnList.Add (right [i]); 
			}
			return returnList;
		}
	}
}


