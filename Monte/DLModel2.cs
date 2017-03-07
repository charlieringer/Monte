using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Monte
{
    public class DLModel
    {
        private int maxForwardIters;
        private double[] biasH;
        private double biasO;
        //Vector for the first set of weights (between the input and hidden layer 1)
        private double[] w1;
        //Vector for the second set of weights (between hidden layer 1 and the hidden layer 2).
        private double[] w2;
        private int lengthOfInput;
        private float alpha;
        private Random randGen = new Random (1);

        public delegate AIState StateCreator();

        //This below is a poor way to do this. Think of a better way to stop signature classes in the constructor
        public DLModel() : this("Assets/Monte/DefaultSettings.xml", 1){}
        public DLModel(string settingsFile, int flag){ parseXML(settingsFile); }
        public DLModel (int _lengthOfInput, string settingsFile) { initWeights(_lengthOfInput); parseXML(settingsFile); }
        public DLModel (int _lengthOfInput) : this (_lengthOfInput, "Assets/Monte/DefaultSettings.xml") {}
        public DLModel (string modelfile) : this(modelfile, "Assets/Monte/DefaultSettings.xml") {}
        public DLModel (string modelfile, string settingsFile){ parseModel((modelfile)); parseXML(settingsFile);}

        private void parseXML(string settingsFile)
        {
            XmlDocument settings = new XmlDocument ();
            settings.Load(settingsFile);

            XmlNode root = settings.DocumentElement;
            XmlNode node = root.SelectSingleNode("descendant::DeepLearningSettings");
            alpha = float.Parse(node.Attributes.GetNamedItem("Alpha").Value);
            maxForwardIters = int.Parse(node.Attributes.GetNamedItem("MaxForwardItters").Value);
        }

        private void parseModel(string modelfile)
        {
            string[] lines = File.ReadAllLines(modelfile);

            lengthOfInput = int.Parse(lines[0]);
            w1 = new double[lengthOfInput * lengthOfInput];
            w2 = new double[lengthOfInput];
            biasH = new double[lengthOfInput];

            for (int i = 1; i < lines.Length; i++)
            {
                if (i < (lengthOfInput * lengthOfInput) + 1) w1[i - 1] = double.Parse(lines[i]);
                else if (i < lengthOfInput*lengthOfInput+lengthOfInput+1) w2[i-lengthOfInput*lengthOfInput-1] = double.Parse(lines[i]);
                else if (i < lines.Length-1) biasH[i-(lengthOfInput*lengthOfInput+lengthOfInput)-1] = double.Parse(lines[i]);
                else biasO = double.Parse(lines[i]);
            }
        }

        private void initWeights(int _lengthOfInput)
        {
            w1 = new double[_lengthOfInput * _lengthOfInput];
            //w2 is double the length of input. Why? Because first first half (= to length of input) corresponds to P1 and the rest to P2.
            w2 = new double[_lengthOfInput];

            biasH = new double[_lengthOfInput];
            biasO = 0.0;
            for (int i = 0; i < w1.Length; i++){ w1[i] = randGen.NextDouble();}
            for (int i = 0; i < w2.Length; i++){ w2[i] = randGen.NextDouble();}
            for (int i = 0; i < biasH.Length; i++){ biasH[i] = randGen.NextDouble();}
            biasO = randGen.NextDouble();

            lengthOfInput = _lengthOfInput;
        }

        public void train(int gamesPerEpisode, int episodes, StateCreator sc)
        {
            //If we have not set the length of input it means we currently no nothing about the game
            //So to start off we get a state from our state creator and see how long it is
            //And use that length to build the network.
            if (lengthOfInput == 0) initWeights(sc().stateRep.Length);

            //For every episode
            for(int i = 0; i < episodes; i++)
            {
                //Play n games and return the total cost for this episode (or epoch)
                double totalCost = trainingEpisode(sc, gamesPerEpisode);
                //Output this cost (so we can see if our cost is reducing
                Console.WriteLine("Training Episode " + (i+1) + " of " + episodes +" complete. Total cost: " +totalCost);
            }
            //Once done we output it to a file which is the time it was made
            DateTime date = DateTime.Now;

            string dataString = String.Format("{0:HH.mm.ss_dd.MM.yyyy}", date);
            string fileName = "Model_" + dataString + ".model";
            File.Create(fileName).Close();
            StreamWriter writer = new StreamWriter(fileName);
           //We just write all of the values to file
            writer.WriteLine(lengthOfInput);
            foreach(double value in w1)
                writer.WriteLine(value);
            foreach(double value in w2)
                writer.WriteLine(value);
            foreach(double value in biasH)
                writer.WriteLine(value);
            writer.WriteLine(biasO);
            writer.Close();
        }

        private double trainingEpisode(StateCreator sc, int numbItters)
        {
            //total inputs = all the inital states we evaluate
            List<int[]> totalInputs = new List<int[]>();
            //total hidden layers = all the hidden states we evaluate
            List<double[]> totalHiddenLayers = new List<double[]>();
            //total output = the output each time we evaluate
            List<double> totalResults = new List<double>();
            //total rewards = all the rewards
            List<double> totalRewards = new List<double>();
            //Play a game = number of times passed in
            for(int i = 0; i < numbItters; i++)
            {
                //passing in the tracking lists (which track all the values)
                playForward(sc, totalInputs, totalHiddenLayers, totalResults, totalRewards);
            }
            double totalCost = backpropagate(totalInputs,totalHiddenLayers,totalResults,totalRewards);
            return totalCost;
        }

        //Simulates a games
        private void playForward(StateCreator stateCreator, List<int[]> inputs, List<double[]> hiddenLayers, List<double> results, List<double> rewards)
        {
            //Makes a new stating state
            AIState currentState = stateCreator();
            //Loop count (for detecting drawn games
            int count = 0;
            //Reward for this play through
            double thisReward = 0;
            while(currentState.getWinner() < 0)
            {
                count++;
                List<AIState> children = currentState.generateChildren();
                if (count == maxForwardIters || children.Count == 0)
                {
                    //TODO: Consider how we handle drawn games. Currently we just ignore them.
                    return;
                }
                float totalScore = 0.0f;

                List<float> scores = new List<float> ();
                foreach(AIState child in children)
                {

                    //child.stateScore = (float)Math.Abs((child.playerIndex)-evaluate(child.stateRep));
                    child.stateScore = (float)evaluate(child.stateRep);
                    float childScore = child.stateScore.Value;
                    totalScore += child.stateScore.Value;
                    scores.Add(child.stateScore.Value);
                }

                double randomPoint = randGen.NextDouble() * totalScore;
                float runningTotal = 0.0f;
                int i = 0;
                for (; i < scores.Count; i++) {
                    runningTotal += scores [i];
                    if (runningTotal >= randomPoint) {
                        break;
                    }
                }
                int endResult = children[i].getWinner ();
                if(endResult >= 0)
                {
                    if (endResult == currentState.playerIndex) currentState.addWin();
                    else currentState.addLoss();
                    break;
                }
                //Otherwise select that node as the children and continue
                currentState = children[i];
            }

			while (currentState.parent != null)
			{
				inputs.Add(currentState.stateRep);
				results.Add(currentState.stateScore.Value);
				//TODO: Correctly Calculate Reward. I think this is done. Need to double check

				rewards.Add((currentState.wins > currentState.losses) ? 1 : 0);
			    //rewards.Add(reward);

				double[] hiddenLayer = getHiddenLayer(currentState.stateRep);
				hiddenLayers.Add (hiddenLayer);
				currentState = currentState.parent;
			}
        }

        private double backpropagate(List<int[]> inputs, List<double[]> hiddenLayers, List<double> output, List<double> rewards)
        {
            double totalCost = 0.0;
            double[] hiddenCosts = new double[lengthOfInput];

            for (int i = 0; i < inputs.Count; i++)
            {
                //Updates weights between output layer and hidden layer
                double cost = Math.Abs(output[i] - rewards[i]);
                totalCost += cost;
                double partialDir = -output[i] * (1 - output [i]);
                for (int j = 0; j < w2.Length; j++) {
                    //TODO: Results below needs to be the output from the hidden layer NOT the total output (think this is done now)
                    w2[j] += (alpha * cost * partialDir * hiddenLayers[i][j]);
                    hiddenCosts[j] = cost*partialDir*w2[j];
                }
                biasO += (alpha * cost * partialDir);

                //Updates weights between hidden layer and input layer
                for (int k = 0; k < lengthOfInput; k++) {
                    //TODO:Calculate cost correctly.
                    double hCost = hiddenCosts[k];
                    double hiddenLayerK =  (hiddenLayers [i] [k]);
                    double partialDirH = hiddenLayerK * (1 - hiddenLayerK);
                    for (int l = 0; l < lengthOfInput; l++) {
                        w1[k * lengthOfInput + l] += (alpha * hCost * partialDirH * inputs [i] [l]);
                    }
                    biasH[k] += (alpha * hCost * partialDirH);
                }
            }
            return totalCost;
        }

        public double evaluate(int[] stateBoard)
        {
            double[] hiddenLayer = getHiddenLayer(stateBoard);
            double score = getRawScore(hiddenLayer);

            //if (score < 0) score = 0; //ReLU - Not sure if this is needed.
            return sig(score);
            //return(score);
        }

        public double[] getHiddenLayer(int[] stateBoard)
        {
            double[] hiddenLayer = new double[lengthOfInput];
            for (int i = 0; i < lengthOfInput; i++)
            {
                double thisElement = 0.0;
                for(int j = 0; j < lengthOfInput; j++)
                {
                    thisElement += stateBoard[j]*w1[i*lengthOfInput+j];
                }
               // thisElement = sig(thisElement+ biasH[i]);
                thisElement += biasH[i];
                //thisElement = sig(thisElement);
               hiddenLayer[i] = tanH(thisElement);
            }
            return hiddenLayer;
        }

        private double getRawScore(double[] hiddenLayer)
        {
            double returnValue = 0.0f;
            for(int i = 0; i < lengthOfInput; i++)
            {
                returnValue += hiddenLayer[i]*w2[i];
            }
            returnValue += biasO;
            return returnValue;
        }

        private double sig(double x)
        {
            //s(x) = 1/1*e^-x
            return 1.0/(1.0+Math.Exp(-x));
        }

        private double tanH(double x)
        {
            return 2.0/(1.0+Math.Exp(-x*2))-1;
        }
    }
}