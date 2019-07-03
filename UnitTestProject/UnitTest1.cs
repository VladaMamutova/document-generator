using DocumentGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SplitIntoTwoStringsTest1()
        {
            // arrange
            string text = "א א";
            string part1 = "א";
            string part2 = "א";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoTwoStringsTest2()
        {
            // arrange
            string text = "א אגûאבü‏גûבüא";
            string part1 = "א";
            string part2 = "אגûאבü‏גûבüא";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoTwoStringsTest3()
        {
            // arrange
            string text = "אגûאבü‏גûבüא afs";
            string part1 = "אגûאבü‏גûבüא";
            string part2 = "afs";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoTwoStringsTest4()
        {
            // arrange
            string text = "12345 1234567 123";
            string part1 = "12345";
            string part2 = "1234567 123";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoTwoStringsTest5()
        {
            // arrange
            string text = "12345 12 12345 123";
            string part1 = "12345 12";
            string part2 = "12345 123";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoTwoStringsTest6()
        {
            // arrange
            string text = "12345 123456 12345 123";
            string part1 = "12345 123456";
            string part2 = "12345 123";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoTwoStringsTest7()
        {
            // arrange
            string text = "12345 1234 12 123456789";
            string part1 = "12345 1234";
            string part2 = "12 123456789";

            // act
            string[] strings = StringHelper.SplitIntoTwoParts(text);

            // assert
            Assert.AreEqual(2, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest1()
        {
            // arrange
            string text = "1 1234 1";
            string part1 = "1";
            string part2 = "1234";
            string part3 = "1";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest2()
        {
            // arrange
            string text = "123 1234 1234";
            string part1 = "123";
            string part2 = "1234";
            string part3 = "1234";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest3()
        {
            // arrange
            string text = "123 12 1234 1234 12";
            string part1 = "123 12";
            string part2 = "1234";
            string part3 = "1234 12";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest4()
        {
            // arrange
            string text = "123 12 1234 123456";
            string part1 = "123";
            string part2 = "12 1234";
            string part3 = "123456";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest5()
        {
            // arrange
            string text = "1 1 123456789012345678901234567890";
            string part1 = "1";
            string part2 = "1";
            string part3 = "123456789012345678901234567890";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest6()
        {
            // arrange
            string text = "12 1 1 123456789012345678901234567890";
            string part1 = "12";
            string part2 = "1 1";
            string part3 = "123456789012345678901234567890";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest7()
        {
            // arrange
            string text = "123 12 12 123456789012345678901234567890";
            string part1 = "123";
            string part2 = "12 12";
            string part3 = "123456789012345678901234567890";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }

        [TestMethod]
        public void SplitIntoThreeStringsTest8()
        {
            // arrange
            string text = "123456789012345678901234567890 123 12 1234";
            string part1 = "123456789012345678901234567890";
            string part2 = "123 12";
            string part3 = "1234";

            // act
            string[] strings = StringHelper.SplitIntoThreeParts(text);

            // assert
            Assert.AreEqual(3, strings.Length);
            Assert.AreEqual(part1, strings[0]);
            Assert.AreEqual(part2, strings[1]);
            Assert.AreEqual(part3, strings[2]);
        }
    }
}
