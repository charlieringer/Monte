using System;
using System.Threading;
using System.Collections.Generic;

namespace Monte
{
	public class ModelBased : AIAgent
	{
		Model model;

		public ModelBased(Model _model)
		{
			model = _model;
		}

		public ModelBased (string modelfile)
		{
			model = new Model(modelfile);
		}

	    protected override void mainAlgorithm(AIState initalState)
		{
			List<AIState> children = initalState.generateChildren();
		    for (int i = 0; i < children.Count; i++)
		    {
		        if (children[i].getWinner() == initalState.playerIndex)
		        {
		            next = children[i];
		            done = true;
		            return;
		        }
		        children[i].stateScore = model.evaluate(children[i]);
		    }
		    List<AIState> sortedchildren = AIState.mergeSort(children);
			next = sortedchildren[0];
			done = true;
		}
	}
}