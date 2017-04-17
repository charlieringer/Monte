using System.Collections.Generic;

namespace Monte
{
	//Abstract base class for the AI state
	//This is for the Client to implement
	public abstract class AIState
	{
	    //Used so that we do not prune the same part of the tree twice (for MCTSWithPruning)
	    public bool unpruned = true;
		//Tracks Wins
		public double wins { get; set; }
		//Tracks Losses
		public int losses { get; set; }
		//Tracks total games played
		public int totGames { get; set; }
		//Which player caused that state
		public int playerIndex { get; set; }
		//How deep in the tree this is
		public int depth { get; set; }
		//It's parent
		public AIState parent { get; set; }
		//List of child nodes
		public List<AIState> children { get; set; }
		//Interger representation of the game state
		public int[] stateRep { get; set; }
		//The score which is derived from the learner and represents the score for the state
		public double? stateScore { get; set; }
	    //Number of different piece types
	    public int numbPieceTypes;
        //Used so that tree nodes are not remade (which is expensive due to evaluation) for MCTSWithLearning
	    public bool treeNode = false;

	    //All of the constructors
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

		protected AIState(int pIndex, AIState _parent, int _depth, int[] _stateRep, int _numbPieceTypes)
		{
			playerIndex = pIndex;
			parent = _parent;
			depth = _depth;
			stateRep = _stateRep;
			children = new List<AIState> ();
			wins = losses = totGames = 0;
			stateScore = null;
		    numbPieceTypes = _numbPieceTypes;
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

	    //Used to sort a list of AIState based on their stateScore.
	    public static List<AIState> mergeSort(List<AIState> startList)
	    {
	        //If the list is empty of contains 1 items is is already sorted.
	        if (startList.Count <= 1) return startList;
	        //Piviot is the mid point
	        int pivot = startList.Count / 2;
	        //Made 2 new lists, one for left and one for right
	        List<AIState> left = new List<AIState>();
	        List<AIState> right = new List<AIState>();
            //Everything left of the pivot is added to left
	        for (int i = 0; i < pivot; i++) left.Add(startList[i]);
	        //And everything right is added to the right list
	        for (int i = pivot; i < startList.Count; i++) right.Add(startList[i]);
            //Then sort them
	        left = mergeSort(left);
	        right = mergeSort(right);
            //And merge them
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
