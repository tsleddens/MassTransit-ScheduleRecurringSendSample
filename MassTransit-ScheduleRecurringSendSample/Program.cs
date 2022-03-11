using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using MassTransit.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddHangfire(configuration =>
    {
        configuration.UseMemoryStorage();
    });
    services.AddHangfireServer();


    var eventBusSettings = context.Configuration.GetSection(EventBusSettings.SectionName).Get<EventBusSettings>();

    services.AddMassTransit(configurator =>
    {
        configurator.AddConsumer<TestMessageConsumer>();

        configurator.UsingRabbitMq((registrationContext, factoryConfigurator) =>
        {

            factoryConfigurator.Host(eventBusSettings.Host, h =>
            {
                h.Username(eventBusSettings.Username);
                h.Password(eventBusSettings.Password);
            });
            factoryConfigurator.UseHangfireScheduler();
        });
    });

    services.AddMassTransitHostedService();
});

var host = builder.Build();

await RegisterScheduler(host.Services, host.Services.GetRequiredService<IConfiguration>());

await host.RunAsync();

async Task RegisterScheduler(IServiceProvider services, IConfiguration configuration)
{
    var bus = services.GetRequiredService<IBus>();

    Uri sendEndpointUri = new("queue:hangfire");

    var sendEndpoint = await bus.GetSendEndpoint(sendEndpointUri);
    
    await sendEndpoint.ScheduleRecurringSend<TestMessage>(sendEndpointUri, new ScheduleTest(), new TestMessage("Hello World"));
}

public class ScheduleTest : DefaultRecurringSchedule
{
    public ScheduleTest()
    {
        CronExpression = "* * * * *";
    }
}

public class TestMessage
{
    public string Text { get; }

    public TestMessage(string text)
    {
        Text = text;
    }
}

public class TestMessageConsumer : IConsumer<TestMessage>
{
    public async Task Consume(ConsumeContext<TestMessage> context)
    {
        await Console.Out.WriteLineAsync($"----------------------------------------------------------------- MESSAGE RECEIVED {context.Message.Text}");
    }
}

public record EventBusSettings
{
    public const string SectionName = "EventBus";

    public string Host { get; init; } = null!;
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
}