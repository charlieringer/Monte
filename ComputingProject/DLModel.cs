using System;
using System.Collections.Generic;


public class DLModel
{
	int maxIters = 64;
	public float[] w1;
	public float[] w2;
	public int lengthOfInput;

	public delegate AIState StateCreator();

	private System.Random randGen = new System.Random ();

	public DLModel (int _lengthOfInput)
	{
		w1 = new float[_lengthOfInput * _lengthOfInput];
		w2 = new float[_lengthOfInput * 2];
		lengthOfInput = _lengthOfInput;
	}

	public DLModel (float[] _w1, float[] _w2)
	{
		w1 = _w1;
		w2 = _w2;
		lengthOfInput = w2.Length/2;
	}

	public DLModel (string modelfile)
	{
		//TODO:Read model from file
	}

	public void train(int numbItters, int episodes, StateCreator sc)
	{
		//TODO: Work out the default file name
		string defaultFileName = "";
		train (numbItters, episodes, sc, defaultFileName);
	}

	public void train(int numbItters, int episodes, StateCreator sc, string outputFile)
	{
		for(int i = 0; i < episodes; i++)
		{
			trainingEpisode(sc, numbItters);
		}
		//TODO:Write model to file
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
		float[] newW1 = new float[w1.Length];
		for(int i = 0; i < newW1.Length; i++)
			newW1[i] = w1[i];

		float[] newW2 = new float[w2.Length];
		for(int i = 0; i < newW2.Length; i++)
			newW1[i] = w2[i];

		DLModel updatedModel = new DLModel(newW1, newW2);



		/*
		for i in range(0, len(w2)):
			weight = w2[i]
			for j in range(0,len(inputStates)):
				cost = abs(results[j]-rewards[j])
				dSig = results[j]*(1-results[j])
				newW2[i]  = w2[i] - (0.1 * cost * dSig * results[j]) 

		for i in range(0, 10):
			for j in range(0, 10):
				weight = w1[i*j]
				for k in range(0,len(inputStates)):
					cost = abs(hiddenLayers[k][j]-rewards[k])
					totCost += cost
					sigHL = sig(hiddenLayers[k][j])
					dSig = sigHL*(1-sigHL)
					newW1[i*j]  = w1[i*j] - (0.1 * cost * dSig * inputStates[k].getBoard()[j]) 
		return newW1, newW2
		*/
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
				float thisScore = evaluate(child.stateRep);
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
			float[] hiddenLayer = getHiddenLayer(currentState.stateRep);
			hiddenLayers.Add (hiddenLayer);
			currentState = currentState.parent;
		}
	}

	public float evaluate(int[] stateBoard)
	{
		float[] hiddenLayer = getHiddenLayer(stateBoard);
		float score = getHiddenLayerWeight2(hiddenLayer);
		float logScore = 0.0f;
		if (score < 0) score = 0;
		logScore = (float)Math.Log(score);
		return sig(logScore);
	}

	public float[] getHiddenLayer(int[] stateBoard)
	{
		float[] hiddenLayer = new float[lengthOfInput];
		for (int i = 0; i < lengthOfInput; i++)
		{
			float thisElement = 0.0f;
			for(int j = 0; j < lengthOfInput; j++)
			{
				thisElement += stateBoard[i]*w1[i*j];
			}
			hiddenLayer[i] = thisElement;
		}
		return hiddenLayer;
	}

	private float getHiddenLayerWeight2(float[] hiddenLayer)
	{
		float returnValue = 0.0f;
		for(int i = 0; i < lengthOfInput; i++)
		{
			returnValue += hiddenLayer[i]*w2[i];
		}
		return returnValue;
	}

	private float sig(float x)
	{
		return (float)(1.0/(1.0+Math.Exp(-x)));
	}
}




