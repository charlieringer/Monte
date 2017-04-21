using NUnit.Framework;
using Monte;

namespace UnitTests
{
    public class MCTSWithPruningTests
    {
        [Test]
        public void MCTSWithPruningTests_Init_NullModel()
        {
            MCTSWithPruning agent = new MCTSWithPruning(null);
            Assert.True(agent != null);
            Assert.True(agent.model != null);
        }

        [Test]
        public void MCTSWithPruningTests_Init_NullModel_Settings()
        {
            MCTSWithPruning agent = new MCTSWithPruning(null,"");
            Assert.True(agent != null);
            Assert.True(agent.model != null);
        }

        [Test]
        public void MCTSWithPruningTests_Run_NullInitalState()
        {
            MCTSWithPruning agent = new MCTSWithPruning(new Model(), "DefaultSettings.xml");
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
        public void MCTSWithPruningTests_Run_NoChildren()
        {
            MCTSWithPruning agent = new MCTSWithPruning(new Model(), "DefaultSettings.xml");
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
        public void MCTSWithPruningTests_Run_Children()
        {
            MCTSWithPruning agent = new MCTSWithPruning(new Model(), "DefaultSettings.xml");
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