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
		parseXML ("DefaultSettings.xml");
		//thinkingTime = 5.0;
		//exploreWeight = 1.45f;
		//maxRollout = 64;
	}

	public MCTSMaster (String fileName)
	{
		parseXML (fileName);
		//thinkingTime = 5.0;
		//exploreWeight = 1.45f;
		//maxRollout = 64;
	}

	void parseXML(string filePath)
	{
		XmlDocument settings = new XmlDocument ();
		settings.Load("fileName"); 

		XmlNode node = settings.SelectSingleNode("MCTSSettings");
		//Maybe need XmlNode node = settings.SelectSingleNode("/MCTSSettings");

		thinkingTime = Double.Parse(node.Attributes.GetNamedItem("ThinkingTime").Value);
		exploreWeight = float.Parse(node.Attributes.GetNamedItem("ExploreWeight").Value);
		maxRollout = int.Parse(node.Attributes.GetNamedItem("MaxRollout").Value);
	}

	public void reset()
	{
		//Resets the flags (for threading purposes)
		started = false;
		done = false;
		next = null;
	}

	public void run(AIState initalState)
	{
		//Make a new AI thread with this state
		aiThread = new Thread (new ThreadStart (() => mainAlgorithm(initalState)));
		//And start it.
		aiThread.Start ();
		//Set started to true
		started = true;
	}
	//Main MCTS algortim
	abstract protected void mainAlgorithm(AIState initalState);
	//Rollout function (plays random moves till it hits a termination)
	abstract protected void rollout(AIState rolloutStart);
}




