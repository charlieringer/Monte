using System;
using System.Collections.Generic;
using System.Xml;

namespace Monte
{
	public class DLModel
	{
		int maxIters = 64;
		public float[] w1;
		public float[] w2;
		public int lengthOfInput;
		private float alpha;

		public delegate AIState StateCreator();

		private System.Random randGen = new System.Random ();

		public DLModel (int _lengthOfInput, string settingsFile)
		{
			w1 = new float[_lengthOfInput * _lengthOfInput];
			//w2 is double the length of input. Why? Because first first half (= to length of input) corresponds to P1 and the rest to P2.
			w2 = new float[_lengthOfInput * 2];
			lengthOfInput = _lengthOfInput;

			XmlDocument settings = new XmlDocument ();
			settings.Load(settingsFile); 

			XmlNode node = settings.SelectSingleNode("DeepLearningSettings");
			//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");
			alpha = float.Parse(node.Attributes.GetNamedItem("Aplha").Value);
		}

		public DLModel (int _lengthOfInput) : this (_lengthOfInput, "Monte/DefaultSettings.xml") {}

		public DLModel (string modelfile) : this(modelfile, "Monte/DefaultSettings.xml") {}

		public DLModel (string modelfile, string settingsFile)
		{
			//TODO:Read model from file

			XmlDocument settings = new XmlDocument ();
			settings.Load(settingsFile); 
			
			XmlNode root = settings.DocumentElement;
			XmlNode node = root.SelectSingleNode("descendant::DeepLearningSettings");
			//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");
			alpha = float.Parse(node.Attributes.GetNamedItem("Aplha").Value);
		}

		public void train(int numbItters, int episodes, StateCreator sc)
		{
			for(int i = 0; i < episodes; i++)
			{
				trainingEpisode(sc, numbItters);
			}
			//TODO:Write model to file
			string date = DateTime.Now.ToShortDateString();
			string time = DateTime.Now.ToShortTimeString();
			string fileName = "Model_" + date + time;
		}

		private void trainingEpisode(StateCreator sc, int numbItters)
		{
			List<int[]> totalInputs = new List<int[]>();
			List<float[]> totalHiddenLayers = new List<float[]>();
			List<float> totalResults = new List<float>();
			List<float> totalRewards = new List<float>();
			List<float> playerIndxs = new List<float>();

			for(int i = 0; i < numbItters; i++)
			{
				playForward(sc, totalInputs, totalHiddenLayers, totalResults, totalRewards, playerIndxs);
			}
			float[] newW1 = new float[w1.Length];
			for(int i = 0; i < newW1.Length; i++)
				newW1[i] = w1[i];

			float[] newW2 = new float[w2.Length];
			for(int i = 0; i < newW2.Length; i++)
				newW1[i] = w2[i];

			/* NOTE: TODO NEED TO ALLOW FOR THE NEW NN METHOD
			 * Where we have 2 outputs, one for each player
			 * 
			 * Actually it might be fine, let me think about it somemore.
			 */
			for (int i = 0; i < newW2.Length; i++) {
				float currentWeight = w2 [i];
				for (int j = 0; j < totalInputs.Count; j++) {
					float cost = Math.Abs(totalResults [j] - totalRewards [j]);
					float deltaSigmoid = totalResults [j] * (1 - totalResults [j]);
					//TODO: Investigate the line below. I think it is wrong. 
					newW2 [i] = currentWeight - (alpha * cost * deltaSigmoid * totalResults [j]);
				}
			}
			for (int i = 0; i < lengthOfInput; i++) {
				for (int j = 0; j < lengthOfInput; j++) {
					float currentWeight = w1 [i*j];
					for (int k = 0; k < totalInputs.Count; k++) {
						float cost = Math.Abs (totalHiddenLayers [k] [j] - totalRewards [k]);
						float hiddenLayerSig = sig (totalHiddenLayers [k] [j]);
						float deltaSigmoid = hiddenLayerSig * (1 - hiddenLayerSig);
						//TODO: Investigate the line below. I think it is wrong. 
						newW1 [i * j] = currentWeight - (alpha * cost * deltaSigmoid * totalInputs [k] [j]);

					}
				}
			}
			w1 = newW1;
			w2 = newW2;
		} 

		private void playForward(StateCreator stateCreator, List<int[]> inputs, List<float[]> hiddenLayers, List<float> results, List<float> rewards, List<float> playerIndxs)
		{
			
			AIState currentState = stateCreator();
			int count = 0;
			while(currentState.getWinner() < 0)
			{
				count++;
				if (count == maxIters)
				{
					//TODO: Consider how we handle drawn games. Currently we just ignore them.
					return;
				}

				List<AIState> children = currentState.generateChildren();
				float totalScore = 0.0f;

				List<float> scores = new List<float> ();
				foreach(AIState child in children)
				{
					float thisScore = evaluate(child.stateRep, child.playerIndex);
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
					if (endResult == currentState.playerIndex)
						currentState.addWin ();
					else
						currentState.addLoss ();
					break;
				} else {
					//Otherwise select that nodes as the children and continue
					currentState = children[i];
				}
			}

			while (currentState.parent != null)
			{
				inputs.Add(currentState.stateRep);
				results.Add(currentState.stateScore.Value);
				//TODO: Correctly Calculate Reward. I think this is done. Need to double check
				rewards.Add((currentState.wins > currentState.losses) ? 1 : -1);
				playerIndxs.Add (currentState.playerIndex);
				float[] hiddenLayer = getHiddenLayer(currentState.stateRep);
				hiddenLayers.Add (hiddenLayer);
				currentState = currentState.parent;
			}
		}

		public float evaluate(int[] stateBoard, int pIndex)
		{
			
			float[] hiddenLayer = getHiddenLayer(stateBoard);
			float score = getRawScore(hiddenLayer, pIndex);
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

		private float getRawScore(float[] hiddenLayer, int pIndex)
		{
			int start = lengthOfInput * pIndex;
			int end = start + lengthOfInput;
			float returnValue = 0.0f;
			for(int i = start; i < end; i++)
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
}



