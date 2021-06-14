using GreenPipes;
using MassTransit.QuartzIntegration;
using MassTransit.Scheduling.Consumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading.Tasks;

namespace MassTransit.Scheduling
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args)
                .Build()
                .RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();

            return Host.CreateDefaultBuilder(args).
                ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AppConfig>(configuration.GetSection("AppConfig"));
                    services.Configure<QuartzConfig>(configuration.GetSection("quartz"));


                    // Service Bus
                    services.AddMassTransit(mt =>
                {
                    var schedulerEndpoint = new Uri("rabbitmq://localhost/quartz-scheduler");

                    mt.AddMessageScheduler(schedulerEndpoint);

                    mt.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.UseMessageScheduler(schedulerEndpoint);

                        var scheduler = context.GetRequiredService<IScheduler>();
                        var options = context.GetRequiredService<IOptions<AppConfig>>().Value;

                        cfg.Host(options.Host, options.VirtualHost, h =>
                        {
                            h.Username(options.Username);
                            h.Password(options.Password);
                        });

                        cfg.ConfigureEndpoints(context);

                        cfg.UseJsonSerializer(); // Because we are using json within Quartz for serializer type

                        cfg.ReceiveEndpoint(options.QueueName, endpoint =>
                        {
                            var partitionCount = Environment.ProcessorCount;
                            endpoint.PrefetchCount = (ushort)(partitionCount);
                            var partitioner = endpoint.CreatePartitioner(partitionCount);

                            endpoint.Consumer(() => new ScheduleMessageConsumer(scheduler), x => x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.CorrelationId)));

                            endpoint.Consumer(() => new CancelScheduledMessageConsumer(scheduler), x => x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));
                        });

                    });
                });

                    services.AddSingleton<NotificationConsumer>();
                    services.AddSingleton<ScheduleNotificationConsumer>();

                    services.AddHostedService<Worker>();

                    services.AddSingleton(x =>
                    {
                        var quartzConfig = x.GetRequiredService<IOptions<QuartzConfig>>().Value
                            .UpdateConnectionString(hostContext.Configuration.GetConnectionString("scheduler-db"))
                            .ToNameValueCollection();

                        return new StdSchedulerFactory(quartzConfig)
                                .GetScheduler()
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();

                    });
                });
        }
    }
}
