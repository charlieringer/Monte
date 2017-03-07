using System;
using System.Threading;
using System.Collections.Generic;

namespace Monte
{
	public class FitnessBasedAI : MCTSMaster
	{
		protected Thread aiThread;

		public bool done;
		public bool started;
		public AIState next;
		DLModel model;

		public FitnessBasedAI(DLModel _model)
		{
			model = _model;
		}

		public FitnessBasedAI (string modelfile)
		{
			model = new DLModel(modelfile);
		}



	    protected override void mainAlgorithm(AIState initalState)
		{
			List<AIState> children = initalState.generateChildren();
			AIState best = null;
			float? bestScore = null;
		    float total = 0.0f;
			foreach(AIState child in children)
			{
				child.stateScore = (float)model.evaluate(child.stateRep);
			    total += child.stateScore.Value;
				if (bestScore == null ||child.stateScore > bestScore) {
					best = child;
					bestScore = child.stateScore;
				}
			}
		    Console.WriteLine("Total for this itter: " + total);
			next = best;
			done = true;
		}

	    protected override void rollout(AIState initalState){}
	}
}