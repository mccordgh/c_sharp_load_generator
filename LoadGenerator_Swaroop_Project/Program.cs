using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace LoadGenerator_Swaroop_Project
{
    public class Program
    {
        class Constants
        {
            public static readonly string RequestsCreatedFormat =    "Created                                 : {0}{1}";
            public static readonly string RequestsCompletedFormat =  "  Completed                  : {0}{1}";
            public static readonly string RequestsFaultedFormat =    "    Faulted                  : {0}{1}";
            public static readonly string RequestsStatusFormat =     "    Status Code {0}{1}: {2}{3}";
            public static readonly string RequestsCancelledFormat =  "    Cancelled                : {0}{1}";
            public static readonly string RequestsActiveFormat =     "  Active Requests                       : {0}{1}";

            public static readonly int RequestsStatusSpacer = 13;
            public static readonly int SpacerStringMinLength = 4;
        }

        static readonly int OneSecond = 1000;
        static Timer OutputTimer;

        static RequestsManager requestsManager;
        static ConsoleConfig config;

        static void Main()
        {
            requestsManager = new RequestsManager(new ProgramClient("https://devswarosh-bcdr-20210209.azureedge.net/test.txt"));
            config = new ConsoleConfig(20, 50, 1000);

            // Keep reference so timer doesnt get garbage collected
            OutputTimer = InitConsoleOutputTimer(500);

            RunGeneratorLoop();
        }

        static Timer InitConsoleOutputTimer(int outputFrequency)
        {
            Console.CursorVisible = false;
            Console.Clear();

            return new Timer(_ => UpdateConsoleOutput(), null, 0, outputFrequency);
        }

        static List<string> BuildOutputForEachRequestStatus(out int totalRequestsCompleted)
        {
            List<string> outputForEachRequestStatus = new List<string>();

            totalRequestsCompleted = 0;

            foreach (KeyValuePair<HttpStatusCode, int> request in requestsManager.CompletedRequests)
            {
                string key = request.Key.ToString();
                int value = request.Value;

                string spaces = new string(' ', Constants.RequestsStatusSpacer - key.Length);

                outputForEachRequestStatus.Add(string.Format(Constants.RequestsStatusFormat, key, spaces, GetSpacerString(value), value));
                totalRequestsCompleted += value;
            }

            return outputForEachRequestStatus;
        }

        static string GetSpacerString(int number)
        {
            int spacerLength = Constants.SpacerStringMinLength - number.ToString().Length;

            if (spacerLength <= 0)
            {
                return string.Empty;
            }

            return new string(' ', spacerLength);
        }

        static void UpdateConsoleOutput()
        {
            requestsManager.CleanupRequestTasks();

            int totalRequestsCompleted;
            List<string> outputForEachRequestStatus = BuildOutputForEachRequestStatus(out totalRequestsCompleted);

            int tasksCount = requestsManager.TotalActiveRequests();
            int totalRequestsCreated = tasksCount + requestsManager.TotalRequestsFaulted + requestsManager.TotalRequestsCancelled + totalRequestsCompleted;

            Console.SetCursorPosition(0, 0);

            Console.WriteLine(string.Format(Constants.RequestsCreatedFormat, GetSpacerString(totalRequestsCreated), totalRequestsCreated));
            Console.WriteLine(string.Format(Constants.RequestsCompletedFormat, GetSpacerString(totalRequestsCompleted), totalRequestsCompleted));
            Console.WriteLine(string.Format(Constants.RequestsFaultedFormat, GetSpacerString(requestsManager.TotalRequestsFaulted), requestsManager.TotalRequestsFaulted));
            Console.WriteLine(string.Format(Constants.RequestsCancelledFormat, GetSpacerString(requestsManager.TotalRequestsCancelled), requestsManager.TotalRequestsCancelled));

            foreach (string output in outputForEachRequestStatus)
            {
                Console.WriteLine(output);
            }

            Console.WriteLine(string.Format(Constants.RequestsActiveFormat, GetSpacerString(tasksCount), tasksCount));
        }

        static void FinalCleanUp()
        {
            requestsManager.CancelAllRequests();
            OutputTimer.Dispose();

            Console.CursorVisible = true;
            Console.WriteLine("\nPress any key to continue.");
            Console.ReadKey();
        }

        static void RunGeneratorLoop()
        {
            double waitTime = (OneSecond / config.TransactionsPerSecond) * config.TransactionsPerBatch;

            while (true)
            {
                // speed back up a little if we seem to have stabilized a bit
                if (config.TransactionsPerSecond < config.DesiredTransactionsPerSecond && requestsManager.TotalActiveRequests() <= config.MaxOutstandingRequests)
                {
                    if (config.TransactionsPerSecond +  config.RestoreTransactionsByAmount > config.DesiredTransactionsPerSecond)
                    {
                        config.TransactionsPerSecond = config.DesiredTransactionsPerSecond;
                    }
                    else
                    {
                        config.TransactionsPerSecond += config.RestoreTransactionsByAmount;
                    }
                    config.UpdateTransactionsPerBatch();
                }

                if ((requestsManager.TotalActiveRequests() + config.TransactionsPerBatch) > config.MaxOutstandingRequests)
                {
                    config.ThrottleTransactionsPerSecond(0.95);
                    config.UpdateTransactionsPerBatch();
                }

                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    FinalCleanUp();
                    break;
                }

                for (int i = 0; i < config.TransactionsPerBatch; i += 1)
                {
                    requestsManager.EnqueueNewRequest();
                }

                Thread.Sleep((int)waitTime);
            }
        }
    }
}
