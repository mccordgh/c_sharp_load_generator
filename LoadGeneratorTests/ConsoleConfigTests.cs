using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoadGenerator_Swaroop_Project;

namespace LoadGeneratorTests
{
    [TestClass]
    public class ConsoleConfigTests
    {
        [TestMethod]
        public void Test_ItInitializesCorrectly()
        {
            int batchesPerSecond = 20;
            int desiredTransactionsPerSecond = 200;
            int maxOutstandingRequests = 1000;

            ConsoleConfig config = new ConsoleConfig(batchesPerSecond, desiredTransactionsPerSecond, maxOutstandingRequests);

            Assert.AreEqual(20, config.BatchesPerSecond);
            Assert.AreEqual(200, config.DesiredTransactionsPerSecond);
            Assert.AreEqual(1000, config.MaxOutstandingRequests);
            Assert.AreEqual(config.DesiredTransactionsPerSecond, config.TransactionsPerSecond);
        }

        [DataRow(10, 20, 30, 0.1, 2)]
        [DataRow(100, 200, 300, 0.5, 100)]
        [DataRow(500, 1000, 2000, 0.9, 900)]
        [DataTestMethod]
        public void Test_ThrottleTransactionsPerSecond(int batchesPerSecond, int desiredTransactionsPerSecond, int maxOutstandingRequests, double throttleAmount, int expectedTransactionsPerSecond)
        {
            ConsoleConfig config = new ConsoleConfig(batchesPerSecond, desiredTransactionsPerSecond, maxOutstandingRequests);

            config.ThrottleTransactionsPerSecond(throttleAmount);

            Assert.AreEqual(expectedTransactionsPerSecond, config.TransactionsPerSecond);
        }

        [DataRow(20, 200, 1000, 10)]
        [DataRow(30, 500, 2000, 16)]
        [DataRow(40, 2000, 4500, 50)]
        [DataTestMethod]
        public void Test_UpdateTransactionsPerBatch(int batchesPerSecond, int desiredTransactionsPerSecond, int maxOutstandingRequests, int expectedTransactionsPerBatch)
        {
            ConsoleConfig config = new ConsoleConfig(batchesPerSecond, desiredTransactionsPerSecond, maxOutstandingRequests);

            config.UpdateTransactionsPerBatch();

            Assert.AreEqual(expectedTransactionsPerBatch, config.TransactionsPerBatch);
        }
    }
}
