using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGenerator_Swaroop_Project
{
    public class RequestsManager
    {
        private readonly CancellationTokenSource CancellationSource = new CancellationTokenSource();
        private readonly ProgramService Service;

        public ConcurrentDictionary<HttpStatusCode, int> CompletedRequests = new ConcurrentDictionary<HttpStatusCode, int>();
        public List<Task> RequestTasks = new List<Task>();

        public int TotalRequestsCancelled = 0;
        public int TotalRequestsFaulted = 0;
        public int LastCompletedRequestsCount = 0;

        public RequestsManager(ProgramService service)
        {
            Service = service;
        }

        public int TotalActiveRequests()
        {
            return RequestTasks.Count;
        }

        public void CleanupRequestTasks()
        {
            RequestTasks = RequestTasks.FindAll((task) => {
                if (task == null)
                {
                    return false;
                }

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
                        HttpResponseMessage response = await Service.GetTestUrlAsync();

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
}
