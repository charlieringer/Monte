using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Monte
{
	public class ModelBasedAgent : AIAgent
	{
		Model model;

		public ModelBasedAgent(Model _model)
		{
			model = _model;
		}

		public ModelBasedAgent (string modelfile)
		{
			model = new Model(modelfile);
		}

	    protected override void mainAlgorithm(AIState initalState)
		{
		    //Console.WriteLine("Running main algorithm");
			List<AIState> children = initalState.generateChildren();
		    for (int i = 0; i < children.Count; i++)
		    {
		        if (children[i].getWinner() == initalState.playerIndex)
		        {
		            next = children[i];
		            done = true;
		            return;
		        }
		        double thisScore = model.evaluate(children[i]);
		        children[i].stateScore = thisScore;
		        //Console.WriteLine(thisScore);
		    }
		    List<AIState> sortedchildren = AIState.mergeSort(children);
		    if (sortedchildren[0].stateScore == sortedchildren[sortedchildren.Count - 1].stateScore)
		    {
		        //Console.WriteLine("All moves scored the same...");
		    }
		    else
		    {
		        //Console.WriteLine("Whooo, variation!!!!");
		    }
			next = sortedchildren[sortedchildren.Count-1];
			done = true;
		}
	}
}