namespace HttpSample.Breaking
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    public static class SignatureChangeDemo
    {
        [FunctionName("SignatureChangeDemo")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            var helloMessage = await context.CallActivityAsync<string>("SignatureChangeDemo_Hello", "Getting here");
            
            // bool version
            // var helloMessage = await context.CallActivityAsync<bool>("SignatureChangeDemo_Hello", "Getting here");
            outputs.Add(helloMessage.ToString());

            using (var timeoutCts = new CancellationTokenSource())
            {
                var dueTime = context.CurrentUtcDateTime.Add(TimeSpan.FromMinutes(10.00));
                var signatureChangeDemoEvent = context.WaitForExternalEvent<bool>("SignatureChangeDemoEvent");
                var durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);
                var response = await Task.WhenAny(signatureChangeDemoEvent, durableTimeout);

                try
                {
                    // outputs.Add(await context.CallActivityAsync<string>("SignatureChangeDemo_OutputMessage", signatureChangeDemoEvent.Result));
                    outputs.Add(await context.CallActivityAsync<string>("SignatureChangeDemo_OutputMessage", helloMessage));
                }
                catch (Exception ex)
                {
                    outputs.Add(ex.Message);
                }
            }

            return outputs;
        }

        // Original version
        [FunctionName("SignatureChangeDemo_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return "true";
        }

        // Change version 1
        // [FunctionName("SignatureChangeDemo_Hello")]
        // public static bool SayHello([ActivityTrigger] string name, ILogger log)
        // {
        //     log.LogInformation($"Saying hello to {name}.");
        //     return true;
        // }

        // Change version 1
        // [FunctionName("SignatureChangeDemo_OutputMessage")]
        // public static string OutputMessage([ActivityTrigger] string msg, string data, ILogger log)
        // {
        //      log.LogInformation($"{msg}.");
        //      log.LogInformation($"{data}.");
        //      return $"{msg}!";
        // }

        // Change version 2
        [FunctionName("SignatureChangeDemo_OutputMessage")]
        public static bool OutputMessage([ActivityTrigger] bool msg, ILogger log)
        {
             log.LogInformation($"{msg}.");
             return true;
        }

        // Original version
        // [FunctionName("SignatureChangeDemo_OutputMessage")]
        // public static string OutputMessage([ActivityTrigger] string msg, ILogger log)
        // {
        //     log.LogInformation($"{msg}.");
        //     return $"{msg}!";
        // }

        [FunctionName("SignatureChangeDemo_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var instanceId = await starter.StartNewAsync("SignatureChangeDemo", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(RaiseSignatureChangeDemoEventClient))]
        public static async Task<IActionResult> RaiseSignatureChangeDemoEventClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SignatureChangeDemo/{eventName}/{instanceId}")]
            HttpRequest req,
            [DurableClient] IDurableOrchestrationClient durableClient,
            string eventName,
            string instanceId,
            ILogger log)
        {
            var signatureChangeDemo = await req.ReadAsStringAsync();
            await durableClient.RaiseEventAsync(instanceId, eventName, System.Convert.ToBoolean(signatureChangeDemo));
            return new AcceptedResult();
        }
    }

}
