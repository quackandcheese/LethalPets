using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LethalPets
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInProcess("Lethal Company.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "quackandcheese.lethalpets";
        public const string ModName = "LethalPets";
        public const string ModVersion = "1.0.0";

        public static AssetBundle Bundle;

        public static ConfigFile config;

        public static BepInEx.Logging.ManualLogSource logger;
        private void Awake()
        {
            Bundle = QuickLoadAssetBundle("lethalpets.assets");
            logger = Logger;
            config = Config;

            Harmony harmony = new Harmony(ModGUID);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            TerminalCommands.Create();

            RegisterPets();
        }

        public void RegisterPets()
        {
            PetManager.RegisterPet(Bundle.LoadAsset<PetDefinition>("Assets/LethalPets/Pets/Cat/CatDefinition.asset"));
        }


        #region HELPERS
        public static T FindAsset<T>(string name = "") where T : UnityEngine.Object
        {
            if (name == "")
                return Resources.FindObjectsOfTypeAll<T>().First();
            else
                return Resources.FindObjectsOfTypeAll<T>().First(x => x.name == name);
        }

        public static AssetBundle QuickLoadAssetBundle(string assetBundleName)
        {
            string AssetBundlePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), assetBundleName);

            return AssetBundle.LoadFromFile(AssetBundlePath);
        }
        #endregion
    }
}
