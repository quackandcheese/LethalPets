using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminalApi;
using UnityEngine;
using static System.Data.Odbc.ODBC32;
using static TerminalApi.TerminalApi;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace LethalPets
{
    public class TerminalCommands
    {
        public static TerminalKeyword adoptKeyword = CreateTerminalKeyword("adopt", true);

        public static void Create()
        { 
            TerminalNode petNode = CreateTerminalNode("Opening Pet Store...\n", true, "openpetstore");
            TerminalKeyword petsKeyword = CreateTerminalKeyword("pets", triggeringNode: petNode);
            AddTerminalKeyword(petsKeyword);
        }

        public static string CreatePetStore()
        {
            string displayText = "PET STORE" + "\n\n";
            displayText += "To adopt a pet, type ADOPT before its name.\n";
            displayText += "To learn more about a pet, type INFO before its name.\n";
            displayText += "____________________________" + "\n\n\n";

            foreach (PetDefinition petEntry in PetManager.petDefinitions)
            {
                displayText += $"* {petEntry.petName}  //  Price: ${petEntry.price}\n";
            }

            displayText += "\n";

            return displayText;
        }

        public static void CreatePetCommand(PetDefinition petDef)
        {
            string simpleName = petDef.petName.ToLower().Trim();
            TerminalNode petNode = CreateTerminalNode($"Do you really want to buy {petDef.petName}?", true, simpleName);
            TerminalKeyword petNameKeyword = CreateTerminalKeyword(simpleName);
            adoptKeyword = adoptKeyword.AddCompatibleNoun(petNameKeyword, petNode);
            petNameKeyword.defaultVerb = adoptKeyword;

            AddTerminalKeyword(petNameKeyword);
            UpdateKeyword(adoptKeyword);
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.RunTerminalEvents))]
    public class RunTerminalEventsPatch : MonoBehaviour
    {
        static IEnumerator PostfixCoroutine(Terminal __instance, TerminalNode node)
        {
            if (string.IsNullOrWhiteSpace(node.terminalEvent))
            {
                yield break;
            }

            if (node.terminalEvent == "openpetstore")
                node.displayText = TerminalCommands.CreatePetStore();

            foreach (PetDefinition petDef in PetManager.petDefinitions)
            {
                if (node.terminalEvent == petDef.petName.ToLower().Trim())
                {
                    if (__instance.groupCredits >= petDef.price)
                    {
                        node.displayText = PetManager.SpawnPet(petDef);
                        __instance.groupCredits = Mathf.Clamp(__instance.groupCredits - petDef.price, 0, 10000000);

                        if (__instance.IsServer)
                        {
                            __instance.SyncGroupCreditsClientRpc(__instance.groupCredits, __instance.numberOfItemsInDropship);
                        }
                    }
                    else
                    {
                        __instance.LoadNewNode(__instance.terminalNodes.specialNodes[2]);
                    }
                }
            }

            yield break;
        }

        static void Postfix(Terminal __instance, TerminalNode node)
        {
            __instance.StartCoroutine(PostfixCoroutine(__instance, node));
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake))]
    public class TerminalStartPatch
    {
        static void Postfix()
        {
            foreach(PetDefinition petDef in PetManager.petDefinitions)
            {
                string simpleName = petDef.petName.ToLower().Trim();
                string infoText = $"{petDef.petName}\n\nSpecies: {petDef.species}\n\nDescription: {petDef.description}\n\nPrice: ${petDef.price}\n\n";
                TerminalNode infoNode = CreateTerminalNode(infoText, true);
                AddCompatibleNoun("info", simpleName, infoNode);
            }
        }
    }
}
