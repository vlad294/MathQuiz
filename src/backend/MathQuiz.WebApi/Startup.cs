using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathQuiz.EventBus;
using MathQuiz.EventBus.Abstractions;
using MathQuiz.EventBus.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MathQuiz.WebApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSingleton<IRabbitMqPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMqPersistentConnection>>();
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    DispatchConsumersAsync = true
                };

                return new DefaultRabbitMqPersistentConnection(factory, logger);
            });
            services.AddSingleton<IEventBus, RabbitMqEventBus>();
            services.AddScoped<TestEventHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var test = app.ApplicationServices.GetRequiredService<IEventBus>();

            test.Subscribe<TestEvent, TestEventHandler>();

            test.Publish(new TestEvent
            {
                Kek = "Hello world!"
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }

    [IntegrationEvent(AddMachineName = true, ExchangeName = "KEKOZAVR", QueueName = "MEGOZAVR")]
    public class TestEvent
    {
        public string Kek { get; set; }
    }

    public class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public Task Handle(TestEvent @event)
        {
            Console.WriteLine(@event.Kek);
            return Task.CompletedTask;
        }
    }
}
