using System;

namespace Instagram.api.classes{
    [Serializable]
    public class Tag : InstagramBaseObject
    {
        public string name;
        public int media_count;
    }
}