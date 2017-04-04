using System;
using System.IO;

namespace Monte
{
    public class Network
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
        private Random randGen = new Random (1);

        public Network(int _pIndx)
        {
            pIndx = _pIndx;
        }

        public Network(int lengthOfInput, int numbHiddenLayers, int _pIndx)
        {
            initWeights(lengthOfInput, numbHiddenLayers);
            pIndx = _pIndx;
        }

        private void initWeights(int _lengthOfInput, int _numbHiddenLayers)
        {
            numbHiddenLayers = _numbHiddenLayers;
            lengthOfInput = _lengthOfInput;
            if (_numbHiddenLayers == 0) _numbHiddenLayers = 1;
            wH = new double[_numbHiddenLayers, _lengthOfInput * _lengthOfInput];

            //w2 is double the length of input. Why? Because first first half (= to length of input) corresponds to P1 and the rest to P2.
            wOut = new double[_lengthOfInput];

            biasH = new double[_numbHiddenLayers,_lengthOfInput];
            biasOut = 0.0;
            for(int i = 0; i < _numbHiddenLayers; i++)
            {
                for (int j = 0; j < _lengthOfInput * _lengthOfInput; j++)
                {
                    wH[i, j] = getNextWeight(-1 / Math.Sqrt(_lengthOfInput), 1 / Math.Sqrt(_lengthOfInput));}
                for (int j = 0; j < _lengthOfInput; j++){ biasH[i,j] = getNextWeight(-1 / Math.Sqrt(_lengthOfInput), 1 / Math.Sqrt(_lengthOfInput));}
            }
            for (int i = 0; i < _lengthOfInput; i++){ wOut[i] = getNextWeight(-1 / Math.Sqrt(_lengthOfInput), 1 / Math.Sqrt(_lengthOfInput));}
            biasOut = getNextWeight(-1 / Math.Sqrt(_lengthOfInput), 1 / Math.Sqrt(_lengthOfInput));
        }

        private double getNextWeight(double lower, double upper)
        {
            return randGen.NextDouble() * (upper - lower) + lower;
        }

        public void writeToFile(StreamWriter writer)
        {
            for (int i = 0; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput * lengthOfInput; j++) writer.WriteLine(wH[i, j]);
                for (int j = 0; j < lengthOfInput; j++) writer.WriteLine(biasH[i, j]);
            }
            for (int i = 0; i < lengthOfInput; i++) writer.WriteLine(wOut[i]);
            writer.WriteLine(biasOut);
        }

        public int readFromFile(string[] lines, int startLine)
        {
            int counter = startLine;
            for (int i = 0; i < numbHiddenLayers; i++)
            {
                for (int j = 0; j < lengthOfInput * lengthOfInput; j++)
                {
                    wH[i, j] = double.Parse(lines[counter]);
                    counter++;
                }
                for (int j = 0; j < lengthOfInput; j++)
                {
                    biasH[i, j] = double.Parse(lines[counter]);
                    counter++;
                }
            }
            for (int i = 0; i < lengthOfInput; i++)
            {
                wOut[i] = double.Parse(lines[counter]);
                counter++;
            }
            biasOut = double.Parse(lines[counter]);
            return ++counter;
        }

    }
}