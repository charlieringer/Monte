using System.Collections.Generic;
using System;
using System.Diagnostics;
using NUnit.Framework;
using Monte;

namespace UnitTests
{
    [TestFixture]
    public class AIStateTests
    {
        [Test]
        public void TestAIState_MergeSortPreSorted()
        {
            List<AIState> list = new List<AIState>();
            for (int i = 0; i < 10; i++)
            {
                TestAIState test = new TestAIState("empty");
                test.stateScore = i;
                list.Add(test);
            }

            List<AIState> sortedList = AIState.mergeSort(list);
            bool sorted = true;
            double last = -1;
            foreach (AIState state in sortedList)
            {
                if (!(state.stateScore.Value >= last))
                {
                    sorted = false;
                    break;
                }
                last = state.stateScore.Value;

            }
            Assert.True(sorted);
        }

        [Test]
        public void TestAIState_MergeSortReverseSorted()
        {
            List<AIState> list = new List<AIState>();
            for (int i = 9; i >= 0; i--)
            {
                TestAIState test = new TestAIState("empty");
                test.stateScore = i;
                list.Add(test);
            }

            List<AIState> sortedList = AIState.mergeSort(list);
            bool sorted = true;
            double last = -1;
            foreach (AIState state in sortedList)
            {
                if (!(state.stateScore.Value >= last))
                {
                    sorted = false;
                    break;
                }
                last = state.stateScore.Value;

            }
            Assert.True(sorted);
        }

        [Test]
        public void TestAIState_MergeSortRandom()
        {
            Random randGen = new Random ();
            List<AIState> list = new List<AIState>();
            for (int i = 0; i < 10; i++)
            {
                TestAIState test = new TestAIState("empty");
                test.stateScore = randGen.NextDouble();
                list.Add(test);
            }

            List<AIState> sortedList = AIState.mergeSort(list);
            bool sorted = true;
            double last = -1;
            foreach (AIState state in sortedList)
            {
                if (!(state.stateScore.Value >= last))
                {
                    Console.WriteLine("SS: " + state.stateScore.Value + " Last " + last);
                    sorted = false;
                    break;
                }
                last = state.stateScore.Value;

            }
            Assert.True(sorted);
        }

        [Test]
        public void TestAIState_Win()
        {
            AIState head = new TestAIState("empty");
            AIState tail1 = new TestAIState("empty");
            tail1.parent = head;
            AIState tail2 = new TestAIState("empty");
            tail2.parent = tail1;
            AIState tail3 = new TestAIState("empty");
            tail3.parent = tail2;
            tail3.addWin();
            Assert.True(tail3.wins == 1 && tail3.losses == 0);
            Assert.True(tail2.wins == 0 && tail2.losses == 1);
            Assert.True(tail1.wins == 1 && tail1.losses == 0);
            Assert.True(head.wins == 0 && head.losses == 1);
        }

        [Test]
        public void TestAIState_Loss()
        {
            AIState head = new TestAIState("empty");
            AIState tail1 = new TestAIState("empty");
            tail1.parent = head;
            AIState tail2 = new TestAIState("empty");
            tail2.parent = tail1;
            AIState tail3 = new TestAIState("empty");
            tail3.parent = tail2;
            tail3.addLoss();
            Assert.True(tail3.wins == 0 && tail3.losses == 1);
            Assert.True(tail2.wins == 1 && tail2.losses == 0);
            Assert.True(tail1.wins == 0 && tail1.losses == 1);
            Assert.True(head.wins == 1 && head.losses == 0);
        }

        [Test]
        public void TestAIState_Draw()
        {
            AIState head = new TestAIState("empty");
            AIState tail1 = new TestAIState("empty");
            tail1.parent = head;
            AIState tail2 = new TestAIState("empty");
            tail2.parent = tail1;
            AIState tail3 = new TestAIState("empty");
            tail3.parent = tail2;
            tail3.addDraw(0.5);
            Assert.True(tail3.wins == 0.5 && tail3.losses == 0);
            Assert.True(tail2.wins == 0.5 && tail2.losses == 0);
            Assert.True(tail1.wins == 0.5 && tail1.losses == 0);
            Assert.True(head.wins == 0.5 && head.losses == 0);
        }
    }
}