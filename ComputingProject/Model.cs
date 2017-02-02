using System;

public class Model
{
	public float[] w1;
	public float[] w2;
	public int lengthOfInput;


	public Model (int _lengthOfInput)
	{
		lengthOfInput = _lengthOfInput;
		w1 = new float[lengthOfInput*lengthOfInput];
		w2 = new float[lengthOfInput];
	}

	public Model (float[] _w1, float[] _w2)
	{
		w1 = _w1;
		w2 = _w2;
	}

	public Model(string file)
	{
		//TODO: Load model from file
	}

	public float evaluate(int[] stateBoard, int player)
	{
		float[] hiddenLayer = getHiddenLayer(stateBoard);
		float[] scores = getHiddenLayerWeight2(hiddenLayer);
		float logScore = 0.0f;
		if (player == 0) {
			if (scores [0] < 0)
				scores [0] = 0;
			logScore = (float)Math.Log (scores [0]);
			return sig (logScore);
		} else {
			if (scores [1] < 0) scores [1] = 0;
			logScore = (float)Math.Log (scores [1]);
			return sig (logScore);
		}
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

	private float[] getHiddenLayerWeight2(float[] hiddenLayer)
	{
		float firstEval = 0.0f;
		float secondEval = 0.0f;
		for (int i = 0; i < lengthOfInput / 2; i++) {
			firstEval += hiddenLayer [i] * w2 [i];
			secondEval = +hiddenLayer [i] * w2 [i + lengthOfInput / 2];
		}
		return new float[]{firstEval, secondEval};
	}

	private float sig(float x)
	{
		return (float)(1.0/(1.0+Math.Exp(-x)));
	}
}


