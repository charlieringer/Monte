using System;
using System.Collections.Generic;
using Monte;
namespace UnitTests
{
    //Test AIState
    public class TestAIState : AIState
    {
        bool hasChidren = false;
        public TestAIState(string testType)
        {
            if (testType.Contains("empty"))
            {
                return;
            }
            if (testType.Contains("no_numbPieceTypes"))
            {
                stateRep = new int[10];
                return;
            }
            if (testType.Contains("no_stateRep"))
            {
                numbPieceTypes = 1;
                return;
            }
            if (testType.Contains("full_state"))
            {
                stateRep = new int[10];
                numbPieceTypes = 1;
            }
            if (testType.Contains("children"))
            {
                stateRep = new int[10];
                numbPieceTypes = 1;
                hasChidren = true;
            }
        }

        public override List<AIState> generateChildren()
        {
            List<AIState> children = new List<AIState>();
            if (hasChidren)
            {
                for (int i = 0; i < 10; i++)
                {
                    Random gen = new Random();
                    AIState child = new TestAIState("empty");
                    child.stateScore = gen.NextDouble();
                    children.Add(child);
                }
            }
            this.children = children;
            return children;
        }

        public override int getWinner ()
        {
            return 0;
        }
    }
}
