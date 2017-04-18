//using System.Threading.Tasks;
using System.Threading;
using System;

namespace Monte
{
	public abstract class AIAgent
	{
		protected readonly Random randGen = new Random ();
	    private Thread aiTask;
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
		    aiTask = new Thread (() => mainAlgorithm(initalState));
		    bool aiHasStarted = false;
		    //Repeatedly try to start a new thread (in case the first fails)
		    while (!aiHasStarted)
		    {
		        try
		        {
		            //Try to start the thread..
		            aiTask.Start();
		            aiHasStarted = true;
		        }
		        //Catch any failure
		        catch(SystemException)
		        {
		            Console.WriteLine("Monte Error: Failed to start AI task. Retrying...");
		            //Force a garbage collection here in case there is memory we can clean up
		            //(so the thread creation does not fail next time)
		            GC.Collect();
		            GC.WaitForPendingFinalizers();
		        }
		    }
			//Set started to true
			started = true;
		}
		//Main algortim which is implemented by the various agents.
		protected abstract void mainAlgorithm(AIState initalState);
	}
}


