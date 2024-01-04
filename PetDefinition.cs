using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalPets
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Pet")]
    public class PetDefinition : ScriptableObject
    {
        public string petName;
        public string species;
        [TextArea] public string description;
        public int price;
        public GameObject prefab;
        [HideInInspector]
        public string SimpleName => petName.Trim().ToLower();
        //public List<string> terminalWords = new List<string>();
    }
}
