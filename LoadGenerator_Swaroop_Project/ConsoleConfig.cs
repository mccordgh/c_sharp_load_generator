using System.Threading;

namespace LoadGenerator_Swaroop_Project
{
    public class ConsoleConfig
    {
        public int MaxOutstandingRequests;
        public int TransactionsPerSecond;
        public int TransactionsPerBatch;
        public int DesiredTransactionsPerSecond;
        public int BatchesPerSecond;
        public int RestoreTransactionsByAmount;

        public ConsoleConfig(int batchesPerSecond = 20, int desiredTransactionsPerSecond = 200, int maxOutstandingRequests = 1000)
        {
            BatchesPerSecond = batchesPerSecond;
            DesiredTransactionsPerSecond = desiredTransactionsPerSecond;
            TransactionsPerSecond = DesiredTransactionsPerSecond;
            RestoreTransactionsByAmount = (int)(DesiredTransactionsPerSecond * 0.10);
            MaxOutstandingRequests = maxOutstandingRequests;

            UpdateTransactionsPerBatch();
        }

        public void ThrottleTransactionsPerSecond(double amount)
        {
            TransactionsPerSecond = (int)(TransactionsPerSecond * amount);
        }

        public void UpdateTransactionsPerBatch()
        {
            TransactionsPerBatch = TransactionsPerSecond / BatchesPerSecond;
        }
    }
}
