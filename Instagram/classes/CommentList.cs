using System;

namespace Instagram.api.classes{
    [Serializable]
    public class CommentList : InstagramBaseObject
    {
        public int count;
        public Comment[] data;
    }
}