using Discord;
using Microsoft.Extensions.Logging;

namespace LilGuy.Services;

public class MessageTransferService(ILogger<MessageTransferService> _logger)
{
    private const int MaxMessagesCount = 300;

    public async Task ChannelTransfer(ITextChannel sourceChannel, ITextChannel destinationChannel)
    {
        var messagesList = new List<IMessage>(MaxMessagesCount);
        var requestOption = new RequestOptions
        {
            RetryMode = RetryMode.RetryRatelimit, RatelimitCallback = OnRateLimit
        };

        // We first get a batch of last messages
        var messages = await sourceChannel.GetMessagesAsync(MaxMessagesCount, options: requestOption).FlattenAsync();
        messagesList.AddRange(messages);
        int count;

        // Then we get the oldest one and continue from there
        do
        {
            messages = await sourceChannel.GetMessagesAsync(messagesList[^1], Direction.Before, MaxMessagesCount,
                options: requestOption).FlattenAsync();
            var tempList = messages.ToList();
            count = tempList.Count;
            messagesList.AddRange(tempList);
            await Task.Delay(1000);
        } while (count > 0);

        messagesList.Reverse();

        // We got all the messages, in history order, just send them
        await SendMessages(destinationChannel, messagesList, requestOption);
    }

    private async Task SendMessages(ITextChannel destinationChannel, List<IMessage> messagesList,
        RequestOptions requestOption)
    {
        // Dont send more than 15 messages per second
        // Global rate limit is at 50 RPS for now
        var chunks = messagesList.Chunk(15);
        foreach (var chunk in chunks)
        {
            foreach (var message in chunk)
            {
                foreach (var attachment in message.Attachments)
                    // Send the attachment url to prevent redownloading
                    await destinationChannel.SendMessageAsync(attachment.Url, options: requestOption);

                // If the message is empty, sending timeouts
                if (string.IsNullOrEmpty(message.Content)) continue;
                var timestamp = message.Timestamp.ToString("g");
                await destinationChannel.SendMessageAsync($"{message.Content}\t- {timestamp}", options: requestOption);
            }

            await Task.Delay(1000);
        }
    }

    private Task OnRateLimit(IRateLimitInfo rateLimitInfo)
    {
        var toWait = rateLimitInfo.RetryAfter ?? 300;
        _logger.LogWarning("Reached rate limit for {Endpoint}, retrying after {Seconds}", rateLimitInfo.Endpoint,
            rateLimitInfo.RetryAfter);
        return Task.CompletedTask;
    }
}