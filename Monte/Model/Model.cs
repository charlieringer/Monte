using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading;

namespace Monte
{
    public class Model
    {
        private Network player0Network;
        private Network player1Network;
        private int maxForwardIters;
        private int lengthOfInput;
        private double alpha;
        private double normVal;
        private double confThreshold;
        private double drawReward;
        private int numbHiddenLayers;
        private readonly Random randGen = new Random (1);
        public delegate AIState StateCreator();
        private readonly double[,] runTimeHiddenLayers;

        public Model()
        {
            parseXML("Assets/Monte/DefaultSettings.xml");
            runTimeHiddenLayers = new double[numbHiddenLayers, lengthOfInput];
        }

        public Model(string file)
        {
            if (file.Contains(".xml"))
            {
                parseXML(file);
            } else if (file.Contains(".model"))
            {
                parseModel(file);
                parseXML("Assets/Monte/DefaultSettings.xml");

            } else
            {
                Console.WriteLine("Error: File supplied was neither a model or xml file. Constucting an empty model with default settings");
                parseXML("Assets/Monte/DefaultSettings.xml");
            }
            runTimeHiddenLayers = new double[numbHiddenLayers, lengthOfInput];
        }

        public Model(string modelfile, string settingsFile)
        {
            parseXML(settingsFile);
            parseModel(modelfile);
            runTimeHiddenLayers = new double[numbHiddenLayers, lengthOfInput];

        }

        private void parseXML(string settingsFile)
        {
            //Reads in the settings file xml
            try
            {
                //Tries to read it in
                XmlDocument settings = new XmlDocument();
                settings.Load(settingsFile);

                XmlNode root = settings.DocumentElement;
                XmlNode node = root.SelectSingleNode("descendant::DeepLearningSettings");

                alpha = float.Parse(node.Attributes.GetNamedItem("Alpha").Value);
                maxForwardIters = int.Parse(node.Attributes.GetNamedItem("MaxForwardItters").Value);
                numbHiddenLayers = int.Parse(node.Attributes.GetNamedItem("NumbHiddenLayers").Value);
                normVal = double.Parse(node.Attributes.GetNamedItem("Normalisation").Value);
                drawReward = double.Parse(node.Attributes.GetNamedItem("DrawScore").Value);
                confThreshold = double.Parse(node.Attributes.GetNamedItem("ConfidenceThreshold").Value);
            }
            catch(FileNotFoundException)
            {
                //But if it fails default values are used
                alpha = 0.01;
                maxForwardIters = 64;
                numbHiddenLayers = 1;
                normVal = 0.05;
                confThreshold = 0.8;
                drawReward = 0.5;
                Console.WriteLine("Error, could not find file when constructing Model. Default settings values used (Alpha = 0.01, MaxForwardItters=64, NumbHiddenLayers=1, Normalisation=0.05, DrawSore=0.5, ConfidenceThreshold=0.8).");
                Console.WriteLine("File:" + settingsFile);
            } catch (Exception e)
            {
                //But if it fails default values are used
                alpha = 0.01;
                maxForwardIters = 64;
                numbHiddenLayers = 1;
                normVal = 0.05;
                confThreshold = 0.8;
                drawReward = 0.5;
                Console.WriteLine("Error reading settings file when constructing Model. Default settings values used (Alpha = 0.01, MaxForwardItters=64, NumbHiddenLayers=1, Normalisation=0.05, DrawSore=0.5, ConfidenceThreshold=0.8).");
                Console.WriteLine("File:" + settingsFile);
            }

        }

        private void parseModel(string modelfile)
        {
            try
            {
                string[] lines = File.ReadAllLines(modelfile);
                lengthOfInput = int.Parse(lines[0]);
                numbHiddenLayers = int.Parse(lines[1]);
                player0Network = new Network(lengthOfInput, numbHiddenLayers, 0);
                player1Network = new Network(lengthOfInput, numbHiddenLayers, 1);
                player0Network.wH = new double[numbHiddenLayers, lengthOfInput * lengthOfInput];
                player1Network.wH = new double[numbHiddenLayers, lengthOfInput * lengthOfInput];

                player0Network.wOut = new double[lengthOfInput];
                player1Network.wOut = new double[lengthOfInput];

                player0Network.biasH = new double[numbHiddenLayers, lengthOfInput];
                player1Network.biasH = new double[numbHiddenLayers, lengthOfInput];

                int nextStartPos = player0Network.readFromFile(lines, 2);
                player1Network.readFromFile(lines, nextStartPos);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error, could not find file. Could not initalise model. Please check file/filepath.");
                Console.WriteLine("File path:" + modelfile);
            }
            catch
            {
                Console.WriteLine("Error reading model file, perhaps it is malformed. Could not initalise model. Please check file/filepath.");
                Console.WriteLine("File path:" + modelfile);
            }

        }

        public void train(int gamesPerEpisode, int episodes, StateCreator sc)
        {
            //If the state creator is null then we cannot train
            if (sc == null)
            {
                Console.WriteLine("Error: State Creator is null, terminating.");
                return;
            }
            //If we have not set the length of input it means we currently no nothing about the game
            //So to start off we get a state from our state creator and see how long it is
            //And use that length to build the network.
            if (lengthOfInput == 0)
            {
                AIState state = sc();
                if (!validateAIState(state))
                {
                    return;
                }
                lengthOfInput = preprocess(sc().stateRep).Length;
                player0Network = new Network(lengthOfInput, numbHiddenLayers, 0);
                player1Network = new Network(lengthOfInput, numbHiddenLayers, 1);
            }

            //For every episode
            for(int i = 0; i < episodes; i++)
            {
                if (i == episodes - 1)
                {
                    Console.WriteLine("Last Epoch");
                }
                //Play n games and return the total cost for this episode (or epoch)
                double avgCost = trainingEpisode(sc, gamesPerEpisode);
                //Output this cost (so we can see if our cost is reducing
                Console.WriteLine("Training Episode " + (i+1) + " of " + episodes +" complete. Avg cost: " + avgCost);
                if (avgCost <= 0.0001) break;
            }

            //Once done we output it to a file which is the time it was made
            string dateString = String.Format("{0:HH.mm.ss_dd.MM.yyyy}",DateTime.Now);
            string fileName = "Model_" + dateString + ".model";
            File.Create(fileName).Close();
            StreamWriter writer = new StreamWriter(fileName);
            //We just write all of the values to file
            writer.WriteLine(lengthOfInput);
            writer.WriteLine(numbHiddenLayers);
            player0Network.writeToFile(writer);
            player1Network.writeToFile(writer);
            writer.Close();
        }

        private double trainingEpisode(StateCreator stateCreator, int numbItters)
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
                playForward(stateCreator, totalInputs, totalHiddenLayers, totalResults, totalRewards, playerIndxs);
            }
            //Once we have compelted an epoch we now backprop.
            double avgCost = backpropagate(totalInputs,totalHiddenLayers,totalResults,totalRewards, playerIndxs);
            return avgCost;
        }

        //Simulates a game
        private void playForward(StateCreator stateCreator, List<int[]> inputs, List<double[,]> hiddenLayers, List<double> results, List<double> rewards, List<int> playerIndxs)
        {
            //Makes a new stating state
            AIState currentState = stateCreator();
            //Loop count (for detecting drawn games)
            int count = 0;
            while(currentState.getWinner() < 0)
            {
                count++;
                List<AIState> children = currentState.generateChildren();
                if (count == maxForwardIters || children.Count == 0)
                {
                    while (currentState.parent != null)
                    {
                        inputs.Add(preprocess(currentState.stateRep));
                        results.Add(currentState.stateScore.Value);

                        rewards.Add(drawReward);

                        double[,] hiddenLayer = getHiddenLayers(preprocess(currentState.stateRep), currentState.playerIndex);
                        hiddenLayers.Add (hiddenLayer);
                        playerIndxs.Add(currentState.playerIndex);
                        currentState = currentState.parent;
                    }
                    return;
                }

                //Evaluate all moves
                foreach(AIState child in children) child.stateScore = evaluateTraining(child);
                //and then sort them
                children = AIState.mergeSort(children);

                bool childSelected = false;
                int selectedChild = 0;

                for (int i = children.Count-1; i >= 0; i--)
                {
                    double randNum = randGen.NextDouble();
                    double numberToBeat = children[i].stateScore > confThreshold ? confThreshold : children[i].stateScore.Value;
                    if (randNum < numberToBeat || children[i].getWinner() == currentState.playerIndex)
                    {
                        selectedChild = i;
                        childSelected = true;
                        break;
                    }
                }
                if (!childSelected)
                    selectedChild = 0;

                int endResult = children[selectedChild].getWinner();
                if(endResult >= 0)
                {
                    if (endResult == children[selectedChild].playerIndex) children[selectedChild].addWin();
                    else children[selectedChild].addLoss();
                    break;
                }
                currentState = children[selectedChild];
            }

            while (currentState.parent != null)
            {
                inputs.Add(preprocess(currentState.stateRep));
                results.Add(currentState.stateScore.Value);

                rewards.Add(currentState.wins > currentState.losses ? 1 : 0);
                double[,] hiddenLayer = getHiddenLayers(preprocess(currentState.stateRep), currentState.playerIndex);
                hiddenLayers.Add(hiddenLayer);
                playerIndxs.Add(currentState.playerIndex);
                currentState = currentState.parent;
            }
        }

        private double backpropagate(List<int[]> inputs, List<double[,]> hiddenLayers, List<double> output, List<double> rewards, List<int> playerIndxs)
        {
            double totalCost = 0.0;

            for (int i = 0; i < inputs.Count; i++)
            {
                totalCost += Math.Abs(output[i] - rewards[i]);
            }
            if (totalCost <= 0.0001) return totalCost;

            for (int i = 0; i < inputs.Count; i++)
            {
                //Select the relevant network based on which was used to play this move.
                Network thisPlayer = (playerIndxs[i] == 0) ? player0Network : player1Network;
                //Hidden Costs used for updating the hidden layers later...
                double[] hiddenCosts = new double[lengthOfInput];
                //Updates weights between output layer and hidden layer
                double cost = Math.Abs(output[i] - rewards[i]);
                //totalCost += cost;
                double sigDir = output[i] * (1 - output[i]);
                //tot error = cost * sigdir
                double totalError = cost * sigDir;

                double grt0Cost =( 2*Math.Abs(0.5-output[i])) * alpha * normVal;
                if (output[i] < 0.5) grt0Cost = -grt0Cost;
                //Update weights between output layer and hidden layer
                for (int j = 0; j < thisPlayer.wOut.Length; j++)
                {
                    //Cost at the last hidden layer is totError * the current weight
                    hiddenCosts[j] = totalError * thisPlayer.wOut[j];
                    //Weight is updated by the totError * the ourput at the hidden layer * alpha(learning rate)
                    double totalChange = alpha * totalError * hiddenLayers[i][numbHiddenLayers - 1, j] - grt0Cost;
                    thisPlayer.wOut[j] += totalChange;
                }
                //Bias is updated by alpha * totalError (* 1 as bias unit is 1 but that is pointless to calculate)
                thisPlayer.biasOut += (alpha * totalError) - grt0Cost;

                //Updates weights between hiddens layer and input layer
                //For every hidden layer
                for (int k = numbHiddenLayers-1; k >= 0; k--)
                {
                    //Create a new list of hidden layers
                    double[] nextHiddenCosts = new double[lengthOfInput];
                    //And for every node in that layer
                    for (int l = 0; l < lengthOfInput; l++)
                    {
                        //Work out it's cost, the weight value
                        double hCost = hiddenCosts[l];
                        //This is the node in the hidden layer whose weights we are updating
                        //(the weights between this node and all nodes in the prev layer)
                        double hiddenLayerKL = hiddenLayers[i][k,l];
                        //Calcualte the dir of the 'output' (the node in the righthand layer)
                        double tanHDir = 1 - hiddenLayerKL * hiddenLayerKL;
                        //tot error = cost * sigdir
                        double grt0HCost = -hiddenLayerKL * alpha * normVal;
                        double totalErrorH = hCost* tanHDir;
                        //For ever node in the preceding layer (or, input layer)
                        for (int m = 0; m < lengthOfInput; m++)
                        {
                            //Cost at the last hidden layer is totErrorH * the current weight
                            nextHiddenCosts[m] += totalErrorH * thisPlayer.wH[k, m * lengthOfInput + l];// - grt0Cost;
                            //if k = 0 we are on the last layer so we update with respect to the input
                            if (k == 0)
                                thisPlayer.wH[k, m * lengthOfInput + l] +=
                                    alpha * totalErrorH * inputs[i][m] + grt0HCost;
                            //Otherwise we use the value of the previous layer
                            else
                                thisPlayer.wH[k, m * lengthOfInput + l] +=
                                    alpha * hCost * totalErrorH * hiddenLayers[i][k - 1, m] + grt0HCost;
                            nextHiddenCosts[m] += totalErrorH * thisPlayer.wH[k,m * lengthOfInput + l];
                        }
                        //Once we are done updated all of the weights assoiated with this node we update the
                        //cost to the relate to the node (so we can work backwards)
                        thisPlayer.biasH[k, l] += (alpha * hCost * totalErrorH) + grt0HCost;
                    }
                    hiddenCosts = nextHiddenCosts;
                }
            }
            return totalCost/inputs.Count;
        }


        public double evaluate(AIState state)
        {
            int[] processedBoard = preprocess(state.stateRep);
            double[,] hiddenLayer = getHiddenLayers(processedBoard, state.playerIndex, runTimeHiddenLayers);
            double score = getRawScore(hiddenLayer, state.playerIndex);
            return sig(score);
        }

        public double evaluateTraining(AIState state)
        {
            int[] processedBoard = preprocess(state.stateRep);
            double[,] hiddenLayer = getHiddenLayers(processedBoard, state.playerIndex);
            double score = getRawScore(hiddenLayer, state.playerIndex);
            return sig(score);
        }

        private double[,] getHiddenLayers(int[] stateBoard, int playerIndx, double[,] hiddenLayers)
        {
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;

            for (int j = 0; j < lengthOfInput; j++)
            {
                double thisElement = 0.0;
                for(int k = 0; k < lengthOfInput; k++)
                {
                    thisElement += stateBoard[j]*thisPlayer.wH[0, j*lengthOfInput+k];
                }
                thisElement += thisPlayer.biasH[0,j];
                hiddenLayers[0,j] = tanH(thisElement);
            }

            for (int i = 1; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput; j++)
                {
                    hiddenLayers[i,j] = getHiddenNeuron(i,j, hiddenLayers, thisPlayer);
                }
            }
            return hiddenLayers;
        }

        private double getHiddenNeuron(int i, int j, double[,] hiddenLayers, Network thisPlayer)
        {
            double thisElement = 0.0;
            for(int k = 0; k < lengthOfInput; k++)
            {
                thisElement += hiddenLayers[i-1,j]*thisPlayer.wH[i, j*lengthOfInput+k];
            }
            thisElement += thisPlayer.biasH[i,j];
            return tanH(thisElement);
        }


        private double[,] getHiddenLayers(int[] stateBoard, int playerIndx)
        {
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;
            double[,] hiddenLayers = new double[numbHiddenLayers, lengthOfInput];

            for (int j = 0; j < lengthOfInput; j++)
            {
                double thisElement = 0.0;
                for(int k = 0; k < lengthOfInput; k++)
                {
                    thisElement += stateBoard[j]*thisPlayer.wH[0, j*lengthOfInput+k];
                }
                thisElement += thisPlayer.biasH[0,j];
                hiddenLayers[0,j] = tanH(thisElement);
            }

            for (int i = 1; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput; j++)
                {
                    double thisElement = 0.0;
                    for(int k = 0; k < lengthOfInput; k++)
                    {
                        thisElement += hiddenLayers[i-1,j]*thisPlayer.wH[i, j*lengthOfInput+k];
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

            if (thisPlayer?.wH == null)
            {
                Console.WriteLine("Model is null. Please check initialisation.");
                return 0;
            }

            double returnValue = 0.0f;
            for (int i = 0; i < lengthOfInput; i++)
            {
                returnValue += hiddenLayer[numbHiddenLayers - 1, i] * thisPlayer.wOut[i];
            }
            returnValue += thisPlayer.biasOut;
            return returnValue;
        }

        private static int[] preprocess(int[] rawInput)
        {
            int range = rawInput[rawInput.Length - 1];
            int[] processedInput = new int[(rawInput.Length-1) * range];
            int currentType = 1;
            for (int i = 0; i < processedInput.Length; i++)
            {
                if(i%(rawInput.Length-1) == 0 && i != 0)
                {
                    currentType++;
                    continue;
                }
                if (rawInput[i % rawInput.Length] == currentType) processedInput[i] = 1;
            }
            return processedInput;
        }

        private static bool validateAIState(AIState state)
        {
            if (state?.stateRep != null) return true;
            if(state == null) Console.WriteLine("Error, state from State Creator is null, are you instantiating correctly? Terminating");
            else Console.WriteLine("Error, stateRep from State Creator is null, are you instantiating correctly? Terminating");
            return false;
        }

        private static double sig(double x)
        {
            //s(x) = 1/1*e^-x
            return 1.0/(1.0+Math.Exp(-x));
        }

        private static double tanH(double x)
        {
            return 2.0/(1.0+Math.Exp(-x*2))-1;
        }
    }
}