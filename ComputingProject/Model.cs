using System;

public class Model
{
	public int[] w1;
	public int[] w2;

	public Model (int lengthOfInput)
	{
		w1 = new int[lengthOfInput*lengthOfInput];
		w2 = new int[lengthOfInput];
	}

	public Model (int[] _w1, int[] _w2)
	{
		w1 = _w1;
		w2 = _w2;
	}

	public Model(string file)
	{
		//TODO: Load model from file
	}

	public float evaluate(int[] stateBoard)
	{
		return 0.0f;
	}
}


