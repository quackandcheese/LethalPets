using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalPets
{
    [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.TeleportPlayerClientRpc))]
    public class EntranceTeleportPatch
    {
        static void Postfix(EntranceTeleport __instance, int playerObj)
        {
            PlayerControllerB localPlayer = __instance.playersManager.allPlayerScripts[playerObj];//GameNetworkManager.Instance.localPlayerController;

            foreach (GameObject gameObject in PetManager.spawnedPets)
            {
                if (gameObject.TryGetComponent(out PetAI petAI))
                {
                    if (petAI.ownerPlayer == localPlayer && petAI.CanFollowOwnerIntoFacility())
                    {
                        petAI.agent.Warp(__instance.exitPoint.position);
                        gameObject.transform.eulerAngles = new Vector3(0, __instance.exitPoint.eulerAngles.y, 0);
                    }
                }
            }
        }
    }
}
