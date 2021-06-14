using MassTransit.QuartzIntegration;
using MassTransit.Scheduling.Commands;
using MassTransit.Scheduling.Consumer;
using MassTransit.Scheduling.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace MassTransit.Scheduling
{
    public class Worker : IHostedService
    {
        private static ScheduledRecurringMessage<SendNotificationCommand> _recurringScheduledMessage;
        private readonly IBusControl _busControl;
        private readonly IScheduler _scheduler;
        private readonly IMessageScheduler _messageScheduler;
        private readonly IServiceProvider _serviceProvider;

        public Worker(
            IServiceProvider serviceProvider,
            IBusControl bus,
            IScheduler scheduler,
            IMessageScheduler messageScheduler)
        {
            _serviceProvider = serviceProvider;
            _busControl = bus;
            _scheduler = scheduler;
            _messageScheduler = messageScheduler;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            var options = _serviceProvider.GetRequiredService<IOptions<AppConfig>>().Value;

            Console.WriteLine("Starting Bus");

            await _busControl
                .StartAsync(cancellationToken)
                .ConfigureAwait(false);

            _scheduler.JobFactory = new MassTransitJobFactory(
                _busControl,
                null);

            try
            {
                await _scheduler.Start();

                await StartConsumersAsync();

                WriteOptionsConsole();

                do
                {
                    var operation = Console.ReadLine();

                    if ("q".Equals(operation, StringComparison.OrdinalIgnoreCase))
                        break;

                    switch (operation)
                    {
                        case "1":
                            Console.Write("Sending IScheduleNotification");
                            var endpoint = await _busControl.GetSendEndpoint(new Uri("rabbitmq://localhost/schedule_notification_queue"));
                            await endpoint.Send<IScheduleNotification>(new
                            {
                                DeliveryTime = DateTime.Now.AddMinutes(1),
                                EmailAddress = "masstransit@live.com",
                                Body = "Hello World!"
                            });
                            Console.WriteLine("Message Sended with successfully");
                            break;
                        case "2":
                            var schedulerAddress = new Uri($"rabbitmq://{options.Host}/{ options.QueueName}");
                            _recurringScheduledMessage = await _busControl.GetSendEndpoint(schedulerAddress)
                                .Result.ScheduleRecurringSend(new Uri("rabbitmq://localhost/notification_queue"),
                                new PollExternalSystemSchedule(),
                                new SendNotificationCommand
                                {
                                    EmailAddress = "test@yopmail.com",
                                    Body = "Hello World!"
                                });
                            Console.WriteLine("Message Sended with successfully");
                            break;
                        case "3":
                            var queue = new Uri("rabbitmq://localhost/notification_queue");
                            Console.WriteLine("Please, enter with cancellationId");
                            var cancellationId = new Guid(Console.ReadLine().Trim());
                            await _messageScheduler.CancelScheduledSend(queue, cancellationId);
                            Console.WriteLine("Message canceled with successfully");
                            break;
                        case "4":
                            if (_recurringScheduledMessage != null)
                            {
                                Console.WriteLine("Cancel sending SendNotificationCommand every 5 seconds");
                                await _busControl.CancelScheduledRecurringSend(_recurringScheduledMessage);
                                _recurringScheduledMessage = null;
                            }
                            else
                                Console.WriteLine("No schedule to cancel, please press 3 before");
                            break;
                    }

                } while (true);
            }
            catch (Exception)
            {
                await _scheduler.Shutdown();
                throw;
            }

            Console.WriteLine("Started");
        }

        private void WriteOptionsConsole()
        {
            Console.WriteLine("'1' -> Scheduling a message from a consumer");
            Console.WriteLine("'2' -> Scheduling a recurring message");
            Console.WriteLine("'3' -> Cancel Scheduling a Message");
            Console.WriteLine("'4' -> Cancel Scheduling a recurring message");
            Console.WriteLine("'q' to exit");
        }

        public async Task StopAsync(
            CancellationToken cancellationToken)
        {
            await _scheduler.Standby();
            Console.WriteLine("Stopping");
            await _busControl.StopAsync(cancellationToken);
            await _scheduler.Shutdown();
            Console.WriteLine("Stopped");
        }

        private async Task StartConsumersAsync()
        {
            var handlerScheduleNotification = _busControl.ConnectReceiveEndpoint(
                "schedule_notification_queue", e =>
                {
                    e.Consumer<ScheduleNotificationConsumer>(_serviceProvider);
                });

            var handlerNotification = _busControl.ConnectReceiveEndpoint("notification_queue", e =>
            {
                e.Consumer<NotificationConsumer>(_serviceProvider);
            });


            await Task.WhenAll(
                handlerScheduleNotification.Ready,
                handlerNotification.Ready);
        }
    }
}
