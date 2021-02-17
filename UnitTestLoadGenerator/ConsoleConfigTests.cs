using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestLoadGenerator
{
    [TestClass]
    public class ConsoleConfigTests
    {
        private ConsoleConfig config = new ConsoleConfig();

        [TestMethod]
        public void Test_ThrottleTransactionsPerSecond(int transactionsPerSecond, int batchesPerSecond)
        {
        }
    }
}
