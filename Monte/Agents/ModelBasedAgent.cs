using System.Collections.Generic;
using System;

namespace Monte
{
    //Agent which makes it's moves based only on the model.
    //It selects the move which evaluates the best and sets that as the best move
	public class ModelBasedAgent : AIAgent
	{
	    //Model used to make choices
		private readonly Model model;
        //Constructor which takes a model as sets this model as it.
		public ModelBasedAgent(Model _model){ model = _model; }

	    protected override void mainAlgorithm(AIState initialState)
		{
		    //Create the children
			List<AIState> children = initialState.generateChildren();
		    //If no childern are generated
		    if (children.Count == 0)
		    {
		        //Report this error and return.
		        Console.WriteLine("Monte Error: State supplied has no children.");
		        next = null;
		        done = true;
		        return;
		    }
            //Otherwise loop through all the children
		    for (int i = 0; i < children.Count; i++)
		    {
		        //Is the state is a winning state
		        if (children[i].getWinner() == children[i].playerIndex)
		        {
		            //Just set it as the next move (to save computation as it is obviously a good move)
		            next = children[i];
		            done = true;
		            return;
		        }
		        //Evaluate this move
		        children[i].stateScore = model.evaluate(children[i]);
		    }
		    //If no move wins then sort the moves
		    List<AIState> sortedchildren = AIState.mergeSort(children);
		    //Set the next node as the best child
			next = sortedchildren[sortedchildren.Count-1];
		    //Then we are done
			done = true;
		}
	}
}