using System;

namespace Instagram.api.classes{
    [Serializable]
    public class ImageLink : InstagramBaseObject
    {
        public string url;
        public int width;
        public int height;
    }
}