using System;
using System.Collections.Generic;


public class Learner
{
	Model currentModel;
	int maxIters = 128;
	int lengthOfInput;

	public delegate AIState StateCreator();

	private System.Random randGen = new System.Random ();

	public Learner (int _lengthOfInput)
	{
		lengthOfInput = _lengthOfInput;
		currentModel = new Model (lengthOfInput);
	}

	public Learner (Model model)
	{
		currentModel = model;
		lengthOfInput = model.w2.Length;
	}

	public Learner (string modelfile)
	{
		currentModel = new Model(modelfile);
		lengthOfInput = currentModel.w2.Length;
	}

	public Model train(int numbItters, int episodes, StateCreator sc)
	{
		for(int i = 0; i < episodes; i++)
		{
			trainingEpisode(sc, numbItters);
		}
		return currentModel;
	}

	private void trainingEpisode(StateCreator sc, int numbItters)
	{
		List<int[]> totalInputs = new List<int[]>();
		List<float[]> totalHiddenLayers = new List<float[]>();
		List<float> totalResults = new List<float>();
		List<float> totalRewards = new List<float>();

		for(int i = 0; i < numbItters; i++)
		{
			playForward(sc, totalInputs, totalHiddenLayers, totalResults, totalRewards);
		}
		//TODO: BACKPROPIGATE AND DO ALL THAT HARD STUFF
	} 

	private void playForward(StateCreator stateCreator, List<int[]> inputs, List<float[]> hiddenLayers, List<float> results, List<float> rewards)
	{
		
		AIState currentState = stateCreator();
		int count = 0;
		while(currentState.getWinner() < 0)
		{
			count++;
			if (count == maxIters)
			{
				//TODO: Handle drawn games
				break;
			}

			List<AIState> children = currentState.generateChildren();
			float totalScore = 0.0f;

			List<float> scores = new List<float> ();
			foreach(AIState child in children)
			{
				float thisScore = currentModel.evaluate(child.stateRep);
				totalScore += thisScore;
				scores.Add(child.stateScore.Value);
			}

			double randomPoint = randGen.NextDouble() * totalScore;
			float runningTotal = 0.0f;
			int i = 0;
			for (i = 0; i < scores.Count; i++) {
				runningTotal += scores [i];
				if (runningTotal >= randomPoint) {
					break;
				}
			}
			int endResult = children[i].getWinner ();
			if(endResult >= 0)
			{
				//TODO: Work out how we handle wins in a general way
				/* Py code: 
				if winResult > 0:
				if winResult == currentState.getBoard()[9]:
					currentState.addWin()
				else:
					currentState.addLoss()
				break
				*/
			} else {
				//Otherwise select that nodes as the childern and continue
				currentState = children[i];
			}
		}

		while (currentState.parent != null)
		{
			inputs.Add(currentState.stateRep);
			results.Add(currentState.stateScore.Value);
			//TODO: Correctly Calculate Reward. 
			int reward = 0;
			rewards.Add(reward);
			//TODO: Work out a good way to retrive this. 
			float[] hiddenLayer = new float[1];
			hiddenLayers.Add (hiddenLayer);
			currentState = currentState.parent;
		}
	}
}




