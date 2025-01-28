using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class ActionConfig
{
    public List<string> actions= new List<string>();
    public List<Types.Character> characters = new List<Types.Character>();
    public List<Types.Object> objects= new List<Types.Object>();
    public string classification;
    public int contextLevel;
    public string currentAttentionObject;

    [Serializable]
    public static class Types
    {
        [Serializable]
        public struct Character
        {
            public string name;
            public string bio;
        }
        [Serializable]
        public struct Object
        {
            public string name;
            public string description;
        }
    }
}
