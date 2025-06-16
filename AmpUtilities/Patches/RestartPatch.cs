using HarmonyLib;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API.Session;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Server;

namespace AmpUtilities.Patches
{
    internal class RestartPatch
    {
        [HarmonyPatch]
        [HarmonyPatch(typeof(TorchServer), "Restart")]
        public class MyRestartPatch
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
