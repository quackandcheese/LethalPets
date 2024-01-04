using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace LethalPets
{
    public static class PetManager
    {
        public static List<PetDefinition> petDefinitions = new List<PetDefinition>();
        public static List<GameObject> spawnedPets = new List<GameObject>();

        public static void RegisterPet(PetDefinition petDefinition)
        {
            petDefinitions.Add(petDefinition);
            //TerminalCommands.CreatePetCommand(petDefinition);
        }


        public static string SpawnPet(PetDefinition pet)
        {
            GameObject petObject;

            if (pet)
            {
                if (pet.prefab)
                {
                    petObject = GameObject.Instantiate<GameObject>(pet.prefab, StartOfRound.Instance.elevatorTransform.position + (Vector3.up * 1), Quaternion.identity, null);
                    spawnedPets.Add(petObject);

                    if (!petObject.GetComponent<NetworkObject>().IsSpawned)
                    {
                        petObject.GetComponent<NetworkObject>().Spawn(false);
                    }

                    if (petObject.TryGetComponent<PetAI>(out var petAI)) 
                    {
                        petAI.ownerPlayer = GameNetworkManager.Instance.localPlayerController;
                    }

                    return $"{pet.petName} has been purchased! Treat them well!" + "\n\n";
                }
            }

            return "Unable to purchase pet." + "\n\n";
        }
    }
}
