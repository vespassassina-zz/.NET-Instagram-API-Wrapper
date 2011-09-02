using System;

namespace Instagram.api.classes
{
    [Serializable]
    public class InstagramResponse<T> {
        public Pagination pagination;
        public Metadata meta;
        public T data;
    }
}
