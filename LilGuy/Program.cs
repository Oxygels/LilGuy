// See https://aka.ms/new-console-template for more information

using Discord;
using Discord.WebSocket;
using LilGuy.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

var discordSocketConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
};
var discordSocketClient = new DiscordSocketClient(discordSocketConfig);

//builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services
    .AddSingleton(discordSocketClient)
    .AddSingleton<MessageTransferService>()
    .AddHostedService<DiscordHostedService>();

var app = builder.Build();
app.Run();