using System;
using System.Threading;
using System.Collections.Generic;

namespace Monte
{
	public class ModelBased : AIAgent
	{
		Learner model;

		public ModelBased(Learner _model)
		{
			model = _model;
		}

		public ModelBased (string modelfile)
		{
			model = new Learner(modelfile);
		}

	    protected override void mainAlgorithm(AIState initalState)
		{
			List<AIState> children = initalState.generateChildren();
			AIState best = null;
			double? bestScore = null;
		    double total = 0.0f;
		    bool moveSelected = false;
			foreach(AIState child in children)
			{
				child.stateScore = (float)model.evaluate(child.stateRep, child.playerIndex);
			    total += child.stateScore.Value;
				if (bestScore == null ||child.stateScore > bestScore) {
					best = child;
					bestScore = child.stateScore;
				    moveSelected = true;
				}
			}
		    if (!moveSelected) best = children[0];
			next = best;
			done = true;
		}
	}
}