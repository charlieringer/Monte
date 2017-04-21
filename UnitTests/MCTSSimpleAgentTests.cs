using NUnit.Framework;
using Monte;

namespace UnitTests
{
    public class MCTSSimpleAgentTests
    {
        [Test]
        public void MCTSSimpleAgentTests_Init_NullSettings()
        {
            AIAgent agent = new MCTSSimpleAgent(null);
            Assert.True(agent != null);
        }

        [Test]
        public void MCTSSimpleAgentTests_Init_NoSettings()
        {
            AIAgent agent = new MCTSSimpleAgent("");
            Assert.True(agent != null);
        }

        [Test]
        public void MCTSSimpleAgentTests_Run_NullInitalState()
        {
            AIAgent agent = new MCTSSimpleAgent("DefaultSettings.xml");
            agent.run(null);
            bool aiDone = false;
            while (!aiDone)
            {
                if (agent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = agent.next;
            Assert.True(next == null);
        }

        [Test]
        public void MCTSSimpleAgentTests_Run_NoChildren()
        {
            AIAgent agent = new MCTSSimpleAgent("DefaultSettings.xml");
            agent.run(new TestAIState("empty"));
            bool aiDone = false;
            while (!aiDone)
            {
                if (agent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = agent.next;
            Assert.True(next == null);
        }

        [Test]
        public void MCTSSimpleAgentTests_Run_Children()
        {
            AIAgent agent = new ModelBasedAgent(new Model());
            agent.run(new TestAIState("children"));
            bool aiDone = false;
            while (!aiDone)
            {
                if (agent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = agent.next;
            Assert.True(next != null);
        }
    }
}