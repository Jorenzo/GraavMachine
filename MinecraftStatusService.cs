using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting;
using NetCord.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraavMachine
{
  public class MinecraftStatusService : BackgroundService
  {
    private readonly GatewayClient _gatewayClient;
    private readonly MinecraftSettings _minecraftSettings;
    private readonly DiscordSettings _discordSettings;

    private bool LastOnline = false;
    public MinecraftStatusService(GatewayClient gatewayClient, MinecraftSettings settings, DiscordSettings discordSettings)
    {
      _gatewayClient = gatewayClient;
      _minecraftSettings = settings;
      _discordSettings = discordSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        (bool online, int onlinePlayers, int maxPlayers) status = await MinecraftServer.PingMinecraftServer(_minecraftSettings.IP, _minecraftSettings.Port);

        string activityText = status.online ? $"Server Online {status.onlinePlayers}/{status.maxPlayers}" : "Server Offline";
        
        Console.WriteLine(activityText);

        try
        {
          await _gatewayClient.UpdatePresenceAsync(new PresenceProperties(status.online ? UserStatusType.Online : UserStatusType.DoNotDisturb).AddActivities(new UserActivityProperties(activityText, UserActivityType.Playing)));
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to update Discord presence. {ex}");
        }

        if (status.online != LastOnline)
        {
          LastOnline = status.online;

          MessageProperties message = new MessageProperties();
          message.Content = LastOnline ? "Server is online" : "Server is offline";
          message.Components = [];

          await _gatewayClient.Rest.SendMessageAsync(_discordSettings.ChannelID, message);
        }

        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
      }
    }
  }
}
