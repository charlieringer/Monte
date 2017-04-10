using System.Collections.Generic;

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
		    for (int i = 0; i < children.Count; i++)
		    {
		        if (children[i].getWinner() == (initialState.playerIndex+1)%2)
		        {
		            next = children[i];
		            done = true;
		            return;
		        }
		        double thisScore = model.evaluate(children[i]);
		        children[i].stateScore = thisScore;
		    }
		    List<AIState> sortedchildren = AIState.mergeSort(children);
			//next = sortedchildren[sortedchildren.Count-1];
		    next = sortedchildren[0];
			done = true;
		}
	}
}