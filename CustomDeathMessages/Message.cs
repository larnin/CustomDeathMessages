using Events;
using Events.ClientToAllClients;
using Spectrum.API.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CustomDeathMessages
{
    static class Message
    {
        static Dictionary<string, string> m_sentenses;

        public static void SendMessage(string message)
        {
#pragma warning disable CS0618 // Le type ou le membre est obsolète
            StaticTransceivedEvent<ChatMessage.Data>.Broadcast(new ChatMessage.Data(message.Colorize("[FFE999]")));
#pragma warning restore CS0618 // Le type ou le membre est obsolète
        }

        public static string GetMessage(string key, params object[] values)
        {
            if (m_sentenses == null)
                InitializeSentenses();

            if(!m_sentenses.ContainsKey(key))
            {
                throw new Exception("The value you want to retrieve does not exist.");
            }

            var str = m_sentenses[key];
            
            for(int i = 0; i < values.Length; i++)
                str = str.Replace("{" + i + "}", values[i].ToString());

            return str;
        }

        public static string GetPlayerName()
        {
            var methode = G.Sys.GameManager_.Mode_.GetType().GetMethod("GetSortedListOfModeInfos", BindingFlags.Instance | BindingFlags.NonPublic);

            var clientLogic = G.Sys.PlayerManager_.GetType().GetField("clientLogic_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(G.Sys.PlayerManager_);
            return clientLogic.GetType().GetMethod("GetLocalChatName", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(clientLogic, new Object[] { false, "FFFFFF" }) as string;
        }

        private static void InitializeSentenses()
        {
            var settings = new Settings("CustomDeathMessages");

            var entries = new Dictionary<string, string>
            {
                {"KillGrid", "{0} was terminated by the laser grid" },
                {"SelfTermination", "{0} reset" },
                {"LaserOverheated", "{0} was wrecked after getting split" },
                {"Impact", "{0} got wrecked" },
                {"Overheated", "{0} exploded from overheating" },
                {"AntiTunnelSquish", "{0} got wrecked?" },
                {"StuntCollect", "{0} grabbed the x{1} multiplier!" },
                {"KickNoLevel", "{0} was kicked due to not having this level" },
                {"Finished", "{0} finished" }
            };

            foreach (var s in entries)
                if (!settings.ContainsKey(s.Key))
                    settings.Add(s.Key, s.Value);

            settings.Save();

            m_sentenses = new Dictionary<string, string>();
            foreach (var s in entries)
                m_sentenses[s.Key] = settings.GetItem<string>(s.Key);
        }
    }
}
