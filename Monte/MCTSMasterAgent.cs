using System.Xml;
using System;
using System.IO;

namespace Monte
{
    public abstract class MCTSMasterAgent : AIAgent
    {
        protected double thinkingTime;
        protected double exploreWeight;
        protected double drawScore;
        protected int maxRollout;

        protected MCTSMasterAgent() { parseXML("Assets/Monte/DefaultSettings.xml"); }
        protected MCTSMasterAgent (string fileName) { parseXML(fileName); }
        protected MCTSMasterAgent(double _thinkingTime, double _exploreWeight, int _maxRollout, double _drawScore)
        {
            thinkingTime = _thinkingTime*10000000;
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

                thinkingTime = double.Parse(node.Attributes.GetNamedItem("ThinkingTime").Value) * 10000000;
                exploreWeight = double.Parse(node.Attributes.GetNamedItem("ExploreWeight").Value);
                maxRollout = int.Parse(node.Attributes.GetNamedItem("MaxRollout").Value);
                drawScore = double.Parse(node.Attributes.GetNamedItem("DrawScore").Value);
            }
            catch (FileNotFoundException)
            {
                thinkingTime = 5000000;
                exploreWeight = 1.45;
                maxRollout = 64;
                drawScore = 0.5;
                Console.WriteLine(
                    "Error, could not find file. Default settings values used (ThinkingTime = 0.25 secs, ExploreWeight = 1.45, MaxRollout = 64, DrawScore = 0.5).");
            }
            catch
            {
                thinkingTime = 2500000;
                exploreWeight = 1.45;
                maxRollout = 64;
                drawScore = 0.5;
                Console.WriteLine(
                    "Error reading settings file, perhaps it is malformed. Default settings values used (ThinkingTime = 0.25 secs, ExploreWeight = 1.45, MaxRollout = 64, DrawScore = 0.5).");
            }
        }

        //Rollout function (plays random moves till it hits a termination)
        protected abstract void rollout(AIState rolloutStart);
    }
}