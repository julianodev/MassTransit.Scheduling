using MassTransit.Scheduling.Commands;
using MassTransit.Scheduling.Models;
using System;
using System.Threading.Tasks;

namespace MassTransit.Scheduling.Consumer
{
    public sealed class ScheduleNotificationConsumer : IConsumer<IScheduleNotification>
    {
        private readonly IMessageScheduler _messageScheduler;

        public ScheduleNotificationConsumer(
            IMessageScheduler messageScheduler)
        {
            _messageScheduler = messageScheduler;
        }

        public async Task Consume(ConsumeContext<IScheduleNotification> context)
        {
            await Console.Out.WriteLineAsync($"ScheduleNotificationConsumer> Received a IScheduleNotification to {context.Message.EmailAddress}");
            await Console.Out.WriteLineAsync($"ScheduleNotificationConsumer> Sending at {DateTime.Now} a SendNotification to {context.Message.EmailAddress} for {context.Message.DeliveryTime}");

            var queue = new Uri("rabbitmq://localhost/notification_queue");


            var message = await context.ScheduleSend(queue,
                                   context.Message.DeliveryTime,
                                   new SendNotificationCommand
                                   {
                                       EmailAddress = context.Message.EmailAddress,
                                       Body = context.Message.Body,
                                       DateRequest = context.Message.DeliveryTime,
                                       CancelMessage = true
                                   });

            Console.WriteLine($"CancellationTokenId:  {message.TokenId}");
        }
    }
}
