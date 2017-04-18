using NUnit.Framework;
using Monte;

namespace UnitTests
{
    [TestFixture]
    public class RandomAgentTests
    {
        [Test]
        public void RandomAgentTests_Run_NullInitalState()
        {
            AIAgent randAgent = new RandomAgent();
            randAgent.run(null);
            bool aiDone = false;
            while (!aiDone)
            {
                if (randAgent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = randAgent.next;
            Assert.True(next == null);
        }

        [Test]
        public void RandomAgentTests_Run_NoChildren()
        {
            AIAgent randAgent = new RandomAgent();
            randAgent.run(new TestAIState("empty"));
            bool aiDone = false;
            while (!aiDone)
            {
                if (randAgent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = randAgent.next;
            Assert.True(next == null);
        }

        [Test]
        public void RandomAgentTests_Run_Children()
        {
            AIAgent randAgent = new RandomAgent();
            randAgent.run(new TestAIState("children"));
            bool aiDone = false;
            while (!aiDone)
            {
                if (randAgent.done)
                {
                    aiDone = true;
                }
            }
            AIState next = randAgent.next;
            Assert.True(next != null);
        }
    }
}