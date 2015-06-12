namespace BuildStatusTests
{
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompareBuildStatus
    {
        [TestMethod]
        public void Failed_GreaterThan_Succeeded()
        {
            Assert.IsTrue(BuildStatus.Failed > BuildStatus.Succeeded);
        }

        [TestMethod]
        public void Failed_GreaterThan_PartiallySucceeded()
        {
            Assert.IsTrue(BuildStatus.Failed > BuildStatus.PartiallySucceeded);
        }

        [TestMethod]
        public void PartiallySucceeded_GreaterThan_Succeeded()
        {
            Assert.IsTrue(BuildStatus.PartiallySucceeded > BuildStatus.Succeeded);
        }

        [TestMethod]
        public void Succeeded_GreaterThan_None()
        {
            Assert.IsTrue(BuildStatus.Succeeded > BuildStatus.None);
        }
    }
}
