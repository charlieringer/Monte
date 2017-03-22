using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Monte
{
    public class Learner
    {
        private Network player0Network;
        private Network player1Network;
        private int maxForwardIters;
        private int lengthOfInput;
        private double alpha;
        private int numbHiddenLayers;
        private readonly Random randGen = new Random (1);
        public delegate AIState StateCreator();

        //TODO:This below is a poor way to do this. Think of a better way to stop signature classes in the constructor
        public Learner(){ parseXML("Assets/Monte/DefaultSettings.xml"); }
        public Learner(string settingsFile, int flag){ parseXML(settingsFile); }
        public Learner (string modelfile) : this(modelfile, "Assets/Monte/DefaultSettings.xml") {}
        public Learner(string modelfile, string settingsFile)
        {
            parseModel((modelfile));
            parseXML(settingsFile);
        }

        public Learner(double _alpha, int _maxItters, int _hiddenLayers)
        {
            alpha = _alpha;
            maxForwardIters = _maxItters;
            numbHiddenLayers = _hiddenLayers;
        }

        private void parseXML(string settingsFile)
        {
            XmlDocument settings = new XmlDocument ();
            settings.Load(settingsFile);

            XmlNode root = settings.DocumentElement;
            XmlNode node = root.SelectSingleNode("descendant::DeepLearningSettings");

            alpha = float.Parse(node.Attributes.GetNamedItem("Alpha").Value);
            maxForwardIters = int.Parse(node.Attributes.GetNamedItem("MaxForwardItters").Value);
            numbHiddenLayers = int.Parse(node.Attributes.GetNamedItem("NumbHiddenLayers").Value);
        }

        private void parseModel(string modelfile)
        {
            string[] lines = File.ReadAllLines(modelfile);

            lengthOfInput = int.Parse(lines[0]);
            numbHiddenLayers = int.Parse(lines[1]);
            player0Network.wH= new double[numbHiddenLayers, lengthOfInput * lengthOfInput];
            player1Network.wH = new double[numbHiddenLayers, lengthOfInput * lengthOfInput];

            player0Network.wOut = new double[lengthOfInput];
            player1Network.wOut = new double[lengthOfInput];

            player0Network.biasH = new double[numbHiddenLayers, lengthOfInput];
            player1Network.biasH = new double[numbHiddenLayers, lengthOfInput];

            //TODO: Parse model from file.
            for (int i = 1; i < lines.Length; i++)
            {
//                if (i < (lengthOfInput * lengthOfInput) + 1) w1[i - 1] = double.Parse(lines[i]);
//                else if (i < lengthOfInput*lengthOfInput+lengthOfInput+1) w2[i-lengthOfInput*lengthOfInput-1] = double.Parse(lines[i]);
//                else if (i < lines.Length-1) biasH[i-(lengthOfInput*lengthOfInput+lengthOfInput)-1] = double.Parse(lines[i]);
//                else biasOut = double.Parse(lines[i]);
            }
        }

        public void train(int gamesPerEpisode, int episodes, StateCreator sc)
        {
            //If we have not set the length of input it means we currently no nothing about the game
            //So to start off we get a state from our state creator and see how long it is
            //And use that length to build the network.
            if (lengthOfInput == 0)
            {
                lengthOfInput = sc().stateRep.Length;
                player0Network = new Network(lengthOfInput, numbHiddenLayers);
                player1Network = new Network(lengthOfInput, numbHiddenLayers);
            }

            //For every episode
            for(int i = 0; i < episodes; i++)
            {
                //Play n games and return the total cost for this episode (or epoch)
                double totalCost = trainingEpisode(sc, gamesPerEpisode);
                //Output this cost (so we can see if our cost is reducing
                Console.WriteLine("Training Episode " + (i+1) + " of " + episodes +" complete. Total cost: " +totalCost);
            }
            //Once done we output it to a file which is the time it was made
            string dataString = $"{0:HH.mm.ss_dd.MM.yyyy},date";

            string fileName = "Model_" + dataString + ".model";
            File.Create(fileName).Close();
            StreamWriter writer = new StreamWriter(fileName);
            //We just write all of the values to file
            writer.WriteLine(lengthOfInput);
            writer.WriteLine(numbHiddenLayers);
            player0Network.writeToFile(writer);
            player1Network.writeToFile(writer);
            writer.Close();
        }

        private double trainingEpisode(StateCreator sc, int numbItters)
        {
            //total inputs = all the inital states we evaluate
            List<int[]> totalInputs = new List<int[]>();
            //total hidden layers = all the hidden states we evaluate
            List<double[,]> totalHiddenLayers = new List<double[,]>();
            //total output = the output each time we evaluate
            List<double> totalResults = new List<double>();
            //total rewards = all the rewards
            List<double> totalRewards = new List<double>();
            //total rewards = all the rewards
            List<int> playerIndxs = new List<int>();
            //Play a game = number of times passed in
            for(int i = 0; i < numbItters; i++)
            {
                //passing in the tracking lists (which track all the values)
                playForward(sc, totalInputs, totalHiddenLayers, totalResults, totalRewards, playerIndxs);
            }
            double totalCost = backpropagate(totalInputs,totalHiddenLayers,totalResults,totalRewards, playerIndxs);
            return totalCost;
        }

        //Simulates a games
        private void playForward(StateCreator stateCreator, List<int[]> inputs, List<double[,]> hiddenLayers, List<double> results, List<double> rewards, List<int> playerIndxs)
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
                float bestScore = 0.0f;
                AIState bestChild = null;

                List<float> scores = new List<float> ();
                foreach(AIState child in children)
                {
                    child.stateScore = (float)evaluate(child.stateRep, child.playerIndex);
                    float childScore = child.stateScore.Value;
//                    totalScore += child.stateScore.Value;
//                    scores.Add(child.stateScore.Value);

                    if (childScore > bestScore)
                    {
                        bestScore = childScore;
                        bestChild = child;
                    }
                }

//                double randomPoint = randGen.NextDouble() * totalScore;
//                float runningTotal = 0.0f;
//                int i = 0;
//                for (; i < scores.Count; i++) {
//                    runningTotal += scores [i];
//                    if (runningTotal >= randomPoint) {
//                        break;
//                    }
//                }
//                int endResult = children[i].getWinner ();
                int endResult = bestChild.getWinner ();
                if(endResult >= 0)
                {
                    if (endResult == currentState.playerIndex) currentState.addWin();
                    else currentState.addLoss();
                    break;
                }
                //Otherwise select that node as the children and continue
//                currentState = children[i];
                currentState = bestChild;
            }

            while (currentState.parent != null)
            {
                inputs.Add(currentState.stateRep);
                results.Add(currentState.stateScore.Value);
                //TODO: Correctly Calculate Reward. I think this is done. Need to double check

                rewards.Add((currentState.wins > currentState.losses) ? 1 : 0);
                //rewards.Add(reward);

                double[,] hiddenLayer = getHiddenLayers(currentState.stateRep, currentState.playerIndex);
                hiddenLayers.Add (hiddenLayer);
                playerIndxs.Add(currentState.playerIndex);
                currentState = currentState.parent;
            }
        }

        private double backpropagate(List<int[]> inputs, List<double[,]> hiddenLayers, List<double> output, List<double> rewards, List<int> playerIndxs)
        {
            double totalCost = 0.0;
            double[] hiddenCosts = new double[lengthOfInput];

            for (int i = 0; i < inputs.Count; i++)
            {
                Network thisPlayer = (playerIndxs[i] == 0) ? player0Network : player1Network;
                //Updates weights between output layer and hidden layer
                double cost = Math.Abs(output[i] - rewards[i]);
                totalCost += cost;
                double partialDir = -output[i] * (1 - output [i]);
                for (int j = 0; j < thisPlayer.wOut.Length; j++) {
                    //TODO: Results below needs to be the output from the hidden layer NOT the total output (think this is done now)
                    thisPlayer.wOut[j] -= (alpha * cost * partialDir * hiddenLayers[i][numbHiddenLayers-1, j]);
                    hiddenCosts[j] = cost*partialDir*thisPlayer.wOut[j];
                }
                thisPlayer.biasOut -= (alpha * cost * partialDir);

                //Updates weights between hidden layer and input layer
                for (int k = numbHiddenLayers-1; k >= 0; k--)
                {
                    for (int l = 0; l < lengthOfInput; l++)
                    {
                        //TODO:Work out what the hell hiddenCosts[l] should be :(
                        double hCost = hiddenCosts[l];
                        double hiddenLayerKL = hiddenLayers[i][k,l];
                        double partialDirH = 1 - (hiddenLayerKL * hiddenLayerKL);
                        for (int m = 0; m < lengthOfInput; m++)
                        {
                            thisPlayer.wH[k,l * lengthOfInput + m] -= (alpha * hCost * partialDirH * inputs[i][m]);
                        }
                        //TODO: Check this works...
                        //Once we are done updated all of the weights assoiated with this node we update the
                        //cost to the relate to the node (so we can work backwards)
                        hiddenCosts[l] = cost*partialDir*hiddenLayers[i][k,l];
                        thisPlayer.biasH[k,l] -= (alpha * hCost * partialDirH);
                    }
                }
            }
            return totalCost;
        }

        public double evaluate(int[] stateBoard, int playerIndx)
        {
            double[,] hiddenLayer = getHiddenLayers(stateBoard, playerIndx);
            double score = getRawScore(hiddenLayer, playerIndx);

            return sig(score);
        }

        public double[,] getHiddenLayers(int[] stateBoard, int playerIndx)
        {
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;
            double[,] hiddenLayers = new double[numbHiddenLayers, lengthOfInput];

            for (int i = 0; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput; j++)
                {
                    double thisElement = 0.0;
                    for(int k = 0; k < lengthOfInput; k++)
                    {
                        if(i == 0) thisElement += stateBoard[j]*thisPlayer.wH[i, j*lengthOfInput+k];
                        else thisElement += hiddenLayers[i-1,j]*thisPlayer.wH[i, j*lengthOfInput+k];
                    }
                    thisElement += thisPlayer.biasH[i,j];
                    hiddenLayers[i,j] = tanH(thisElement);
                }
            }
            return hiddenLayers;
        }

        private double getRawScore(double[,] hiddenLayer, int playerIndx)
        {
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;

            double returnValue = 0.0f;
            for(int i = 0; i < lengthOfInput; i++)
            {
                returnValue += hiddenLayer[numbHiddenLayers-1,i]*thisPlayer.wOut[i];
            }
            returnValue += thisPlayer.biasOut;
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

        private class Network
        {
            //2d Vector where each row corresponds to a hidden layer and each column to a node in that layer.
            public double[,] biasH;
            //2d Vector where each row corresponds to a set ow weights between two hidden layers.
            public double[,] wH;
            //Vector for the second set of weights (between hidden layer 1 and the hidden layer 2).
            public double[] wOut;
            //Bias unit for output node
            public double biasOut;

            public Network(int lengthOfInput, int numbHiddenLayers)
            {
                initWeights(lengthOfInput, numbHiddenLayers);
            }

            private void initWeights(int _lengthOfInput, int _numbHiddenLayers)
            {
                //TODO: init weights correctly
                Random randGen = new Random (1);
                if (_numbHiddenLayers == 0) _numbHiddenLayers = 1;
                wH = new double[_numbHiddenLayers, _lengthOfInput * _lengthOfInput];

                //w2 is double the length of input. Why? Because first first half (= to length of input) corresponds to P1 and the rest to P2.
                wOut = new double[_lengthOfInput];

                biasH = new double[_numbHiddenLayers,_lengthOfInput];
                biasOut = 0.0;
                for(int i = 0; i < _numbHiddenLayers; i++)
                {
                    for (int j = 0; j < _lengthOfInput * _lengthOfInput; j++){ wH[i,j] = randGen.NextDouble();}
                    for (int j = 0; j < _lengthOfInput; j++){ biasH[i,j] = randGen.NextDouble();}
                }
                for (int i = 0; i < _lengthOfInput; i++){ wOut[i] = randGen.NextDouble();}
                biasOut = randGen.NextDouble();
            }

            public void writeToFile(StreamWriter writer)
            {
                //TODO: Write to file
            }
        }
    }
}