using MassTransit;

namespace MassTransit_ScheduleRecurringSendSample;


public class TestMessage
{
    public string Text { get; set; }
}

public class TestMessageConsumer : IConsumer<TestMessage>
{
    public async Task Consume(ConsumeContext<TestMessage> context)
    {
        await Console.Out.WriteLineAsync($"----------------------------------------------------------------- MESSAGE RECEIVED {context.Message.Text}");
    }
}