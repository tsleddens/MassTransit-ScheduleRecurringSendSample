using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using MassTransit.Definition;
using MassTransit.Scheduling;
using MassTransit_ScheduleRecurringSendSample;
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

    services.AddMassTransit(configurator =>
    {
        configurator.AddConsumer<TestMessageConsumer>();

        configurator.UsingRabbitMq((registrationContext, factoryConfigurator) =>
        {
            factoryConfigurator.Host(context.Configuration.GetConnectionString("RabbitMQ"));
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
        await sendEndpoint.ScheduleRecurringSend(new Uri($"queue:{consumerEndpointName}"), new ScheduleTest(), new TestMessage("Hello world!"), cancellationToken);
        await StopAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class ScheduleTest : DefaultRecurringSchedule
{
    public ScheduleTest()
    {
        CronExpression = "* * * * *";
    }
}