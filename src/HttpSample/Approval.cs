using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace HttpSample
{
    using System;
    using System.Threading;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    public static class Approval_v2
    {
        [FunctionName("Approval_2_0_0")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("Approval_Hello_2_0_0", "Getting here"));

            using (var timeoutCts = new CancellationTokenSource())
            {
                var dueTime = context.CurrentUtcDateTime.Add(TimeSpan.FromMinutes(5.00));
                var approvalEvent = context.WaitForExternalEvent<bool>("ApprovalEvent");
                var durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);
                var response = await Task.WhenAny(approvalEvent, durableTimeout);

                outputs.Add(await context.CallActivityAsync<string>("Approval_OutputMessage_2_0_0", approvalEvent.Result));
            }

            return outputs;
        }

        [FunctionName("Approval_Hello_2_0_0")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Approval_OutputMessage_2_0_0")]
        public static string OutputMessage([ActivityTrigger] string msg, ILogger log)
        {
            log.LogInformation($"{msg}.");
            return $"{msg}!";
        }

        [FunctionName("Approval_HttpStart_2_0_0")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Approval_2_0_0", null);


            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("RaiseApprovalEventClient_2_0_0")]
        public static async Task<IActionResult> RaiseApprovalEventClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "approval/v2/{eventName}/{instanceId}")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableClient,
            string eventName,
            string instanceId,
            ILogger log)
        {
            var approval = await req.ReadAsStringAsync();
            await durableClient.RaiseEventAsync(instanceId, eventName, System.Convert.ToBoolean(approval));
            return new AcceptedResult();
        }
    }


}