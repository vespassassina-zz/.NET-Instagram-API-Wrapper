using System;

namespace Instagram.api.classes{


    [Serializable]
    public class Comment : InstagramBaseObject
    {
        public double created_time;
        public string text;
        public string id;
        public User from;
        public bool owncomment = false;
    }
}