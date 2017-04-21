using NUnit.Framework;
using Monte;

namespace UnitTests
{
    public class MCTSWithLearningTests
    {
        [Test]
        public void MCTSWithLearningTests_Init_NullModel()
        {
            MCTSWithLearning agent = new MCTSWithLearning(null);
            Assert.True(agent != null);
            Assert.True(agent.model != null);
        }

        [Test]
        public void MCTSWithLearningTests_Init_NullModel_Settings()
        {
            MCTSWithLearning agent = new MCTSWithLearning(null,"");
            Assert.True(agent != null);
            Assert.True(agent.model != null);
        }

        [Test]
        public void MCTSWithLearningTests_Run_NullInitalState()
        {
            MCTSWithLearning agent = new MCTSWithLearning(new Model(), "DefaultSettings.xml");
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
        public void MCTSWithLearningTests_Run_NoChildren()
        {
            MCTSWithLearning agent = new MCTSWithLearning(new Model(), "DefaultSettings.xml");
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
        public void MCTSWithLearningTests_Run_Children()
        {
            MCTSWithLearning agent = new MCTSWithLearning(new Model(), "DefaultSettings.xml");
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