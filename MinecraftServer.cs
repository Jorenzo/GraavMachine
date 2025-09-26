using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GraavMachine
{
  public class MinecraftServer
  {
    public static async Task<(bool online, int onlinePlayers, int maxPlayers)> PingMinecraftServer(string host, int port)
    {
      try
      {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        using var stream = client.GetStream();

        // Write handshake packet
        using var handshake = new MemoryStream();
        using var writer = new BinaryWriter(handshake, Encoding.UTF8, true);

        WriteVarInt(writer, 0x00); // Packet ID for handshake
        WriteVarInt(writer, 754); // Protocol version (e.g. 754 for 1.16.5, adjust if needed)
        WriteVarInt(writer, Encoding.UTF8.GetByteCount(host)); // Host length
        writer.Write(Encoding.UTF8.GetBytes(host)); // Host string
        writer.Write((ushort)port); // Port
        WriteVarInt(writer, 1); // Next state: status

        // Now write handshake packet with length prefix
        WriteVarInt(new BinaryWriter(stream), (int)handshake.Length);
        stream.Write(handshake.ToArray());

        // Write status request packet
        WriteVarInt(new BinaryWriter(stream), 1); // Packet length
        WriteVarInt(new BinaryWriter(stream), 0); // Packet ID for status request

        // Read length of packet (VarInt)
        int packetLength = ReadVarInt(stream);
        int packetId = ReadVarInt(stream);

        if (packetId != 0x00) // status response packet id
          return (false, 0, 0);

        int jsonLength = ReadVarInt(stream);

        byte[] jsonBytes = new byte[jsonLength];
        int totalRead = 0;
        while (totalRead < jsonLength)
        {
          int read = await stream.ReadAsync(jsonBytes, totalRead, jsonLength - totalRead);
          if (read == 0)
            throw new IOException("Unexpected end of stream while reading JSON.");
          totalRead += read;
        }

        string json = Encoding.UTF8.GetString(jsonBytes);

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        int onlinePlayers = root.GetProperty("players").GetProperty("online").GetInt32();
        int maxPlayers = root.GetProperty("players").GetProperty("max").GetInt32();

        return (true, onlinePlayers, maxPlayers);
      }
      catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
      {
        return (false, 0, 0);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[ERROR] Failed to ping Minecraft server at {host}:{port}");
        Console.WriteLine($"[EXCEPTION] {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine($"[STACKTRACE] {ex.StackTrace}");
        return (false, 0, 0);
      }
    }

    static void WriteVarInt(BinaryWriter writer, int value)
    {
      while (true)
      {
        if ((value & ~0x7F) == 0)
        {
          writer.Write((byte)value);
          return;
        }
        writer.Write((byte)((value & 0x7F) | 0x80));
        value >>= 7;
      }
    }

    static int ReadVarInt(Stream stream)
    {
      int numRead = 0;
      int result = 0;
      byte read;
      do
      {
        int r = stream.ReadByte();
        if (r == -1)
          throw new IOException("Stream ended while reading VarInt");

        read = (byte)r;
        int value = (read & 0x7F);
        result |= (value << (7 * numRead));

        numRead++;
        if (numRead > 5)
          throw new IOException("VarInt is too big");
      } while ((read & 0x80) != 0);

      return result;
    }
  }
}
