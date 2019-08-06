using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests
{
    public static class HmTestHelper
    {
        public static void AssertSuccesfully(this SetTypeResult result)
        {
            Assert.AreEqual(SetTypeResult.Succesfully, result, $"Type op failed. {result.Error} at node {result.FailedNodeId}");
        }
        public static void AssertFailed(this SetTypeResult result)
        {
            Assert.IsFalse(result.IsSuccesfully,$"Type op is not failed.");
        }
        public static void AssertFailed(this SetTypeResult result, int node)
        {
            Assert.IsFalse(result.IsSuccesfully,$"Type op is not failed.");
            Assert.AreEqual(node,result.FailedNodeId, $"wrong failed node id");
        }
    }
}