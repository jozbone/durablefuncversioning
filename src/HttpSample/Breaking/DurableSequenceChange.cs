using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HttpSample.Breaking
{
    public static class DurableSequenceChange
    {
        // [FunctionName("DurableSequenceChange")]
        // public static async Task<List<string>> RunOrchestrator(
        //     [OrchestrationTrigger] IDurableOrchestrationContext context)
        // {
        //     var outputs = new List<string>();
        //
        //     // Replace "hello" with the name of your Durable Activity Function.
        //     outputs.Add(await context.CallActivityAsync<string>("DurableSequenceChange_Hello", "Tokyo"));
        //     outputs.Add(await context.CallActivityAsync<string>("DurableSequenceChange_Hello", "Seattle"));
        //     outputs.Add(await context.CallActivityAsync<string>("DurableSequenceChange_Hello", "London"));
        //
        //     // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
        //     return outputs;
        // }

        [FunctionName("DurableSequenceChange")]
        public static async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                bool result = await context.CallActivityAsync<bool>("DurableSequenceChange_Foo", "Foo");
                // if (result)
                // {
                //     await context.CallActivityAsync("DurableSequenceChange_SendNotification", "Some message");
                // }

                // set breakpoint and look into the control queues
                await context.CallActivityAsync("DurableSequenceChange_Bar", result);
            }
            catch (Exception ex)
            {
                var exType = ex.GetType();
            }
        }

        [FunctionName("DurableSequenceChange_Foo")]
        public static bool Foo([ActivityTrigger] string name, ILogger log)
        {
            return true;
        }

        [FunctionName("DurableSequenceChange_SendNotification")]
        public static Task SendNotification([ActivityTrigger] bool msg, ILogger log)
        {
            log.LogInformation($"Running SendNotification.");
            return Task.FromResult(0);
        }
         
        [FunctionName("DurableSequenceChange_Bar")]
        public static Task Bar([ActivityTrigger] bool result, ILogger log)
        {
            log.LogInformation($"Running Bar. Result: {result}");
            return Task.FromResult(0);
        }

        // [FunctionName("DurableSequenceChange_Hello")]
        // public static string SayHello([ActivityTrigger] string name, ILogger log)
        // {
        //     log.LogInformation($"Saying hello to {name}.");
        //     return $"Hello {name}!";
        // }

        [FunctionName("DurableSequenceChange_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableSequenceChange", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}