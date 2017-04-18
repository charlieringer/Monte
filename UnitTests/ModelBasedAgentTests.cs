using NUnit.Framework;
using Monte;

using NUnit.Framework;
using Monte;

namespace UnitTests
{
    public class ModelBasedAgentTests
    {
        [Test]
        public void ModelBasedAgentTests_Init_NoModel()
        {
            ModelBasedAgent mbAgent = new ModelBasedAgent(null);
            Assert.True(mbAgent != null);
            Assert.True(mbAgent.model != null);
        }

        [Test]
        public void ModelBasedAgentTests_Run_NullInitalState()
        {
            AIAgent mbAgent = new ModelBasedAgent(new Model());
            mbAgent.run(null);
            bool aiDone = false;
            while (!aiDone)
            {
                if (mbAgent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = mbAgent.next;
            Assert.True(next == null);
        }

        [Test]
        public void ModelBasedAgentTests_Run_NoChildren()
        {
            AIAgent mbAgent = new ModelBasedAgent(new Model());
            mbAgent.run(new TestAIState("empty"));
            bool aiDone = false;
            while (!aiDone)
            {
                if (mbAgent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = mbAgent.next;
            Assert.True(next == null);
        }

        [Test]
        public void ModelBasedAgentTests_Run_Children()
        {
            AIAgent mbAgent = new ModelBasedAgent(new Model());
            mbAgent.run(new TestAIState("children"));
            bool aiDone = false;
            while (!aiDone)
            {
                if (mbAgent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = mbAgent.next;
            Assert.True(next != null);
        }

    }
}