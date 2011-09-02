using System;

namespace Instagram.api.classes{
    [Serializable]
    public class LikesList : InstagramBaseObject
    {
        public int count;
        public User[] data;
    }
}