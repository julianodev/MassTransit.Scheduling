using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MassTransit.Scheduling.Consumer;
using MassTransit.Scheduling.Models;
using Quartz;
using MassTransit.QuartzIntegration;
using Quartz.Impl;
using MassTransit.Util;
using Quartz.Simpl;

namespace MassTransit.Scheduling
{
    public class Worker : BackgroundService
    {

        private readonly IServiceProvider _provider;
        private readonly IBusControl _busControl;
        private readonly IMessageScheduler _messageScheduler;

        public Worker(
            IBusControl busControl,
            IServiceProvider serviceProvider,
            IMessageScheduler messageScheduler)
        {
            _busControl = busControl;
            _provider = serviceProvider;
            _messageScheduler = messageScheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scheduler = CreateScheduler();

            await ConfigureBusAsync(
                scheduler);

            scheduler.JobFactory = new MassTransitJobFactory(_busControl, new SimpleJobFactory());

            await scheduler.Start();

            Console.WriteLine("'q' to exit");
            Console.WriteLine("'1' -> Scheduling a message from a consumer");
            Console.WriteLine("'2' -> Cancel Scheduling a Message");

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
                            DeliveryTime = DateTime.Now.AddSeconds(30),
                            EmailAddress = "jefferson@live.com",
                            Body = "Hello World!"
                        });
                        break;
                    case "2":
                        Console.WriteLine("Por favor informe o cancellationId");
                        var cancellationId = Console.ReadLine();
                        var queue = new Uri("rabbitmq://localhost/notification_queue");
                        var id = new Guid(cancellationId);
                        await _messageScheduler.CancelScheduledSend(queue, id);
                        break;
                }

            } while (true);


            await _busControl.StopAsync();
        }

        static IScheduler CreateScheduler()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            var scheduler = TaskUtil.Await(() => schedulerFactory.GetScheduler());

            return scheduler;
        }

        private async Task ConfigureBusAsync(
            IScheduler scheduler)
        {
            var handlerQuartz = _busControl.ConnectReceiveEndpoint("quartz", e =>
            {
                e.Consumer(() => new ScheduleMessageConsumer(scheduler));
            });

            var handlerScheduleNotification = _busControl.ConnectReceiveEndpoint(
                "schedule_notification_queue", e =>
            {
                e.Consumer<ScheduleNotificationConsumer>(_provider);
            });

            var handlerNotification = _busControl.ConnectReceiveEndpoint("notification_queue", e =>
            {
                e.Consumer<NotificationConsumer>(_provider);
            });


            await Task.WhenAll(
                handlerQuartz.Ready,
                handlerScheduleNotification.Ready,
                handlerNotification.Ready);
        }
    }
}
