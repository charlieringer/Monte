using System;
using System.IO;

namespace Monte
{
    internal class Network
    {
        //2d Vector where each row corresponds to a hidden layer and each column to a node in that layer.
        public double[,] biasH;
        //2d Vector where each row corresponds to a set ow weights between two hidden layers.
        public double[,] wH;
        //Vector for the second set of weights (between hidden layer 1 and the hidden layer 2).
        public double[] wOut;
        //Bias unit for output node
        public double biasOut;
        //number of layers
        private int numbHiddenLayers;
        //number of inputs
        private int lengthOfInput;
        //indx of player (for debugging)
        private int pIndx;
        //Random number gen
        private readonly Random randGen = new Random ();

        //Constructor to make a new, untrained, network
        public Network(int lengthOfInput, int numbHiddenLayers, int _pIndx)
        {
            //Init a network of weights
            initWeights(lengthOfInput, numbHiddenLayers);
            //And store which player this relates to
            pIndx = _pIndx;
        }

        //Init all the weights
        private void initWeights(int _lengthOfInput, int _numbHiddenLayers)
        {
            //Store these values (for later)
            numbHiddenLayers = _numbHiddenLayers;
            lengthOfInput = _lengthOfInput;
            //Safety check. There has to be at least 1 layer.
            if (_numbHiddenLayers < 1)
            {
                Console.WriteLine("Monte: Model settings has less than 1 layer. 1 layer is the minimum so setting the number of layers to 1.");
                _numbHiddenLayers = 1;
            }
            //allocate the arrays for all of the weights
            wH = new double[_numbHiddenLayers, _lengthOfInput * _lengthOfInput];
            wOut = new double[_lengthOfInput];
            //And bias weights
            biasH = new double[_numbHiddenLayers,_lengthOfInput];
            biasOut = 0.0;
            //set the max values for the weight d
            double weightBound = 1 / Math.Sqrt(_lengthOfInput);
            //Loop through all of these newly allocated weights
            for(int i = 0; i < _numbHiddenLayers; i++)
            {
                //And init the next weight (between -1/sqrt{inputlength} and 1/sqrt{inputlength}
                for (int j = 0; j < _lengthOfInput * _lengthOfInput; j++) wH[i, j] = getNextWeight(-weightBound, weightBound);
                for (int j = 0; j < _lengthOfInput; j++){ biasH[i,j] = getNextWeight(-weightBound, weightBound);}
            }
            //Init the weights for the output layer as well.
            for (int i = 0; i < _lengthOfInput; i++){ wOut[i] = getNextWeight(-weightBound, weightBound);}
            biasOut = getNextWeight(-weightBound, weightBound);
        }

        //Gets a weight from a uniform distribution between two bounds
        private double getNextWeight(double lower, double upper)
        {
            return randGen.NextDouble() * (upper - lower) + lower;
        }

        //function to write the network to _AppDomain supplied file
        public void writeToFile(StreamWriter writer)
        {
            //Loop through all layers
            for (int i = 0; i < numbHiddenLayers; i++)
            {
                //Write all wieghts to file
                for (int j = 0; j < lengthOfInput * lengthOfInput; j++) writer.WriteLine(wH[i, j]);
                //Then all of the bias weights
                for (int j = 0; j < lengthOfInput; j++) writer.WriteLine(biasH[i, j]);
            }
            //Next all of the output weights
            for (int i = 0; i < lengthOfInput; i++) writer.WriteLine(wOut[i]);
            //And finally the output bias
            writer.WriteLine(biasOut);
        }

        //Reads for the suppiled string[] (which came from a a file).
        public int readFromFile(string[] lines, int startLine)
        {
            //The start line is used to start at a certain point in the file
            int counter = startLine;
            //Loop thorugh all of the hidden layers
            for (int i = 0; i < numbHiddenLayers; i++)
            {
                //and ever weight in the layer
                for (int j = 0; j < lengthOfInput * lengthOfInput; j++)
                {
                    //Get the value
                    wH[i, j] = double.Parse(lines[counter]);
                    //And increment the count
                    counter++;
                }
                //Then for ever bias weight
                for (int j = 0; j < lengthOfInput; j++)
                {
                    //Do the same
                    biasH[i, j] = double.Parse(lines[counter]);
                    counter++;
                }
            }
            //Finally parse the weights for the output layer
            for (int i = 0; i < lengthOfInput; i++)
            {
                wOut[i] = double.Parse(lines[counter]);
                counter++;
            }
            biasOut = double.Parse(lines[counter]);
            //Return the counter (so we know how far through the file this took us).
            return ++counter;
        }
    }
}