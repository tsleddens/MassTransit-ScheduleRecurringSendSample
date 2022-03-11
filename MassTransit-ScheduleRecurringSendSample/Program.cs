using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using MassTransit.Definition;
using MassTransit.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddScoped<SetupRecurringSend>();

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

            factoryConfigurator.ConfigureEndpoints(registrationContext, DefaultEndpointNameFormatter.Instance);
        });
    });

    services.AddMassTransitHostedService(true);

    services.AddHostedService<SetupRecurringSend>();
});

var host = builder.Build();

await host.RunAsync();

public class SetupRecurringSend : IHostedService
{
    private readonly IBus _bus;
    private readonly ILogger<SetupRecurringSend> _logger;

    public SetupRecurringSend(IBus bus, ILogger<SetupRecurringSend> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("---------------- SETTING UP RECURRING SCHEDULE -----------------");

        Uri sendEndpointUri = new("queue:hangfire");
        
        var sendEndpoint = await _bus.GetSendEndpoint(sendEndpointUri);
        
        string consumerEndpointName = DefaultEndpointNameFormatter.Instance.Consumer<TestMessageConsumer>();
        await sendEndpoint.ScheduleRecurringSend(new Uri($"queue:{consumerEndpointName}"), new ScheduleTest(), new TestMessage("Hello World"), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
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