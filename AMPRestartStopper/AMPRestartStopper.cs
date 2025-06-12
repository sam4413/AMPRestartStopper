using HarmonyLib;
using NLog;
using System.Diagnostics;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Server;
using Torch.Session;

namespace AMPRestartStopper
{
    public class AMPRestartStopper : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Harmony _harmony = new Harmony("AMPRestartStopper.AMPRestartStopper");
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");


            Log.Warn("Starting AMPRestartStopper");
            _harmony.PatchAll();
            Log.Warn("Running AMPRestartStopper");

        }
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            switch (state)
            {

                case TorchSessionState.Loaded:
                Log.Info("Session Loaded!");
                break;

                case TorchSessionState.Unloading:
                Log.Info("Session Unloading!");
                break;
            }
        }
        [HarmonyPatch]
        [HarmonyPatch(typeof(TorchServer), "Restart")]
        public class RestartPatch
        {
            public static bool Prefix(TorchServer __instance, bool save)
            {
                if (__instance.Config.DisconnectOnRestart)
                {
                    ModCommunication.SendMessageToClients(new JoinServerMessage("0.0.0.0:25555"));
                    Log.Info("Ejected all players from server for restart.");
                }
                if (__instance.IsRunning && save)
                    __instance.Save().ContinueWith(KillProc, __instance, TaskContinuationOptions.RunContinuationsAsynchronously);

                KillProc(null, __instance);
                return false;
            }
        }
        public static void KillProc(Task<GameSaveResult> task, object torch0)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
