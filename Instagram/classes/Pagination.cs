using System;

namespace Instagram.api.classes{
    [Serializable]
    public class Pagination : InstagramBaseObject {
        public string next_url;
        public string next_max_id;
        public string next_max_like_id;
    }
}