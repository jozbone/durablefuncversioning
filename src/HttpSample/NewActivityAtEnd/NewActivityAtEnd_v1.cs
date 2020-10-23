using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace HttpSample.NewActivityAtEnd
{
    public static class NewActivityAtEnd_v1
    {
        [FunctionName("NewActivityAtEnd_1_0_0")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("NewActivityAtEnd_Hello_1_0_0", "Getting here"));

            using (var timeoutCts = new CancellationTokenSource())
            {
                var dueTime = context.CurrentUtcDateTime.Add(TimeSpan.FromMinutes(5.00));
                var NewActivityAtEndEvent = context.WaitForExternalEvent<bool>("NewActivityAtEndEvent");
                var durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);
                var response = await Task.WhenAny(NewActivityAtEndEvent, durableTimeout);

                outputs.Add(await context.CallActivityAsync<string>("NewActivityAtEnd_OutputMessage_1_0_0", NewActivityAtEndEvent.Result));
            }

            return outputs;
        }

        [FunctionName("NewActivityAtEnd_Hello_1_0_0")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("NewActivityAtEnd_OutputMessage_1_0_0")]
        public static string OutputMessage([ActivityTrigger] string msg, ILogger log)
        {
            log.LogInformation($"{msg}.");
            return $"{msg}!";
        }

        [FunctionName("NewActivityAtEnd_HttpStart_1_0_0")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("NewActivityAtEnd_1_0_0", null);


            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RaiseNewActivityAtEndEventClient))]
        public static async Task<IActionResult> RaiseNewActivityAtEndEventClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "NewActivityAtEnd/{eventName}/{instanceId}")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableClient,
            string eventName,
            string instanceId,
            ILogger log)
        {
            var NewActivityAtEnd = await req.ReadAsStringAsync();
            await durableClient.RaiseEventAsync(instanceId, eventName, System.Convert.ToBoolean(NewActivityAtEnd));
            return new AcceptedResult();
        }
    }

}
