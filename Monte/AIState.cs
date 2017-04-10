using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Monte
{
	//Abstract base class for the AI state
	//This is for the Client to implement
	public abstract class AIState
	{
	    public bool unpruned = true;
		//Tracks Wins
		public double wins { get; set; }
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
		public double? stateScore { get; set; }

	    public bool treeNode = false;

	    protected AIState(){}

	    protected AIState(int pIndex, AIState _parent, int _depth)
		{
			playerIndex = pIndex;
			parent = _parent;
			depth = _depth;
			stateRep = null;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		protected AIState(int pIndex, AIState _parent, int _depth, int[] _stateRep)
		{
			playerIndex = pIndex;
			parent = _parent;
			depth = _depth;
			stateRep = _stateRep;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		protected AIState(int pIndex)
		{
			playerIndex = pIndex;
			parent = null;
			depth = 0;
			stateRep = null;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		}

		protected AIState(int pIndex, int[] _stateRep)
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
			parent?.addLoss ();
		}

		//For adding a loss
		public void addLoss(){
			losses++;
			totGames++;
		    parent?.addWin ();
		}

		//For adding a draw
		public void addDraw(double value)
		{
		    wins += value;
			totGames++;
			parent?.addDraw (value);
		}

		//These function are needed by the AI so MUST be implemented
		//For making child nodes
		public abstract List<AIState> generateChildren ();
		//For checking is a node is terminal (and which player won)ss
		public abstract int getWinner ();

	    public static List<AIState> mergeSort(List<AIState> startList)
	    {
	        if (startList.Count <= 1) return startList;


	        int pivot = startList.Count / 2;
	        List<AIState> left = new List<AIState>();
	        List<AIState> right = new List<AIState>();

	        for (int i = 0; i < pivot; i++) left.Add(startList[i]);
	        for (int i = pivot; i < startList.Count; i++) right.Add(startList[i]);

	        left = mergeSort(left);
	        right = mergeSort(right);

	        return merge(left, right);
	    }

	    private static List<AIState> merge(List<AIState> left, List<AIState> right)
	    {
	        List<AIState> returnList = new List<AIState>();
	        //Whilst both lists have elements
	        while (left.Count > 0 && right.Count > 0)
	        {
	            //If the left is smaller
	            if (left[0].stateScore < right[0].stateScore)
	            {
	                //Add it (and remove from left)
	                returnList.Add(left[0]);
	                left.RemoveAt(0);
	            }
	            else
	            {
	                //Otherwise add right (and remove it from right)
	                returnList.Add(right[0]);
	                right.RemoveAt(0);
	            }
	        }
	        //Add any remaining parts...
	        foreach(AIState state in left) returnList.Add(state);
	        foreach (AIState state in right) returnList.Add(state);
	        return returnList;
	    }
	}
}
