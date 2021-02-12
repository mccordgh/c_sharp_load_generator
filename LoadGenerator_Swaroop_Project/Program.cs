using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGenerator_Swaroop_Project
{
    class Program
    {
        static HttpClient client = new HttpClient();

        static string TestUrl = "https://devswarosh-bcdr-20210209.azureedge.net/test.txt";

        static int ConsoleOutPutFrequency;
        static int MaxOutstandingRequests;
        static int TransactionsPerSecond;
        static int TransactionsPerBatch;

        static int OneSecond = 1000;
        static int TotalRequestsCancelled = 0;
        static int TotalRequestsFaulted = 0;

        static Timer OutputTimer;

        static Dictionary<HttpStatusCode, int> CompletedRequests = new Dictionary<HttpStatusCode, int>();
        static List<Task> RequestTasks = new List<Task>();

        static void Main(string[] args)
        {
            InitRequestOptions();
            InitConsoleOutput();
            LoadGeneratorLoop();
        }

        static void InitRequestOptions()
        {
            // TODO: Set these values via command line args?
            TransactionsPerSecond = 999;
            MaxOutstandingRequests = 1000;
            ConsoleOutPutFrequency = 500;
            TransactionsPerBatch = 5;
        }

        static void InitConsoleOutput()
        {
            Console.CursorVisible = false;
            Console.Clear();
            // Keep reference so it doesnt get disposed
            OutputTimer = new Timer(_ => UpdateConsoleOutput(), null, 0, ConsoleOutPutFrequency);
        }

        static List<string> BuildOutputForEachRequestStatus(out int totalRequestsCompleted)
        {
            List<string> outputForEachRequestStatus = new List<string>();
            List<HttpStatusCode> completedRequestKeys = new List<HttpStatusCode>(CompletedRequests.Keys);

            totalRequestsCompleted = 0;

            foreach (HttpStatusCode key in completedRequestKeys)
            {
                int value = CompletedRequests[key];

                int spaceLength = 13;
                string spaces = new string(' ', spaceLength - key.ToString().Length);

                outputForEachRequestStatus.Add(string.Format("    Status Code {0}{1}: {2}{3}", key, spaces, GetSpacerString(value), value));
                totalRequestsCompleted += value;
            }

            return outputForEachRequestStatus;
        }

        static string GetSpacerString(int number)
        {
            int maxNumberLength = 4;
            return new string(' ', maxNumberLength - number.ToString().Length);
        }

        static void UpdateConsoleOutput()
        {
            RequestTasks = RequestTasks.FindAll(t => t.Status != TaskStatus.RanToCompletion);

            int totalRequestsCompleted;
            List<string> outputForEachRequestStatus = BuildOutputForEachRequestStatus(out totalRequestsCompleted);

            int tasksCount = RequestTasks.Count;
            int totalRequestsCreated = tasksCount + TotalRequestsFaulted + TotalRequestsCancelled + totalRequestsCompleted;

            Console.SetCursorPosition(0, 0);

            Console.WriteLine(string.Format("Created                                 : {0}{1}", GetSpacerString(totalRequestsCreated), totalRequestsCreated));
            Console.WriteLine(string.Format("  Completed                  : {0}{1}", GetSpacerString(totalRequestsCompleted), totalRequestsCompleted));
            Console.WriteLine(string.Format("    Faulted                  : {0}{1}", GetSpacerString(TotalRequestsFaulted), TotalRequestsFaulted));
            Console.WriteLine(string.Format("    Cancelled                : {0}{1}", GetSpacerString(TotalRequestsCancelled), TotalRequestsCancelled));

            foreach (string output in outputForEachRequestStatus)
            {
                Console.WriteLine(output);
            }

            Console.WriteLine(string.Format("  Active Requests                       : {0}{1}", GetSpacerString(tasksCount), tasksCount));
        }

        static void AddRequestToCompletedRequests(HttpStatusCode code)
        {
            if (CompletedRequests.ContainsKey(code))
            {
                CompletedRequests[code] = CompletedRequests[code] + 1;
            }
            else
            {
                CompletedRequests.Add(code, 1);
            }
        }

        static void FireAndForgetRequest()
        {
            RequestTasks.Add(
                Task.Run(async () =>
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(TestUrl);

                        if (response.StatusCode != 0)
                        {
                            AddRequestToCompletedRequests(response.StatusCode);
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.Timeout)
                        {
                            TotalRequestsCancelled += 1;
                        }
                        else // assuming everything else is categorized as "Faulted"
                        {
                            TotalRequestsFaulted += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                })
            );
        }

        static void CleanUp(string msg)
        {
            Console.CursorVisible = true;

            OutputTimer.Dispose();

            Console.WriteLine($"\n\n{msg}");
            Console.WriteLine("\nPress any key to continue.");
            Console.ReadKey();
        }

        static void LoadGeneratorLoop()
        {
            double waitTime = (OneSecond / TransactionsPerSecond) * TransactionsPerBatch;

            bool running = true;

            while (running)
            {
                if ((RequestTasks.Count + TransactionsPerBatch) > MaxOutstandingRequests)
                {
                    CleanUp($"Terminating Load Generator. Max outstanding requests exceeded ({MaxOutstandingRequests}).");
                    break;
                }

                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    CleanUp("Load Generator ended by user pressing Escape Key.");
                    break;
                }

                for (int i = 0; i < TransactionsPerBatch; i += 1)
                {
                    FireAndForgetRequest();
                }

                Thread.Sleep((int)waitTime);
            }
        }
    }
}
