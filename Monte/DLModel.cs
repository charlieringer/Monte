using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Monte
{
	public class DLModel
	{
		int maxIters = 64;
	    //Vector for the first set of weights (between the input and hidden layer 1)
		public double[] w1;
	    //Vector for the second set of weights (between hidden layer 1 and the hidden layer 2).
		public double[] w2;
		public int lengthOfInput;
		private float alpha;

		public delegate AIState StateCreator();

		private System.Random randGen = new System.Random ();

		public DLModel (int _lengthOfInput, string settingsFile)
		{
			w1 = new double[_lengthOfInput * _lengthOfInput];
			//w2 is double the length of input. Why? Because first first half (= to length of input) corresponds to P1 and the rest to P2.
			w2 = new double[_lengthOfInput * 2];
		    for (int i = 0; i < w1.Length; i++){ w1[i] = randGen.NextDouble();}
		    for (int i = 0; i < w2.Length; i++){ w2[i] = randGen.NextDouble();}

			lengthOfInput = _lengthOfInput;

			XmlDocument settings = new XmlDocument ();
			settings.Load(settingsFile);

			XmlNode node = settings.SelectSingleNode("descendant::DeepLearningSettings");
			//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");
		    alpha = float.Parse(node.Attributes.GetNamedItem("Alpha").Value);
		    //if (node != null) alpha = float.Parse(node.Attributes.GetNamedItem("Alpha").Value);
		    //else alpha = 0.1;
		}

		public DLModel (int _lengthOfInput) : this (_lengthOfInput, "Assets/Monte/DefaultSettings.xml") {}

		public DLModel (string modelfile) : this(modelfile, "Assets/Monte/DefaultSettings.xml") {}

		public DLModel (string modelfile, string settingsFile)
		{
			//TODO:Read model from file
		    string[] lines = File.ReadAllLines(modelfile);

		    lengthOfInput = int.Parse(lines[0]);
		    w1 = new double[lengthOfInput * lengthOfInput];
		    w2 = new double[lengthOfInput * 2];
		    for (int i = 1; i < lines.Length; i++)
		    {
		        if (i < (lengthOfInput * lengthOfInput) + 1) w1[i - 1] = double.Parse(lines[i]);
		        else w2[i-lengthOfInput*lengthOfInput-1] = double.Parse(lines[i]);
		    }

			XmlDocument settings = new XmlDocument ();
			settings.Load(settingsFile);

			XmlNode root = settings.DocumentElement;
			XmlNode node = root.SelectSingleNode("descendant::DeepLearningSettings");
			//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");
			alpha = float.Parse(node.Attributes.GetNamedItem("Alpha").Value);
		}

		public void train(int gamesPerEpisode, int episodes, StateCreator sc)
		{
			for(int i = 0; i < episodes; i++)
			{
				double totalCost = trainingEpisode(sc, gamesPerEpisode);
			    Console.WriteLine("Training Episode " + (i+1) + " of " + episodes +" complete. Total cost: " +totalCost);
			}
			//TODO:Write model to file
			DateTime date = DateTime.Now;

		    string dataString = String.Format("{0:HH.mm.ss_dd.MM.yyyy}", date);
		    string fileName = "Model_" + dataString + ".model";
		    File.Create(fileName).Close();
		    StreamWriter writer = new StreamWriter(fileName);
		    writer.WriteLine(lengthOfInput);
		    foreach(double value in w1)
		        writer.WriteLine(value);
		    foreach(double value in w2)
		        writer.WriteLine(value);
		    writer.Close();
		}

		private double trainingEpisode(StateCreator sc, int numbItters)
		{
		    double totalCost = 0.0;
			List<int[]> totalInputs = new List<int[]>();
			List<double[]> totalHiddenLayers = new List<double[]>();
			List<double> totalResults = new List<double>();
			List<double> totalRewards = new List<double>();
			List<double> playerIndxs = new List<double>();

			for(int i = 0; i < numbItters; i++)
			{
				playForward(sc, totalInputs, totalHiddenLayers, totalResults, totalRewards, playerIndxs);
			}
			double[] newW1 = new double[w1.Length];
			for(int i = 0; i < newW1.Length; i++)
				newW1[i] = w1[i];

			double[] newW2 = new double[w2.Length];
			for(int i = 0; i < newW2.Length; i++)
				newW1[i] = w2[i];

			/* NOTE: TODO NEED TO ALLOW FOR THE NEW NN METHOD
			 * Where we have 2 outputs, one for each player
			 *
			 * Actually it might be fine, let me think about it somemore.
			 */
			for (int i = 0; i < newW2.Length; i++) {
				double currentWeight = w2 [i];
				for (int j = 0; j < totalInputs.Count; j++) {
					double cost = Math.Abs(totalResults [j] - totalRewards [j]);
				    totalCost += cost;
					double deltaSigmoid = totalResults [j] * (1 - totalResults [j]);
					//TODO: Investigate the line below. I think it is wrong.
					currentWeight += (alpha * cost * deltaSigmoid * totalResults [j]);
				}
			    newW2[i] = currentWeight;
			}
			for (int i = 0; i < lengthOfInput; i++) {
				for (int j = 0; j < lengthOfInput; j++) {
					double currentWeight = w1 [i*j];
					for (int k = 0; k < totalInputs.Count; k++) {
						double cost = Math.Abs (totalHiddenLayers [k] [j] - totalRewards [k]);
						double hiddenLayerSig = sig (totalHiddenLayers [k] [j]);
						double deltaSigmoid = hiddenLayerSig * (1 - hiddenLayerSig);
						//TODO: Investigate the line below. I think it is wrong.
						currentWeight += (alpha * cost * deltaSigmoid * totalInputs [k] [j]);
					}
				    newW1[i * j] = currentWeight;
				}
			}
			w1 = newW1;
			w2 = newW2;
		    return totalCost;
		}

		private void playForward(StateCreator stateCreator, List<int[]> inputs, List<double[]> hiddenLayers, List<double> results, List<double> rewards, List<double> playerIndxs)
		{

			AIState currentState = stateCreator();
			int count = 0;
			while(currentState.getWinner() < 0)
			{
				count++;
			    List<AIState> children = currentState.generateChildren();
				if (count == maxIters || children.Count == 0)
				{
					//TODO: Consider how we handle drawn games. Currently we just ignore them.
					return;
				}
				float totalScore = 0.0f;

				List<float> scores = new List<float> ();
				foreach(AIState child in children)
				{
					child.stateScore = (float)evaluate(child.stateRep, child.playerIndex);

					totalScore += child.stateScore.Value;
				    if (child.stateScore.HasValue) scores.Add(child.stateScore.Value);
				    else scores.Add(0);
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
						currentState.addLoss (); //NOTE: Just switched these. Probably wrong. Prepare to switch back
					else
						currentState.addWin ();
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
				double[] hiddenLayer = getHiddenLayer(currentState.stateRep);
				hiddenLayers.Add (hiddenLayer);
				currentState = currentState.parent;
			}
		}

		public double evaluate(int[] stateBoard, int pIndex)
		{
			double[] hiddenLayer = getHiddenLayer(stateBoard);
			double score = getRawScore(hiddenLayer, pIndex);
		    if (score < 0) score = 0;

		    double logScore = (float)Math.Log(score);
			return sig(logScore);
		}

		public double[] getHiddenLayer(int[] stateBoard)
		{
			double[] hiddenLayer = new double[lengthOfInput];
			for (int i = 0; i < lengthOfInput; i++)
			{
				double thisElement = 0.0f;
				for(int j = 0; j < lengthOfInput; j++)
				{
					thisElement += stateBoard[i]*w1[i*j];
				}
			    sig(thisElement);
				hiddenLayer[i] = thisElement;
			}
			return hiddenLayer;
		}

		private double getRawScore(double[] hiddenLayer, int pIndex)
		{
			int start = lengthOfInput * pIndex;
			int end = start + lengthOfInput;
			double returnValue = 0.0f;
			for(int i = start; i < end; i++)
			{
				returnValue += hiddenLayer[i%lengthOfInput]*w2[i];
			}
			return returnValue;
		}

		private double sig(double x)
		{
			return (1.0/(1.0+Math.Exp(-x)));
		}
	}
}



