using Spectrum.API;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spectrum.API.Configuration;
using System.IO;
using Harmony;
using System.Reflection;
using Events.Car;
using Events.LocalClient;
using Events;
using Events.Stunt;
using Events.Local;

namespace CustomDeathMessages
{
    public class Entry : IPlugin
    {
        public void Initialize(IManager manager, string ipcIdentifier)
        {
            var harmony = HarmonyInstance.Create("com.Larnin.CustomDeathMessages");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ClientLogic), "OnEventPlayerActionMessage")]
        internal class ClientLogicOnEventPlayerActionMessage
        {
            static bool Prefix(ClientLogic __instance, PlayerActionMessage.Data data)
            {
                var name = __instance.GetType().GetMethod("GetLocalChatName", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { false, "FFFFFF" }) as string;

                string message = data.message_;

                if (message.Contains("was terminated by the laser grid"))
                {
                    message = Message.GetMessage("KillGrid", name);
                }
                else if (message.Contains("reset"))
                {
                    message = Message.GetMessage("SelfTermination", name);
                }
                else if (message.Contains("was wrecked after getting split"))
                {
                    message = Message.GetMessage("LaserOverheated", name);
                }
                else if (message.Contains("got wrecked?"))
                {
                    message = Message.GetMessage("AntiTunnelSquish", name);
                }
                else if (message.Contains("got wrecked"))
                {
                    message = Message.GetMessage("Impact", name);
                }
                else if (message.Contains("exploded from overheating"))
                {
                    message = Message.GetMessage("Overheated", name);
                }
                else if (message.Contains("multiplier!"))
                {
                    int result;
                    int.TryParse(message.Substring(14), out result);
                    message = Message.GetMessage("StuntCollect", name, result);
                }
                else if (message.Contains("was kicked due to not having this level"))
                {
                    message = Message.GetMessage("KickNoLevel", name);
                }
                else if (message.Contains("finished"))
                {
                    message = Message.GetMessage("Finished", name);
                }

                Message.SendMessage(message);

                return false;
            }
        }

        [HarmonyPatch(typeof(ClientLogic), "OnEventToAllClientsRemotePlayerActionMessage")]
        internal class ClientLogicOnEventToAllClientsRemotePlayerActionMessage
        {
            static bool Prefix(ClientLogic __instance, ToAllClientsRemotePlayerActionMessage.Data data)
            {
                string message = data.message_;
                var name = (__instance.GetType().GetField("clientPlayerList_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as List<ClientPlayerInfo>)[data.index_].GetChatName("FFFFFF");

                if(message.Contains("is not ready"))
                {
                    message = Message.GetMessage("NotReady", name);
                }
                else if(message.Contains("left the match to spectate"))
                {
                    message = Message.GetMessage("Spectate", name);
                }
                else if(message.Contains("has taken the lead!"))
                {
                    message = Message.GetMessage("TagPointsLead", name);
                }

                Message.SendMessage(message);

                return false;
            }
        }
    }
}
