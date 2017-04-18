using NUnit.Framework;
using Monte;

namespace UnitTests
{
    [TestFixture]
    public class ModelTests
    {

        [Test]
        public void TestModel_Init_Blank()
        {
            Model model = new Model();
            Assert.False(model == null);
        }

        [Test]
        public void TestModel_Init_XMLOnly()
        {
            Model model = new Model("DefaultSettings.xml");
            Assert.False(model == null);
        }

        [Test]
        public void TestModel_Init_ModelOnly()
        {
            Model model = new Model("Test.model");
            Assert.False(model == null);
        }

        [Test]
        public void TestModel_Init_BothFiles()
        {
            Model model = new Model("DefaultSettings.xml, Test.model");
            Assert.False(model == null);
        }

        [Test]
        public void TestModel_Init_WrongFileType()
        {
            Model model = new Model("Test.txt");
            Assert.False(model == null);
        }

        [Test]
        public void TestModel_Init_FileNotfound()
        {
            Model model = new Model("");
            Assert.False(model == null);
        }

        [Test]
        public void TestModel_Train_NullSC()
        {
            Model model = new Model("");
            int result = model.train(1, 1, null);
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_NegGPE()
        {
            Model model = new Model("");
            int result = model.train(-1, 1, null);
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_NegEpisodes()
        {
            Model model = new Model("");
            int result = model.train(1, -1, null);
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_0GPE()
        {
            Model model = new Model("");
            int result = model.train(0, 1, null);
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_0Episodes()
        {
            Model model = new Model("");
            int result = model.train(1, 0, null);
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_TestState_Empty()
        {
            Model model = new Model("");
            int result = model.train(1, 1, ()=>new TestAIState("empty"));
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_TestState_NoRep()
        {
            Model model = new Model("");
            int result = model.train(1, 1, ()=>new TestAIState("no_stateRep"));
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_TestState_NoNumbPieces()
        {
            Model model = new Model("");
            int result = model.train(1, 1, ()=>new TestAIState("no_numbPieceTypes"));
            Assert.True(result == -1);
        }

        [Test]
        public void TestModel_Train_TestState_Working()
        {
            Model model = new Model("");
            int result = model.train(1, 1, ()=>new TestAIState("full_state"));
            Assert.True(result == 1);
        }

        [Test]
        public void Test_Math_Sig0()
        {
            double result = Model.sig(0);
            Assert.True(result == 0.5);
        }

        [Test]
        public void Test_Math_Sig10()
        {
            double result = Model.sig(10);
            Assert.True(result >= 0.9);
        }

        [Test]
        public void Test_Math_SigMinus10()
        {
            double result = Model.sig(-10);
            Assert.True(result <= 0.1);
        }

        [Test]
        public void Test_Math_TanH0()
        {
            double result = Model.tanH(0);
            Assert.True(result == 0);
        }

        [Test]
        public void Test_Math_TanH10()
        {
            double result = Model.tanH(10);
            Assert.True(result >= 0.9);
        }

        [Test]
        public void Test_Math_TanHMinus10()
        {
            double result = Model.tanH(-10);
            Assert.True(result <= -0.9);
        }
    }
}