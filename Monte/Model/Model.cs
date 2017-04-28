using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Monte
{
    //Model which is trained using RL and used either as a playout agent or to improve the MCTS
    public class Model
    {
        //Model has 2 networks, one for each player
        private Network player0Network;
        private Network player1Network;
        //And various settings for various parts of the network/training
        private int maxForwardIters;
        private int lengthOfInput;
        private double alpha;
        private double normVal;
        private double confThreshold;
        private double drawReward;
        private int numbHiddenLayers;
        private readonly Random randGen = new Random ();
        //this is passed in during training and used to make a new start state for each training game.
        public delegate AIState StateCreator();
        //Used for storing the hidden layer values, to save on allocation.
        private double[,] tempHiddenLayers;

        //If a blank model is made just parse the default settings
        public Model()
        {
            parseXML("Assets/Monte/Default Settings.xml");
        }

        //If passed one file
        public Model(string file)
        {
            //If that file is an xml file
            if (file.Contains(".xml"))
            {
                //Parse it as an xml settings file
                parseXML(file);
            } else if (file.Contains(".model"))
            {
                //Else if it is a model file parse it as a model
                parseModel(file);
                //And use the default settings
                parseXML("Assets/Monte/DefaultSettings.xml");

            } else
            {
                //If the file is neither a model file or an XML file just make an emtpy model with the default settings.
                parseXML("Assets/Monte/DefaultSettings.xml");
                Console.WriteLine("Monte: Error: File supplied was neither a model or xml file. Constucting an empty model with default settings");
            }

        }

        public Model(string modelfile, string settingsFile)
        {
            //if passed to files parse them accordingly.
            parseXML(settingsFile);
            parseModel(modelfile);
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
                //If the file count not be found default values are used
                alpha = 0.01;
                maxForwardIters = 64;
                numbHiddenLayers = 1;
                normVal = 0.05;
                confThreshold = 0.8;
                drawReward = 0.5;
                Console.WriteLine("Monte: Error: Could not find file when constructing Model. Default settings values used (Alpha = 0.01, MaxForwardItters=64, NumbHiddenLayers=1, Normalisation=0.05, DrawSore=0.5, ConfidenceThreshold=0.8). File:" + settingsFile);
            } catch
            {
                //Also if parseing fails default values are used
                alpha = 0.01;
                maxForwardIters = 64;
                numbHiddenLayers = 1;
                normVal = 0.05;
                confThreshold = 0.8;
                drawReward = 0.5;
                Console.WriteLine("Monte: Error reading settings file when constructing Model. Default settings values used (Alpha = 0.01, MaxForwardItters=64, NumbHiddenLayers=1, Normalisation=0.05, DrawSore=0.5, ConfidenceThreshold=0.8). File:" + settingsFile);
            }

        }

        private void parseModel(string modelfile)
        {
            try //try to parse the model file
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
                tempHiddenLayers = new double[numbHiddenLayers, lengthOfInput];
            }
            catch (FileNotFoundException)
            {
                //If the file could not be found error
                Console.WriteLine("Monte: Error, could not find file. Could not initalise model. Please check file/filepath. File:" + modelfile);
            }
            catch
            {
                //Likewise if the file is malformed
                Console.WriteLine("Monte: Error reading model file, perhaps it is malformed. Could not initalise model. File path:" + modelfile);
            }

        }

        //Training is done in a series of episodes where a number of games are played per episode
        public int train(int gamesPerEpisode, int episodes, StateCreator sc)
        {
            //If there are not games to play...
            if (gamesPerEpisode < 1 || episodes < 1)
            {
                Console.WriteLine("Monte Error: Games per episode or Episodes is < 1, terminating.");
                return -1;
            }
            //If the state creator is null then we cannot train
            if (sc == null)
            {
                Console.WriteLine("Monte Error: State Creator is null, terminating.");
                return -1;
            }
            //If we have not set the length of input it means we currently know nothing about the game
            //So to start off we get a state from our state creator and see how long it is
            //And use that length to build the network.
            if (lengthOfInput == 0)
            {
                AIState state = sc();
                if (!validateAIState(state))
                {
                    Console.WriteLine("Monte Error: State failed validation, terminating.");
                    return -1;
                }
                //Length of the input is the length of a preprocessed empty state.
                lengthOfInput = preprocess(sc()).Length;
                //NumbHiddenLayers coming from the Settings file used.
                player0Network = new Network(lengthOfInput, numbHiddenLayers, 0);
                player1Network = new Network(lengthOfInput, numbHiddenLayers, 1);
                tempHiddenLayers = new double[numbHiddenLayers,lengthOfInput];
            }

            //For every episode
            for(int i = 0; i < episodes; i++)
            {
                //Play n games and return the average cost for this episode (or epoch)
                double avgCost = trainingEpisode(sc, gamesPerEpisode);
                //Output this cost (so we can see if our cost is reducing
                Console.WriteLine("Monte: Training Episode " + (i+1) + " of " + episodes +" complete. Avg cost: " + avgCost);
            }

            //Once done we output it to a file which is the time it was made (so it is unique)
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
            return 1;
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
            //Once we have compelted an epoch we now backprop and get the average error (used for seeing how well it is training).
            double avgCost = backpropagate(totalInputs,totalHiddenLayers,totalResults,totalRewards, playerIndxs);
            //Return this value
            return avgCost;
        }

        //Simulates a single game game
        private void playForward(StateCreator stateCreator, List<int[]> inputs, List<double[,]> hiddenLayers, List<double> results, List<double> rewards, List<int> playerIndxs)
        {
            //Makes a new stating state
            AIState currentState = stateCreator();
            //Loop count (for detecting drawn games)
            int count = 0;
            //While there is no winner
            while(currentState.getWinner() < 0)
            {
                //Increment the move count
                count++;
                //And generate all possible moves from this state.
                List<AIState> children = currentState.generateChildren();
                //If we have hit the maximum number of moves or there are no children generated
                if (count == maxForwardIters || children.Count == 0)
                {
                    //It is a draw so work back through the moves
                    while (currentState.parent != null)
                    {
                        //And save the data
                        inputs.Add(preprocess(currentState));
                        results.Add(currentState.stateScore.Value);
                        double[,] hiddenLayer = getHiddenLayers(preprocess(currentState), currentState.playerIndex);
                        hiddenLayers.Add (hiddenLayer);
                        playerIndxs.Add(currentState.playerIndex);
                        //Adding the reward to the user defined reward for a draw
                        rewards.Add(drawReward);
                        //And set the current state as the parent
                        currentState = currentState.parent;
                    }
                    //Once done we are done with this game
                    return;
                }

                //Evaluate all moves
                foreach(AIState child in children) child.stateScore = evaluate(child);
                //and then sort them
                children = AIState.mergeSort(children);

                //Move selection:
                //Default to the best know move is one is not selected.
                int selectedChild = children.Count-1;
                //Loop backwards through the children
                for (int i = children.Count-1; i >= 0; i--)
                {
                    double randNum = randGen.NextDouble();
                    //Moves are selected with a probablity = thier score but with a confidence threshold
                    //This forces some exploration even when the network has a high confidence on the moe
                    double numberToBeat = children[i].stateScore > confThreshold ? confThreshold : children[i].stateScore.Value;
                    if (randNum < numberToBeat || children[i].getWinner() == currentState.playerIndex)
                    {
                        selectedChild = i;
                        break;
                    }
                }
                //Once we have selected a move find out if it is a terminal state
                int endResult = children[selectedChild].getWinner();
                if(endResult >= 0)
                {
                    //if it is we have reased the end of the game
                    //If it is winning add a win (which will add a loss to it's parent etc.)
                    if (endResult == children[selectedChild].playerIndex) children[selectedChild].addWin();
                    //Else add a loss
                    else children[selectedChild].addLoss();
                    break;
                }
                //Otherwise set the current state to that move a repeat.
                currentState = children[selectedChild];
            }

            //Once the game has ended and score have set set etc. store all of the data (for use in backprop)
            while (currentState.parent != null)
            {
                inputs.Add(preprocess(currentState));
                results.Add(currentState.stateScore.Value);
                rewards.Add(currentState.wins > currentState.losses ? 1 : 0);
                double[,] hiddenLayer = getHiddenLayers(preprocess(currentState), currentState.playerIndex);
                hiddenLayers.Add(hiddenLayer);
                playerIndxs.Add(currentState.playerIndex);
                currentState = currentState.parent;
            }
        }

        //Once a whole training episode has finished errors can be calcualted and weights updated.
        private double backpropagate(List<int[]> inputs, List<double[,]> hiddenLayers, List<double> output, List<double> rewards, List<int> playerIndxs)
        {
            //Used for calcualting the average cost
            double totalCost = 0.0;
            //For every input
            for (int i = 0; i < inputs.Count; i++)
            {
                //Select the relevant network based on which was used to play this move.
                Network thisPlayer = (playerIndxs[i] == 0) ? player0Network : player1Network;
                //Hidden Costs used for updating the hidden layers later...
                double[] hiddenCosts = new double[lengthOfInput];
                //Updates weights between output layer and hidden layer
                double cost = Math.Abs(output[i] - rewards[i]);
                //Update the total error
                totalCost += cost;
                double sigDir = output[i] * (1 - output[i]);
                //tot error = cost * sigdir
                double totalError = cost * sigDir;
                //We are always pulling the output slowly towards giving a 'neutral' result (0.5)
                //This is done to avoid nueron saturation
                //The difference is then multiplied by alpha and some normalisation value (to keep it really small so it does not affect training too much)
                double grtLthMidCost =( 2*Math.Abs(0.5-output[i])) * alpha * normVal;
                if (output[i] < 0.5) grtLthMidCost = -grtLthMidCost;
                //Update weights between output layer and hidden layer
                for (int j = 0; j < thisPlayer.wOut.Length; j++)
                {
                    //Cost at the last hidden layer is totError * the current weight
                    hiddenCosts[j] = totalError * thisPlayer.wOut[j];
                    //Weight is updated by the totError * the ourput at the hidden layer * alpha(learning rate)
                    double totalChange = alpha * totalError * hiddenLayers[i][numbHiddenLayers - 1, j] - grtLthMidCost;
                    thisPlayer.wOut[j] += totalChange;
                }
                //Bias is updated by alpha * totalError (* 1 as bias unit is 1 but that is pointless to calculate)
                thisPlayer.biasOut += (alpha * totalError) - grtLthMidCost;

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
                    //Update the hidden costs for the next layer going back
                    hiddenCosts = nextHiddenCosts;
                }
            }
            //Return the total cost / all inputs (to give the avg cost
            return totalCost/inputs.Count;
        }

        //Evaluate
        public double evaluate(AIState state)
        {
            //eIf the network is empty just return 0
            if (player0Network == null || player0Network.wH == null) return 0;
            //Preprocess the state
            int[] processedBoard = preprocess(state);
            //Set the tempHiddenLayers to the result of this state
            setHiddenLayers(processedBoard, state.playerIndex);
            //Get the output and return it.
            return getOutput(state.playerIndex);
        }

        //Set the temp hidden layers to the output of the Network
        private void setHiddenLayers(int[] stateBoard, int playerIndx)
        {
            //Work out which network we are using
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;
            if (thisPlayer.wH.Length == 0)
            {
                Console.WriteLine("Monte: Error, Network was not created correctly and has no weights.");
                return;
            }
            //Loop through all of the inputs
            for (int j = 0; j < lengthOfInput; j++)
            {
                //New element
                double thisElement = 0.0;
                //Loop through all of the connected weights
                for(int k = 0; k < lengthOfInput; k++)
                {
                    //And add the state board * weight to the element.
                    thisElement += stateBoard[j]*thisPlayer.wH[0, j*lengthOfInput+k];
                }
                //Add the bias
                thisElement += thisPlayer.biasH[0,j];
                //And set the first layer of inputs the tanH(total)
                tempHiddenLayers[0,j] = tanH(thisElement);
            }

            //Then work out the hidden neuron for all of the other hidden layers
            for (int i = 1; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput; j++)
                {
                    tempHiddenLayers[i,j] = getHiddenNeuron(i,j, tempHiddenLayers, thisPlayer);
                }
            }
        }

        //Returns a calculated hidden layer
        private double[,] getHiddenLayers(int[] stateBoard, int playerIndx)
        {
            //Works out what network we use
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;
            //Make a new set of layers
            double[,] hiddenLayers = new double[numbHiddenLayers, lengthOfInput];

            //Calualted the first layer (much like the above function)
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

            //Then all of the other layers (again, like the other function)
            for (int i = 1; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput; j++)
                {
                    hiddenLayers[i,j] = getHiddenNeuron(i,j, hiddenLayers, thisPlayer);
                }
            }
            //Once calculated return it
            return hiddenLayers;
        }

        //Get a hidden nueron
        private double getHiddenNeuron(int i, int j, double[,] hiddenLayers, Network thisPlayer)
        {
            //Set at 0
            double thisElement = 0.0;
            //Loop through all weights
            for(int k = 0; k < lengthOfInput; k++)
            {
                //Add them
                thisElement += hiddenLayers[i-1,j]*thisPlayer.wH[i, j*lengthOfInput+k];
            }
            //Then add the bias
            thisElement += thisPlayer.biasH[i,j];
            //Finally get the tanH value and return it
            return tanH(thisElement);
        }

        //Gets the output
        private double getOutput(int playerIndx)
        {
            //Get the correct network
            Network thisPlayer = (playerIndx == 0) ? player0Network : player1Network;

            double returnValue = 0.0f;
            //Loop through all nuerons in the last layer
            for (int i = 0; i < lengthOfInput; i++)
            {
                //And multiple thier values with the weight
                returnValue += tempHiddenLayers[numbHiddenLayers - 1, i] * thisPlayer.wOut[i];
            }
            //Add the bias
            returnValue += thisPlayer.biasOut;
            //Sig the total and return it.
            return sig(returnValue);
        }

        //Preprocesses the state and turns it into the data needed for the network.
        private static int[] preprocess(AIState state)
        {
            //The length of the processed input is staterep * number of different types of pieces
            //(because the inputs are binary)
            int[] processedInput = new int[state.stateRep.Length * state.numbPieceTypes];
            int currentType = 1;
            //Loop through the processed input
            for (int i = 0; i < processedInput.Length; i++)
            {
                //And if the are at a % point update the kind of peice we are looking for
                if(i%state.stateRep.Length == 0 && i != 0) currentType++;
                //If we have a match then set this index to 1
                if (state.stateRep[i % state.stateRep.Length] == currentType) processedInput[i] = 1;
            }
            //Once processed return it.
            return processedInput;
        }

        //Used for making sure a state provided is useable
        private static bool validateAIState(AIState state)
        {
            //If the state and state rep are not null state and it has more than one piece type it checks out so return true
            if (state?.stateRep != null && state?.numbPieceTypes > 0) return true;
            //Otherwise it fails, report this and return false.
            if (state?.stateRep == null) Console.WriteLine("Monte: Error, state or stateRep from State Creator is null, are you instantiating correctly? Terminating");
            if (state?.numbPieceTypes == 0) Console.WriteLine("Monte: Error, numbPieceTypes from State Creator is 0, are you instantiating correctly? Terminating");
            return false;
        }
        //Sigmoid function (used as output activation function)
        public static double sig(double x){ return 1.0/(1.0+Math.Exp(-x)); }
        //tanH function (used as hidden layer activation function)
        public static double tanH(double x) { return 2.0/(1.0+Math.Exp(-x*2))-1; }
    }
}