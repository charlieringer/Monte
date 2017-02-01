using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

abstract public class MCTSMaster
{
	protected double thinkingTime;
	protected float exploreWeight;
	protected int maxRollout;
	protected System.Random randGen = new System.Random ();
	protected Thread aiThread;

	public bool done;
	public bool started;
	public AIState next;

	public MCTSMaster (double _thinkingTime, float _exploreWeight, int _maxRollout)
	{
		thinkingTime = _thinkingTime;
		exploreWeight = _exploreWeight;
		maxRollout = _maxRollout;
	}

	public MCTSMaster ()
	{
		XmlDocument settings = new XmlDocument ();
		settings.Load("DefaultSettings.xml"); 

		XmlNode node = settings.SelectSingleNode("MCTSSettings");
		//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");

		thinkingTime = node.Attributes.GetNamedItem("ThinkingTime").Value;
		exploreWeight = node.Attributes.GetNamedItem("ExploreWeight").Value;
		maxRollout = node.Attributes.GetNamedItem("MaxRollout").Value;

		//thinkingTime = 5.0;
		//exploreWeight = 1.45f;
		//maxRollout = 32;
	}

	public MCTSMaster (String fileName)
	{
		XmlDocument settings = new XmlDocument ();
		settings.Load("fileName"); 

		XmlNode node = settings.SelectSingleNode("MCTSSettings");
		//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");

		thinkingTime = (double)node.Attributes.GetNamedItem("ThinkingTime").Value;
		exploreWeight = (float)node.Attributes.GetNamedItem("ExploreWeight").Value;
		maxRollout = (int)node.Attributes.GetNamedItem("MaxRollout").Value;

		//thinkingTime = 5.0;
		//exploreWeight = 1.45f;
		//maxRollout = 32;
	}

	public void reset()
	{
		//Resets the flags (for threading purposes)
		started = false;
		done = false;
		next = null;
	}

	public void runAI(AIState initalState)
	{
		//Make a new AI thread with this state
		aiThread = new Thread (new ThreadStart (() => run(initalState)));
		//And start it.
		aiThread.Start ();
		//Set started to true
		started = true;
	}
	//Main MCTS algortim
	abstract public void run(AIState initalState);
	//Rollout function (plays random moves till it hits a termination)
	abstract public void rollout(AIState rolloutStart);
}




