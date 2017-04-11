using System.Collections.Generic;
using System;

namespace Monte
{
	public class ModelBasedAgent : AIAgent
	{
		Model model;

		public ModelBasedAgent(Model _model)
		{
			model = _model;
		}

	    protected override void mainAlgorithm(AIState initialState)
		{
			List<AIState> children = initialState.generateChildren();

		    //if no childern are generated
		    if (children.Count == 0)
		    {
		        //Report this error and return.
		        Console.WriteLine("Error: State supplied has no children.");
		        next = null;
		        done = true;
		        return;
		    }

		    for (int i = 0; i < children.Count; i++)
		    {
		        if (children[i].getWinner() == children[i].playerIndex)
		        {
		            next = children[i];
		            done = true;
		            return;
		        }
		        double thisScore = model.evaluate(children[i]);
		        children[i].stateScore = thisScore;
		    }
		    List<AIState> sortedchildren = AIState.mergeSort(children);
			next = sortedchildren[sortedchildren.Count-1];
		    //next = sortedchildren[0];
			done = true;
		}
	}
}