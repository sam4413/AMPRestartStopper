using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Plugins;
using Torch.Commands;
using VRageMath;
using static Sandbox.Engine.Networking.MyWorkshop;
namespace AMPRestartStopper.AmpUtilities.IOPatch
{
    public class AmpCommandHandler : CommandContext
    {
        public event Action<string, string, string> OnResponse;

        private readonly StringBuilder _response = new StringBuilder();
        private CancellationTokenSource _cancelToken;

        public AmpCommandHandler(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null, List<string> args = null)
            : base(torch, plugin, steamIdSender, rawArgs, args)
        {
        }

        public override void Respond(string message, string sender = "Server", string font = "Blue")
        {
            _response.AppendLine(message);

            if (_cancelToken != null)
                _cancelToken.Cancel();
            _cancelToken = new CancellationTokenSource();

            var a = Task.Delay(500, _cancelToken.Token)
                .ContinueWith((t) =>
                {
                    string chunk;
                    lock (_response)
                    {
                        chunk = _response.ToString();
                        _response.Clear();
                    }
                    OnResponse.Invoke(chunk, sender, font);
                });
        }

        public void RespondRaw(string message, Color color, string sender = null, string font = null)
        {
            base.Respond(message, color, sender, font);
        }
    }
}