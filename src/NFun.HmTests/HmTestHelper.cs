using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public static class HmTestHelper
    {
        public static void AssertSuccesfully(this SetTypeResult result)
        {
            Assert.AreEqual(SetTypeResult.Succesfully, result, $"Type op failed. {result.Error} at node {result.FailedNodeId}");
        }
    }
}