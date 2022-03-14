using MassTransit;

namespace MassTransit_ScheduleRecurringSendSample;


public record TestMessage(string Text);

public class TestMessageConsumer : IConsumer<TestMessage>
{
    public async Task Consume(ConsumeContext<TestMessage> context)
    {
        await Console.Out.WriteLineAsync($"----------------------------------------------------------------- MESSAGE RECEIVED {context.Message.Text}");
    }
}