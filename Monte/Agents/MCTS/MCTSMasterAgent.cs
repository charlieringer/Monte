using System.Xml;
using System;
using System.IO;

namespace Monte
{
    public abstract class MCTSMasterAgent : AIAgent
    {
        protected int numbSimulations;
        protected double exploreWeight;
        protected double drawScore;
        protected int maxRollout;

        protected MCTSMasterAgent() { parseXML("Assets/Monte/DefaultSettings.xml"); }
        protected MCTSMasterAgent (string fileName) { parseXML(fileName); }
        protected MCTSMasterAgent(int _numbSimulations, double _exploreWeight, int _maxRollout, double _drawScore)
        {
            numbSimulations = _numbSimulations;
            exploreWeight = _exploreWeight;
            maxRollout = _maxRollout;
            drawScore = _drawScore;
        }

        private void parseXML(string filePath)
        {
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
            catch (FileNotFoundException)
            {
                numbSimulations = 5000000;
                exploreWeight = 1.45;
                maxRollout = 64;
                drawScore = 0.5;
                Console.WriteLine(
                    "Error, could not find file when constructing MCTS base class. Default settings values used (ThinkingTime = 0.25 secs, ExploreWeight = 1.45, MaxRollout = 64, DrawScore = 0.5).");
                Console.WriteLine("File:" + filePath);
            }
            catch
            {
                numbSimulations = 1000;
                exploreWeight = 1.45;
                maxRollout = 64;
                drawScore = 0.5;
                Console.WriteLine(
                    "Error reading settings file when constructing MCTS base class, perhaps it is malformed. Default settings values used (NumberOfSimulations = 1000, ExploreWeight = 1.45, MaxRollout = 64, DrawScore = 0.5).");
                Console.WriteLine("File:" + filePath);
            }
        }

        //Rollout function (plays random moves till it hits a termination)
        protected abstract void rollout(AIState rolloutStart);
    }
}