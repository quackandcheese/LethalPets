using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;

namespace LethalPets
{
    public class Config : SyncedInstance<Config>
    {
        public Dictionary<string, ConfigEntry<bool>> enabledPetsDict = new Dictionary<string, ConfigEntry<bool>>();

        public Config(ConfigFile cfg)
        {
            InitInstance(this);

            foreach (PetDefinition petDef in PetManager.petDefinitions)
            {
                enabledPetsDict.Add(petDef.petName,
                    cfg.Bind(
                    "Enabled Pets",                                 // Config section
                    petDef.petName,                                    // Key of this config
                    true,                                           // Default value
                    $"Is {petDef.petName} available in the store?"     // Description
                ));
            }
        }

        public bool PetIsEnabled(PetDefinition petDef)
        {
            return PetIsEnabled(petDef.petName);
        }
        public bool PetIsEnabled(string petName)
        {
            if (Config.Instance.enabledPetsDict.TryGetValue(petName, out var petIsEnabled))
            {
                return petIsEnabled.Value;
            }
            return false;
        }

        /*        public void AddPetEnabledConfig(PetDefinition petDefinition)
                {
                    enabledPetsDict.Add(petDefinition.name,
                        Plugin.configFile.Bind(
                        "Enabled Pets",                                         // Config section
                        petDefinition.name,                                     // Key of this config
                        true,                                                   // Default value
                        $"Is {petDefinition.name} available in the store?"      // Description
                    ));
                }*/

        public static void RequestSync()
        {
            if (!IsClient) return;

            using FastBufferWriter stream = new(IntSize, Allocator.Temp);
            MessageManager.SendNamedMessage("LethalPets_OnRequestConfigSync", 0uL, stream);
        }

        public static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!IsHost) return;

            Plugin.logger.LogInfo($"Config sync request received from client: {clientId}");

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                MessageManager.SendNamedMessage("LethalPets_OnReceiveConfigSync", clientId, stream);
            }
            catch (Exception e)
            {
                Plugin.logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(IntSize))
            {
                Plugin.logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                Plugin.logger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            SyncInstance(data);

            Plugin.logger.LogInfo("Successfully synced config with host.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("LethalPets_OnRequestConfigSync", OnRequestSync);
                Synced = true;

                return;
            }

            Synced = false;
            MessageManager.RegisterNamedMessageHandler("LethalPets_OnReceiveConfigSync", OnReceiveSync);
            RequestSync();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            Config.RevertSync();
        }
    }
}
