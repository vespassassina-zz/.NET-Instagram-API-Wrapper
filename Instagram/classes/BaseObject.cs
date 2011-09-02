


namespace Instagram.api.classes
{
    [System.Serializable]
    public class InstagramBaseObject {
        protected InstagramApiWrapper InstagramApi { get { return InstagramApiWrapper.GetInstance(); } }
    }
}
