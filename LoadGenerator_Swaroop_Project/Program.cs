using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGenerator_Swaroop_Project
{
    class Program
    {
        class ProgramClient
        {
            private HttpClient Client = new HttpClient();
            private readonly string TestUrl = "https://devswarosh-bcdr-20210209.azureedge.net/test.txt";

            public async Task<HttpResponseMessage> GetTestUrlAsync()
            {
                return await Client.GetAsync(TestUrl);
            }
        }

        class RequestsManager
        {
            private readonly CancellationTokenSource CancellationSource = new CancellationTokenSource();
            private readonly ProgramClient programClient;

            public ConcurrentDictionary<HttpStatusCode, int> CompletedRequests = new ConcurrentDictionary<HttpStatusCode, int>();
            public List<Task> RequestTasks = new List<Task>();

            public int TotalRequestsCancelled = 0;
            public int TotalRequestsFaulted = 0;
            public int LastCompletedRequestsCount = 0;

            public RequestsManager(ProgramClient client)
            {
                programClient = client;
            }

            public int TotalActiveRequests()
            {
                return RequestTasks.Count;
            }

            public void CleanupRequestTasks()
            {
                RequestTasks = RequestTasks.FindAll((task) => {
                     if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Faulted || task.Status == TaskStatus.Canceled)
                     {
                         task.Dispose();
                         return false;
                     }

                     return true;
                 });
            }

            public void CancelAllRequests()
            {
                CancellationSource.Cancel();
            }

            public void EnqueueNewRequest()
            {
                RequestTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            HttpResponseMessage response = await programClient.GetTestUrlAsync();

                            if (response.StatusCode != 0)
                            {
                                CompletedRequests.AddOrUpdate(response.StatusCode, 1, (key, oldValue) => oldValue + 1);
                            }
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.Timeout)
                            {
                                TotalRequestsCancelled += 1;
                            }
                            else
                            {
                                TotalRequestsFaulted += 1; // assuming everything else is categorized as "Faulted"
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }, CancellationSource.Token)
                );
            }
        }

        class ConsoleConfig
        {
            public int MaxOutstandingRequests;
            public int TransactionsPerSecond;
            public int TransactionsPerBatch;
            public int DesiredTransactionsPerSecond;
            public int BatchesPerSecond;
            public int RestoreTransactionsByAmount;

            private Timer OutputTimer;

            public ConsoleConfig(int batchesPerSecond, int desiredTransactionsPerSecond, int maxOutstandingRequests, Timer outputTimer)
            {
                BatchesPerSecond = batchesPerSecond;
                DesiredTransactionsPerSecond = desiredTransactionsPerSecond;
                TransactionsPerSecond = DesiredTransactionsPerSecond;
                RestoreTransactionsByAmount = (int)(DesiredTransactionsPerSecond * 0.05); // 5 % at a time
                MaxOutstandingRequests = maxOutstandingRequests;
                OutputTimer = outputTimer;

                UpdateTransactionsPerBatch();
            }

            public void ThrottleTransactionsPerSecond(double amount)
            {
                TransactionsPerSecond = (int)(TransactionsPerSecond * amount);
            }

            public void DisposeOutputTimer()
            {
                OutputTimer.Dispose();
            }

            public void UpdateTransactionsPerBatch()
            {
                TransactionsPerBatch = TransactionsPerSecond / BatchesPerSecond;
            }
        }

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

        public static readonly int OneSecond = 1000;

        static RequestsManager requestsManager;
        static ConsoleConfig config;

        static void Main()
        {
            requestsManager = new RequestsManager(new ProgramClient());
            config = new ConsoleConfig(20, 50, 1000, InitConsoleOutputTimer(500));

            RunGeneratorLoop();
        }

        static Timer InitConsoleOutputTimer(int outputFrequency)
        {
            Console.CursorVisible = false;
            Console.Clear();

            // Keep reference so timer doesnt get garbage collected
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

            //if (LastCompletedRequestsCount != CompletedRequests.Keys.Count)
            //{
            //    LastCompletedRequestsCount = CompletedRequests.Keys.Count;
            //    Console.Clear();
            //}

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
            config.DisposeOutputTimer();

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
