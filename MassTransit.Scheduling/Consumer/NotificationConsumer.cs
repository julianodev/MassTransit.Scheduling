using MassTransit.Scheduling.Models;
using System;
using System.Threading.Tasks;

namespace MassTransit.Scheduling.Consumer
{
    public class NotificationConsumer : IConsumer<ISendNotification>
    {
        public async Task Consume(ConsumeContext<ISendNotification> context)
        {
            await Console.Out.WriteLineAsync($"NotificationConsumer> Received at {DateTime.Now} a SendNotification: {context.Message.EmailAddress} Message Request: {context.Message.DateRequest}");
        }
    }
}
