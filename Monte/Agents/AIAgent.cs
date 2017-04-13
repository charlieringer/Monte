//using System.Threading.Tasks;
using System.Threading;
using System;

namespace Monte
{
	public abstract class AIAgent
	{
		protected Random randGen = new Random ();
		//protected Task aiTask;
	    protected Thread aiTask;
		public bool done;
		public bool started;
		public AIState next;

	     public void reset()
		{
			//Resets the flags (for threading purposes)
			started = false;
			done = false;
			next = null;
		}

	    //Kicks off the the main algortims on a sperate thread
		public void run(AIState initalState)
		{
			//Make a new AI thread with this state
			//aiTask = new Task (() => mainAlgorithm(initalState));
		    aiTask = new Thread (() => mainAlgorithm(initalState));
			//And start it.
		    bool aiHasStarted = false;
		    //Repeatedly trys to start a new thread (in case the first fails)
		    while (!aiHasStarted)
		    {
		        try
		        {
		            aiTask.Start();
		            aiHasStarted = true;
		        }
		        catch(SystemException)
		        {
		           Console.WriteLine("Error: Failed to start AI task. Retrying...");

		        }
		    }
		    GC.Collect();
		    GC.WaitForPendingFinalizers();
			//Set started to true
			started = true;
		}
		//Main algortim
		protected abstract void mainAlgorithm(AIState initalState);
	}
}


