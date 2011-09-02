
using System;
using System.Text;
using Instagram.api.classes;

namespace Instagram.api.classes
{
    [Serializable]
    public class Counts : InstagramBaseObject
    {
        public int media;
        public int follows;
        public int followed_by;

    }
}
