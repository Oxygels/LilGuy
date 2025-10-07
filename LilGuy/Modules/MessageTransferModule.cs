using Discord;
using Discord.Interactions;
using LilGuy.Services;
using Microsoft.Extensions.Logging;

namespace LilGuy.Modules;

public class MessageTransferModule(MessageTransferService _service, ILogger<MessageTransferModule> _logger)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("channel-transfer",
        "Imports all the messages from the specified source channel to the current channel")]
    public async Task ChannelTransfer(string sourceChannelId)
    {
        _logger.LogInformation("Got {ID}", sourceChannelId);
        // We suppose this command is launched from the destination so we can get every info for it
        var destinationChannel = Context.Channel as ITextChannel;

        // For the source, we use the paramy
        if (!ulong.TryParse(sourceChannelId, out var sourceChannelIdUlong))
        {
            await RespondAsync("Invalid channel ID");
            return;
        }

        var sourceChannel = Context.Client.GetChannel(sourceChannelIdUlong);

        // We already set conditions on the channel types so the cast if safe
        var sourceTextChannel = sourceChannel as ITextChannel;
        var destinationTextChannel = destinationChannel;
        if (sourceTextChannel is null || destinationTextChannel is null)
        {
            await RespondAsync("Channels must be text channels");
            return;
        }

        await RespondAsync($"Starting transfer from {sourceTextChannel.Name} to {destinationTextChannel.Name}");
        await _service.ChannelTransfer(sourceTextChannel, destinationTextChannel);
    }
}