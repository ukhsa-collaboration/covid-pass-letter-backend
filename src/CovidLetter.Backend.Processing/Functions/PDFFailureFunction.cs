namespace CovidLetter.Backend.Processing.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using CovidLetter.Backend.Common.Application;
    using CovidLetter.Backend.Common.Application.Constants;
    using CovidLetter.Backend.Common.Options;
    using CovidLetter.Backend.Common.Utilities;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    public class PDFFailureFunction
    {
        private readonly IQueuePoster queuePoster;
        private readonly ServiceBusClient serviceBusClient;
        private readonly ServiceBusSender serviceBusSender;

        public PDFFailureFunction(IQueuePoster queuePoster)
        {
            this.queuePoster = queuePoster;
            this.serviceBusClient = new ServiceBusClient(Environment.GetEnvironmentVariable("ConnectionStrings__ServiceBus"));
            this.serviceBusSender = this.serviceBusClient.CreateSender(Environment.GetEnvironmentVariable("FailureTopic"));
        }

        [FunctionName("PDFFailures")]
        public async Task Run(
            [ServiceBusTrigger("PDFFailure", Connection ="CSBConnectionString")] string failure)
        {
            failure = failure.Remove(0, 1).Remove(failure.Length - 2).Replace(@"\", string.Empty);
            var fail = JsonConvert.DeserializeObject<FailureNotification>(failure);
            var message = this.queuePoster.MakeJsonMessage(
                fail!.Request!.CorrelationId!,
                fail,
                MessageVersions.V1);
            await this.serviceBusSender.SendMessageAsync(message);
        }
    }
}
