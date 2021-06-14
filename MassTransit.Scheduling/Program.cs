using MassTransit.Scheduling.Consumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;

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
            //var configuration = new ConfigurationBuilder()
            //   .AddJsonFile("appsettings.json")
            //   .Build();

            var host = Host
             .CreateDefaultBuilder(args)
               .ConfigureServices((hostContext, services) =>
               {
                   var schedulerEndpoint = new Uri("rabbitmq://localhost/quartz");

                   services.AddMassTransit(bus => {
                       bus.AddMessageScheduler(schedulerEndpoint);

                       bus.UsingRabbitMq((ctx, busConfigurator) =>
                       {
                           busConfigurator.UseMessageScheduler(schedulerEndpoint);
                           busConfigurator.ConfigureEndpoints(ctx);
                       });
                   });

                   services.AddQuartz();


                   services.AddSingleton<ScheduleNotificationConsumer>();
                   services.AddSingleton<NotificationConsumer>();
               
                   services.AddHostedService<Worker>();
               });

            return host;
        }
    }
}
