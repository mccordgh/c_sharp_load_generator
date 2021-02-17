using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoadGenerator_Swaroop_Project;

namespace LoadGeneratorTests
{
    [TestClass]
    public class ConsoleConfigTests
    {
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

        [DataRow(200, 20, 10)]
        [DataRow(500, 30, 16)]
        [DataRow(2000, 40, 50)]
        [DataTestMethod]
        public void Test_UpdateTransactionsPerBatch(int transactionsPerSecond, int batchesPerSecond, int expectedTransactionsPerBatch)
        {
            ConsoleConfig config = new ConsoleConfig
            {
                TransactionsPerSecond = transactionsPerSecond,
                BatchesPerSecond = batchesPerSecond
            };

            config.UpdateTransactionsPerBatch();

            Assert.AreEqual(expectedTransactionsPerBatch, config.TransactionsPerBatch);
        }
    }
}
