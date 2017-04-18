using System.Collections.Generic;
using Monte;
namespace UnitTests
{
    //Abstract base class for the AI state
    //This is for the Client to implement
    public class TestAIState : AIState
    {
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
        }

        public override List<AIState> generateChildren()
        {
            return new List<AIState>();
        }

        public override int getWinner ()
        {
            return 0;
        }
    }
}
