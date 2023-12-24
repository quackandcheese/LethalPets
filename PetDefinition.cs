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
        public string description;
        public int price;
        public GameObject prefab;
        //public List<string> terminalWords = new List<string>();
    }
}
