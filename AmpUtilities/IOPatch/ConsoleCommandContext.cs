using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.Fluent;
using Torch.API;
using Torch.API.Plugins;
using Torch.Commands;
using VRageMath;

namespace AMPRestartStopper.AmpUtilities.IOPatch
{
    public class ConsoleCommandContext : CommandContext
    {
        public ConsoleCommandContext(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null, List<string> args = null)
            : base(torch, plugin, steamIdSender, rawArgs, args)
        {
        }

        public override void Respond(string message, string sender = null, string font = null)
        {
            Log.Info($"{message}");
        }

        public void RespondRaw(string message, Color color, string sender = null, string font = null)
        {
            base.Respond(message, color, sender, font);
        }
    }
}