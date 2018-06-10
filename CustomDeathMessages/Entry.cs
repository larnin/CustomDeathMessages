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

namespace CustomDeathMessages
{
    public class Entry : IPlugin
    {
        public string IPCIdentifier { get { return "CustomDeathMessages"; }  set { } }

        public void Initialize(IManager manager)
        {
            var harmony = HarmonyInstance.Create("com.Larnin.CustomDeathMessages");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ChatLog), "OnCarEventDeath")]
        internal class ChatLogOnCarEventDeath
        {
            static bool Prefix(GameObject sender, Death.Data data)
            {
                string text = null;
                switch (data.causeOfDeath)
                {
                    case Death.Cause.KillGrid:
                        text = Message.GetMessage("KillGrid", Message.GetPlayerName());
                        break;
                    case Death.Cause.SelfTermination:
                        text = Message.GetMessage("SelfTermination", Message.GetPlayerName());
                        break;
                    case Death.Cause.LaserOverheated:
                        text = Message.GetMessage("LaserOverheated", Message.GetPlayerName());
                        break;
                    case Death.Cause.Impact:
                    case Death.Cause.ImpactSquish:
                        text = Message.GetMessage("Impact", Message.GetPlayerName());
                        break;
                    case Death.Cause.Overheated:
                        text = Message.GetMessage("Overheated", Message.GetPlayerName());
                        break;
                    case Death.Cause.AntiTunnelSquish:
                        text = Message.GetMessage("AntiTunnelSquish", Message.GetPlayerName());
                        break;
                }

                if (text != null && sender.HasComponent<PlayerDataLocal>())
                    Message.SendMessage(text);

                return false;
            }
        }

        [HarmonyPatch(typeof(ClientLogic), "BroadcastNotEnoughDataToUpdateWorkshopLevel")]
        internal class ClientLogicBroadcastNotEnoughDataToUpdateWorkshopLevel
        {
            static bool Prefix()
            {
                Message.SendMessage(Message.GetMessage("KickNoLevel", Message.GetPlayerName()));
                StaticEvent<NotEnoughInfoToUpdateWorkshopLevel.Data>.Broadcast(null);
                return false;
            }
        }

        [HarmonyPatch(typeof(TimeBasedMode), "LocalPlayerFinishVirtual")]
        internal class TimeBasedModeLocalPlayerFinishVirtual
        {
            static bool Prefix(TimeBasedMode __instance, ref int __result, PlayerDataLocal playerData, FinishType finishType)
            {
                int num = -1;

                var list = __instance.GetType().GetField("modePlayerInfos_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as List<ModePlayerInfoBase>;

                TimeBasedModePlayerInfo timeBasedModePlayerInfo = list[playerData.PlayerIndex_] as TimeBasedModePlayerInfo;
                if (finishType == FinishType.Normal)
                {
                    double time = (double)(Timex.ModeTimeCS_ * 10);
                    double points = (double)playerData.Points_;
                    num = (int)__instance.EvaluateTimeForMedal(time, points);
                    if (!CheatsManager.GameplayCheatsUsedThisLevel_)
                    {
                        var function = __instance.GetType().GetMethod("UploadScoreAndReplay", BindingFlags.Instance | BindingFlags.NonPublic);
                        function.Invoke(__instance, new object[] { playerData, finishType, num });
                    }
                    Message.SendMessage(Message.GetMessage("Finished", Message.GetPlayerName()));
                }
                else if (finishType == FinishType.DNF || finishType == FinishType.Spectate || finishType == FinishType.LeavingLevel)
                {
                    num = (int)timeBasedModePlayerInfo.distanceToFinish_;
                }
                else if (finishType.IsFinishedAtStart())
                {
                    num = -1;
                }
                else
                {
                    Debug.LogError("Player finished with unknown FinishType: " + finishType);
                }
                playerData.CarCamera_.Spectate();
                __result = num;

                return false;
            }
        }

        [HarmonyPatch(typeof(StuntCollectibleLogic), "OnTriggerEnter")]
        internal class StuntCollectibleLogicOnTriggerEnter
        {
            static bool Prefix(StuntCollectibleLogic __instance, Collider other)
            {
                bool spawned = (bool)(__instance.GetType().GetField("spawned_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance));
                int type = (int)(__instance.GetType().GetField("type_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance));
                Phantom phatom = __instance.GetType().GetField("phantom_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as Phantom;

                if (!spawned)
                {
                    return false;
                }
                PlayerDataLocal playerDataLocal = GUtils.IsRelevantLocalCar(other);
                if (playerDataLocal)
                {
                    MeshRenderer component = __instance.collectibles_[type].GetComponent<MeshRenderer>();
                    ColorHSB colorHSB = component.sharedMaterial.GetColor("_EmitColor").ToColorHSB();
                    colorHSB.b = 1f;
                    Color col = colorHSB.ToColor();
                    Vector3 collectibleColor = col.ToVector3();
#pragma warning disable CS0618 // Le type ou le membre est obsolète
                    StaticTransceivedEvent<HitTagStuntCollectible.Data>.Broadcast(new HitTagStuntCollectible.Data(playerDataLocal.PlayerIndex_, type, collectibleColor));
#pragma warning restore CS0618 // Le type ou le membre est obsolète
                    Message.SendMessage(Message.GetMessage("StuntCollect", Message.GetPlayerName(), type + 2));
                    for (int i = 0; i < __instance.particlesGPU_.Length; i++)
                    {
                        __instance.particlesGPU_[i].ZEventReceived(false);
                        __instance.particlesGPU_[i].ZEventReceived(true);
                    }
                    phatom.Play("StuntCollectibleHit", 0f, false);
                }

                return false;
            }
        }
    }
}
