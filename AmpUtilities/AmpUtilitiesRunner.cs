using HarmonyLib;
using NLog;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Commands;
using Torch.Session;
using Torch.Views;
using VRage.Utils;

namespace AmpUtilities
{
    public class AmpUtilitiesRunner : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Thread _inputThread;
        private bool _running = true;
        private static CommandManager _commandManager;
        private static IChatManagerServer _chatManagerServer;
        private readonly Harmony _harmony = new Harmony("AmpUtilities.AmpUtilities");

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Log.Info("Patching methods..");
            _harmony.PatchAll();
            Log.Warn("Methods patched!");
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                Log.Info("Session Loaded!");
                _commandManager = Torch.CurrentSession.Managers.GetManager<CommandManager>();
                _chatManagerServer = Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                if (_inputThread == null || !_inputThread.IsAlive)
                {
                    _running = true;
                    _inputThread = new Thread(ReadInputLoop) { IsBackground = true };
                    _inputThread.Start();
                }
                break;

                case TorchSessionState.Unloading:
                Log.Info("Session Unloading!");
                _running = false;
                if (_inputThread != null && _inputThread.IsAlive)
                {
                    try
                    { _inputThread.Interrupt(); }
                    catch { }
                    _inputThread = null;
                }
                break;
            }
        }

        private void ReadInputLoop()
        {
            while (_running)
            {
                try
                {
                    string line = System.Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    Log.Info($"Received Input: {line}");
                    Thread CurrentThread = Thread.CurrentThread;
                    if (CurrentThread != MyUtils.MainThread)
                    {
                        MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                        {
                            try
                            {
                                if (line.StartsWith("!"))
                                {
                                    RunCommand(line);
                                }
                                else
                                {
                                    SendChatMessage("Server", line);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"Error when invoking {ex}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"STDIN error: {ex.Message}");
                }
            }
        }

        public void SendChatMessage(string author, string message)
        {
            try
            {
                if (_chatManagerServer != null)
                    _chatManagerServer.SendMessageAsOther(author, message, VRageMath.Color.PaleVioletRed);
                else
                    Log.Info($"Can't find Chat Manager");
            }
            catch (Exception ex)
            {
                Log.Info($"Error sending chat message {ex.Message}");
            }
        }

        public void RunCommand(string commandText)
        {
            try
            {

                if (_commandManager == null)
                {
                    Log.Info($"Command is null");
                    return;
                }
                if (_commandManager.Commands == null)
                {
                    Log.Info($"Command tree is null");
                    return;
                }
                string argsText;

                if (commandText.StartsWith("!"))
                    commandText = commandText.Substring(1);

                var command = _commandManager.Commands.GetCommand(commandText, out argsText);

                if (command != null)
                {
                    var argsList = argsText.Split(' ').ToList();
                    var context = new CommandContext(Torch, this, 0, argsText, argsList);
                    command.TryInvoke(context);
                }
                else
                {
                    Log.Info($"Command later is null");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Error running command: {ex}");
            }
        }
    }
}