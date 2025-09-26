using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Gateway;
using GraavMachine;
using Microsoft.Extensions.DependencyInjection;

class Program
{
  static async Task Main(string[] args)
  {
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddDiscordGateway(options =>
    {
      options.Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent | GatewayIntents.DirectMessageReactions | GatewayIntents.GuildMessageReactions;
      options.Presence = new PresenceProperties(UserStatusType.Online).AddActivities(new UserActivityProperties("MeinKreft", UserActivityType.Playing));

    }).AddGatewayEventHandlers(typeof(Program).Assembly);

    var config = builder.Configuration;
    string ip = config["MinecraftServer:Ip"] ?? "localhost";
    int port = int.Parse(config["MinecraftServer:Port"] ?? "25565");
    ulong channelID = ulong.Parse(config["Discord:ChannelID"] ?? "0");

    Console.WriteLine($"Ip: {ip}, Port: {port}");

    builder.Services.AddSingleton(new MinecraftSettings { IP = ip, Port = port });
    builder.Services.AddSingleton(new DiscordSettings { ChannelID = channelID });
    builder.Services.AddHostedService<MinecraftStatusService>();

    var host = builder.Build().UseGatewayEventHandlers();

    await host.RunAsync();
  }
}