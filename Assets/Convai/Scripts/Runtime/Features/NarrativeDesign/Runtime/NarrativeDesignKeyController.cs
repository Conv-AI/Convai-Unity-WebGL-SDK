using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    public class NarrativeDesignKeyController : MonoBehaviour
    {
        public NarrativeDesignKeysContainer narrativeDesignKeyController;
        
        [Serializable]
        public class NarrativeDesignKey
        {
            public string name;
            public string value;
        }
        
        [Serializable]
        public class NarrativeDesignKeysContainer
        {
            public List<NarrativeDesignKey> narrativeDesignKeys;
        }
        public void SetTemplateKey(Dictionary<string, string> keyValuePairs)
        {
            narrativeDesignKeyController.narrativeDesignKeys.Clear();
            narrativeDesignKeyController.narrativeDesignKeys.AddRange(from item in keyValuePairs
                select new NarrativeDesignKey { name = item.Key, value = item.Value });
        }
        public void AddTemplateKey(string name, string value)
        {
            narrativeDesignKeyController.narrativeDesignKeys.Add(new NarrativeDesignKey { name = name, value = value });
        }
        public void RemoveTemplateKey(string name)
        {
            NarrativeDesignKey reference = narrativeDesignKeyController.narrativeDesignKeys.Find(x => x.name == name);
            if(reference == null) return;
            narrativeDesignKeyController.narrativeDesignKeys.Remove(reference);
        }
        public void UpdateTemplateKey(string name, string value)
        {
            NarrativeDesignKey reference = narrativeDesignKeyController.narrativeDesignKeys.Find(x => x.name == name);
            if (reference == null) return;
            reference.value = value;
        }
        
     
    }
}