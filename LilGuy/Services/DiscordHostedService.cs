using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LilGuy.Services;

public class DiscordHostedService(
    DiscordSocketClient _client,
    ILogger<DiscordHostedService> _logger,
    IServiceProvider _serviceProvider) : BackgroundService
{
    private readonly InteractionService _interactionService = new(_client.Rest);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += ClientOnLog;
        _client.Ready += ClientOnReady;
        _client.InteractionCreated += ClientOnInteractionCreated;
        var token = Environment.GetEnvironmentVariable("LILGUY_TOKEN");
        await _client.LoginAsync(TokenType.Bot, token);
        var moduleInfos = await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
        foreach (var module in moduleInfos) _logger.LogWarning($"Adding module {module.Name}");
        await _client.StartAsync();
    }

    private async Task ClientOnInteractionCreated(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(_client, arg);
        await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
    }

    private async Task ClientOnReady()
    {
        await _interactionService.RegisterCommandsGloballyAsync();
    }

    private Task ClientOnLog(LogMessage arg)
    {
        switch (arg.Severity)
        {
            case LogSeverity.Critical:
                _logger.LogCritical(arg.Exception, arg.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(arg.Exception, arg.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(arg.Exception, arg.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation(arg.Exception, arg.Message);
                break;
            case LogSeverity.Verbose:
                _logger.LogTrace(arg.Exception, arg.Message);
                break;
            case LogSeverity.Debug:
                _logger.LogDebug(arg.Exception, arg.Message);
                break;
            default:
                _logger.LogWarning(arg.Exception, arg.Message);
                break;
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }
}