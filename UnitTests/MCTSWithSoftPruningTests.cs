using NUnit.Framework;
using Monte;

namespace UnitTests
{
    public class MCTSWithSoftPruningTests
    {
        [Test]
        public void MCTSWithSoftPruningTests_Init_NullModel()
        {
            MCTSWithSoftPruning agent = new MCTSWithSoftPruning(null);
            Assert.True(agent != null);
            Assert.True(agent.model != null);
        }

        [Test]
        public void MCTSWithSoftPruningTests_Init_NullModel_Settings()
        {
            MCTSWithSoftPruning agent = new MCTSWithSoftPruning(null,"");
            Assert.True(agent != null);
            Assert.True(agent.model != null);
        }

        [Test]
        public void MCTSWithSoftPruningTests_Run_NullInitalState()
        {
            MCTSWithSoftPruning agent = new MCTSWithSoftPruning(new Model(), "DefaultSettings.xml");
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
        public void MCTSWithSoftPruningTests_Run_NoChildren()
        {
            MCTSWithSoftPruning agent = new MCTSWithSoftPruning(new Model(), "DefaultSettings.xml");
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
        public void MCTSWithSoftPruningTests_Run_Children()
        {
            MCTSWithSoftPruning agent = new MCTSWithSoftPruning(new Model(), "DefaultSettings.xml");
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