using System;
using System.Collections.Generic;

//Abstract base class for the AI state
//This is for the Client to implement
public abstract class AIState
{
	//Tracks Wins
	private int wins = 0;
	//Tracks Losses
	private int losses = 0;
	//Tracks total games played
	private int totGames = 0;
	//Which player is the current playing in this state
	private int playerIndex;
	//How deep in the tree this is
	private int depth = 0;
	// It's parent
	private AIState parent;
	//List of child nodes
	private List<AIState> children = new List<AIState>();

	public AIState(int pIndex, AIState _parent, int _depth)
	{
		playerIndex = pIndex;
		parent = _parent;
		depth = _depth;
	}

	public AIState(int pIndex)
	{
		playerIndex = pIndex;
		parent = null;
		depth = 0;
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

