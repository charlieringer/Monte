using System.Xml;
using System;
using System.IO;

namespace Monte
{
    //Base class for all the MCTS agents
    public abstract class MCTSMasterAgent : AIAgent
    {
        //Numb simualtions = number to games to simulate before the best most is selected
        protected int numbSimulations;
        //This effects how much we weight the UCT funtion
        protected double exploreWeight;
        //That value to assign a draw in the rollout
        protected double drawScore;
        //How far do we rollout before we call it a draw
        protected int maxRollout;
        //Constructors for the Agent
        protected MCTSMasterAgent() { parseXML("Assets/Monte/DefaultSettings.xml"); }
        protected MCTSMasterAgent (string fileName) { parseXML(fileName); }
        protected MCTSMasterAgent(int _numbSimulations, double _exploreWeight, int _maxRollout, double _drawScore)
        {
            numbSimulations = _numbSimulations;
            exploreWeight = _exploreWeight;
            maxRollout = _maxRollout;
            drawScore = _drawScore;
        }
        //Reads the settings files and sets various values
        private void parseXML(string filePath)
        {
            //Try to read it.
            try
            {
                XmlDocument settings = new XmlDocument();
                settings.Load(filePath);

                XmlNode root = settings.DocumentElement;

                XmlNode node = root.SelectSingleNode("descendant::MCTSSettings");

                numbSimulations = int.Parse(node.Attributes.GetNamedItem("NumberOfSimulations").Value);
                exploreWeight = double.Parse(node.Attributes.GetNamedItem("ExploreWeight").Value);
                maxRollout = int.Parse(node.Attributes.GetNamedItem("MaxRollout").Value);
                drawScore = double.Parse(node.Attributes.GetNamedItem("DrawScore").Value);
            }
            //If the file was not found
            catch (FileNotFoundException)
            {
                numbSimulations = 500;
                exploreWeight = 1.45;
                maxRollout = 64;
                drawScore = 0.5;
                Console.WriteLine("Monte Error: could not find file when constructing MCTS base class. Default settings values used (NumberOfSimulations = 500, ExploreWeight = 1.45, MaxRollout = 64, DrawScore = 0.5). File:" + filePath);
            }
            //Or it was malformed
            catch
            {
                numbSimulations = 500;
                exploreWeight = 1.45;
                maxRollout = 64;
                drawScore = 0.5;
                Console.WriteLine(
                    "Monte, Error reading settings file when constructing MCTS base class, perhaps it is malformed. Default settings values used (NumberOfSimulations = 500, ExploreWeight = 1.45, MaxRollout = 64, DrawScore = 0.5). File:" + filePath);
            }
        }

        //Rollout function (to be written by the implementing agent)
        protected abstract void rollout(AIState rolloutStart);
    }
}