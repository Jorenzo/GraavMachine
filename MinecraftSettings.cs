using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraavMachine
{
  public class MinecraftSettings
  {
    public string IP { get; set; } = string.Empty;
    public int Port { get; set; } = 25565;

    public MinecraftSettings()
    {
    }
  }
}
