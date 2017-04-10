using System.Threading;
using System.Threading.Tasks;
using System;

namespace Monte
{
	public abstract class AIAgent
	{
		protected Random randGen = new Random ();
		protected Task aiThread;
		public bool done;
		public bool started;
		public AIState next;

	     public void reset()
		{
			//Resets the flags (for threading purposes)
			started = false;
			done = false;
			next = null;
		    //aiThread.Join();
		}

	    //Kicks off the the main algortims on a sperate thread
		public void run(AIState initalState)
		{
			//Make a new AI thread with this state
			aiThread = new Task (() => mainAlgorithm(initalState));
			//And start it.
		    bool aiHasStarted = false;
		    //Repeatedly trys to start a new thread (in case the first fails)
		    while (!aiHasStarted)
		    {
		        try
		        {
		            aiThread.Start();
		            aiHasStarted = true;
		        }
		        catch(SystemException)
		        {
		            //Console.WriteLine("Error: Failed to create thread. Retrying...");
		            GC.Collect();
		            GC.WaitForPendingFinalizers();
		        }
		    }
			//Set started to true
			started = true;
		}
		//Main algortim
		protected abstract void mainAlgorithm(AIState initalState);
	}
}


