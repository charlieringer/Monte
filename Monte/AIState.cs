using System;
using System.Collections.Generic;

namespace Monte
{
	//Abstract base class for the AI state
	//This is for the Client to implement
	public abstract class AIState
	{
		//Tracks Wins
		public int wins { get; set; }
		//Tracks Losses
		public int losses { get; set; }
		//Tracks total games played
		public int totGames { get; set; }
		//Which player is the current playing in this state
		public int playerIndex { get; set; }
		//How deep in the tree this is
		public int depth { get; set; }
		// It's parent
		public AIState parent { get; set; }
		//List of child nodes
		public List<AIState> children { get; set; }
		//Interger representation of the game state
		public int[] stateRep { get; set; }
		//The score which is derived from the learner and represents the score for the state
		public float? stateScore { get; set; }

		public AIState(int pIndex, AIState _parent, int _depth)
		{
			playerIndex = pIndex;
			parent = _parent;
			depth = _depth;
			stateRep = null;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		public AIState(int pIndex, AIState _parent, int _depth, int[] _stateRep)
		{
			playerIndex = pIndex;
			parent = _parent;
			depth = _depth;
			stateRep = _stateRep;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		public AIState(int pIndex)
		{
			playerIndex = pIndex;
			parent = null;
			depth = 0;
			stateRep = null;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		public AIState(int pIndex, int[] _stateRep)
		{
			playerIndex = pIndex;
			parent = null;
			depth = 0;
			stateRep = _stateRep;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		//For adding a win
		public void addWin(){
			wins++;
			totGames++;
			if (parent != null)
				parent.addLoss ();
		}

		//For adding a loss
		public void addLoss(){
			losses++;
			totGames++;
			if (parent != null)
				parent.addWin ();
		}

		//For adding a draw
		public void addDraw(){
			totGames++;
			if (parent != null)
				parent.addDraw ();
		}

		//These function are needed by the AI so MUST be implemented
		//For making child nodes
		public abstract List<AIState> generateChildren ();
		//For checking is a node is terminal (and which player won)ss
		public abstract int getWinner ();

	}
}
