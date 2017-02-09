using System;
using System.Threading;
using System.Collections.Generic;


public class FitnessBasedAI
{
	protected System.Random randGen = new System.Random ();
	protected Thread aiThread;

	public bool done;
	public bool started;
	public AIState next;
	DLModel model;

	public FitnessBasedAI(DLModel _model)
	{
		model = _model;
	}

	public FitnessBasedAI (string modelfile)
	{
		model = new DLModel(modelfile);
	}

	public void run(AIState initalState)
	{
		//Make a new AI thread with this state
		aiThread = new Thread (new ThreadStart (() => mainAlgortim(initalState)));
		//And start it.
		aiThread.Start ();
		//Set started to true
		started = true;
	}

	public void reset()
	{
		//Resets the flags (for threading purposes)
		started = false;
		done = false;
		next = null;
	}
	private void mainAlgortim(AIState initalState)
	{
		List<AIState> children = initalState.generateChildren();
		AIState best = null;
		float? bestScore = null;
		foreach(AIState child in children)
		{
			child.stateScore = model.evaluate(child.stateRep);
			if (bestScore == null ||child.stateScore > bestScore) {
				best = child;
				bestScore = child.stateScore;
			}
		}
		next = best;
		done = true;
	}
}

