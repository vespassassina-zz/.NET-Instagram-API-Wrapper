using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ApiBase;
using Instagram.api.classes;

namespace Instagram.api
{
    public class InstagramApiWrapper : Base
    {

        private static InstagramApiWrapper _sharedInstance = null;
        private static Configuration _sharedConfiguration = null;

        public Configuration Configuration {
            get { return _sharedConfiguration; }
            set { _sharedConfiguration = value; } 
        }
        private InstagramApiWrapper() {
        }

        public static InstagramApiWrapper GetInstance(Configuration configuration, ICache cache)
        {
            lock (threadlock)
            {
                if (_sharedInstance == null)
                {
                    _sharedInstance = new InstagramApiWrapper();
                    _cache = cache;
                    _sharedInstance.Configuration = configuration;
                }
            }

            return _sharedInstance;
        }
        public static InstagramApiWrapper GetInstance(Configuration configuration) {
            lock (threadlock) {
                if(_sharedInstance == null) {
                    _sharedInstance = new InstagramApiWrapper();
                    _sharedInstance.Configuration = configuration;

                }
            }

            return _sharedInstance;
        }
        public static InstagramApiWrapper GetInstance() {
            if (_sharedInstance == null) {
                if (_sharedConfiguration == null)
                    throw new ApplicationException("API Uninitialized");
                else
                    _sharedInstance = new InstagramApiWrapper();
            }
            return _sharedInstance;
        }

        #region auth
        public string AuthGetUrl(string scope){
            if (string.IsNullOrEmpty(scope))
                scope = "basic";
            return Configuration.AuthUrl + "?client_id=" + Configuration.ClientId + "&redirect_uri=" + Configuration.ReturnUrl + "&response_type=code&scope=" + scope;
        }
        public AccessToken AuthGetAccessToken(string code)
        {
            string json = RequestPostToUrl(Configuration.TokenRetrievalUrl, new NameValueCollection
                                                               {
                                                                       {"client_id" , Configuration.ClientId},
                                                                       {"client_secret" , Configuration.ClientSecret},
                                                                       {"grant_type" , "authorization_code"},
                                                                       {"redirect_uri" , Configuration.ReturnUrl},
                                                                       {"code" , code}
                                                               },Configuration.Proxy);

            if (!string.IsNullOrEmpty(json)) {
                AccessToken tk = AccessToken.Deserialize(json);
                return tk;
            }

            return null;
        }
        #endregion

        #region user
        public InstagramResponse<User> UserDetails(string userid, string accessToken)
        {

            if (userid == "self")
                return CurrentUserDetails(accessToken);

            string url = Configuration.ApiBaseUrl + "users/" + userid + "?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "users/" + userid + "?client_id=" + Configuration.ClientId;

            if (_cache != null)
                if (_cache.Exists(url))
                    return _cache.Get<InstagramResponse<User>>("users/" + userid);

            string json = RequestGetToUrl(url,Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User> res = DeserializeObject<InstagramResponse<User>>(json);

            if (!string.IsNullOrEmpty(accessToken)) {
                //CurrentUserIsFollowing(userid, accessToken);

                res.data.isFollowed = CurrentUserIsFollowing(res.data.id,accessToken);
            }

            if (_cache != null)
                _cache.Add("users/" + userid, res, 600);

            return res;
        }
        public InstagramResponse<User[]> UsersSearch(string query, string count, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/search?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "users/search?client_id=" + Configuration.ClientId;

            if (_cache != null)
                if (_cache.Exists(url))
                    return _cache.Get<InstagramResponse<User[]>>(url);

            if (!string.IsNullOrEmpty(query)) url = url + "&q=" + query;
            if (!string.IsNullOrEmpty(count)) url = url + "&count=" + count;
            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

            if (_cache != null)
                _cache.Add(url, res,300);

            return res;
        }
        public User[] UsersPopular(string accessToken)
        {
            InstagramMedia[] media = MediaPopular(accessToken,true).data;
            User[] users = UsersInMediaList(media);
            return users;
        }
        public InstagramResponse<InstagramMedia[]> UserRecentMedia(string userid, string min_id, string max_id, string count, string min_timestamp, string max_timestamp, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/" + userid + "/media/recent?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "users/" + userid + "/media/recent?client_id=" + Configuration.ClientId;
            
            if (!string.IsNullOrEmpty(min_id)) url = url + "&min_id=" + min_id;
            if (!string.IsNullOrEmpty(max_id)) url = url + "&max_id=" + max_id;
            if (!string.IsNullOrEmpty(count)) url = url + "&count=" + count;
            if (!string.IsNullOrEmpty(min_timestamp)) url = url + "&min_timestamp=" + min_timestamp;
            if (!string.IsNullOrEmpty(max_timestamp)) url = url + "&max_timestamp=" + max_timestamp;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);


            return res;
        }


        public InstagramResponse<User> CurrentUserDetails(string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/self?access_token=" + accessToken;

            if (_cache != null)
                if (_cache.Exists("users/self/" + accessToken))
                    return _cache.Get<InstagramResponse<User>>("users/self/" + accessToken);

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User> res = DeserializeObject<InstagramResponse<User>>(json);
            res.data.isSelf = true;

            if (_cache != null)
                _cache.Add("users/self/" + accessToken, res, 600);

            return res;
        }
        public InstagramResponse<InstagramMedia[]> CurrentUserRecentMedia(string min_id, string max_id, string count, string min_timestamp, string max_timestamp, string accessToken)
        {
            return UserRecentMedia("self", min_id, max_id, count, min_timestamp, max_timestamp, accessToken);
        }
        public InstagramResponse<InstagramMedia[]> CurrentUserFeed(string min_id, string max_id, string count, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/self/feed?access_token=" + accessToken;

            if (!string.IsNullOrEmpty(min_id)) url = url + "&min_id=" + min_id;
            if (!string.IsNullOrEmpty(max_id)) url = url + "&max_id=" + max_id;
            if (!string.IsNullOrEmpty(count)) url = url + "&count=" + count;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);

            return res;
        }
        public InstagramResponse<InstagramMedia[]> CurrentUserLikedMedia(string max_like_id, string count, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/self/media/liked?access_token=" + accessToken;
            if (!string.IsNullOrEmpty(max_like_id)) url = url + "&max_like_id=" + max_like_id;
            if (!string.IsNullOrEmpty(count)) url = url + "&count=" + count;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);

            return res;
        }
        public User[] UsersInMediaList(InstagramMedia[] media)
        {
            List<User> users = new List<User>();
            foreach (var instagramMedia in media)
            {
                if(!users.Contains(instagramMedia.user))
                    users.Add(instagramMedia.user);
            }

            return users.ToArray();
        }
        #endregion

        #region relationships
        public InstagramResponse<User[]> UserFollows(string userid, string accessToken, string max_user_id)
        {
            string url = Configuration.ApiBaseUrl + "users/" + userid + "/follows?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "users/" + userid + "/follows?client_id=" + Configuration.ClientId;

            if (!string.IsNullOrEmpty(max_user_id)) url = url + "&cursor=" + max_user_id;

            //if(_cache!=null) 
            //    if (_cache.Exists(userid + "/follows"))
            //        return _cache.Get<InstagramResponse<User[]>>(userid + "/follows");

            string json = RequestGetToUrl(url, Configuration.Proxy); 
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);
            //https://api.instagram.com/v1/users/530914/follows?access_token=530914.0c0b99a.56e7a173b9af43eba8a60759904f6fc4&cursor=32754039"
            //if (_cache != null)
            //    _cache.Add(userid + "/follows", res);

            return res;
        }
        public InstagramResponse<User[]> UserFollowedBy(string userid, string accessToken, string max_user_id)
        {
            string url = Configuration.ApiBaseUrl + "users/" + userid + "/followed-by?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "users/" + userid + "/followed-by?client_id=" + Configuration.ClientId;

            if (!string.IsNullOrEmpty(max_user_id)) url = url + "&cursor=" + max_user_id;

            //if (_cache != null)
            //    if (_cache.Exists(userid + "/followed-by"))
            //        return _cache.Get<InstagramResponse<User[]>>(userid + "/followed-by");

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

            //if (_cache != null)
            //    _cache.Add(userid + "/followed-by", res);

            return res;
        }

        public InstagramResponse<User[]> CurrentUserFollows(string accessToken, string max_user_id)
        {
            string url = Configuration.ApiBaseUrl + "users/self/follows?access_token=" + accessToken;

            if (!string.IsNullOrEmpty(max_user_id)) url = url + "&cursor=" + max_user_id;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

   

            return res;
        }
        public InstagramResponse<User[]> CurrentUserFollowedBy(string accessToken, string max_user_id)
        {
            string url = Configuration.ApiBaseUrl + "users/self/followed-by?access_token=" + accessToken;

            if (!string.IsNullOrEmpty(max_user_id)) url = url + "&cursor=" + max_user_id;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);


            return res;
        }
        public InstagramResponse<User[]> CurrentUserRequestedBy(string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/self/requested-by?access_token=" + accessToken;

     

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

       

            return res;
        }

        public InstagramResponse<Relation> CurrentUserRelationshipWith(string recipient_userid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/" + recipient_userid + "/relationship?access_token=" + accessToken;
            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<Relation> res = DeserializeObject<InstagramResponse<Relation>>(json);
            return res;
        }
        public void CurrentUserFollow(string userid, string[] recipient_userids, string accessToken)
        {
            foreach (var recipient_userid in recipient_userids)
            {
                CurrentUserFollow(userid,recipient_userid, accessToken);
            }
        }
        public bool CurrentUserFollow(string userid,string recipient_userid, string accessToken)
        {
            if (_cache != null)
            {
                _cache.Remove(userid + "/follows");
                _cache.Remove("users/self/" + accessToken);
            }

            return CurrentUserSetRelationship(userid, recipient_userid, "follow", accessToken).meta.code == "200";
        }
        public bool CurrentUserFollowToggle(string userid, string recipient_userid, string accessToken)
        {
            if (_cache != null) {
                _cache.Remove("self" + "/follows");
                _cache.Remove(userid + "/follows");
                _cache.Remove("users/" + recipient_userid);
                _cache.Remove("users/self/" + accessToken);
            }

            if (CurrentUserIsFollowing(recipient_userid,accessToken))
                return CurrentUserSetRelationship(userid,recipient_userid, "unfollow", accessToken).meta.code == "200";
            else
                return CurrentUserSetRelationship(userid,recipient_userid, "follow", accessToken).meta.code == "200";
        }
        public void CurrentUserUnfollow(string userid, string[] recipient_userids, string accessToken)
        {
            foreach (var recipient_userid in recipient_userids)
            {
                CurrentUserUnfollow(userid,recipient_userid, accessToken);
            }

            if (_cache != null)
                _cache.Remove(userid + "/follows");
        }
        public bool CurrentUserUnfollow(string userid, string recipient_userid, string accessToken)
        {
            if (_cache != null)
            {
                _cache.Remove(userid + "/follows");
                _cache.Remove("users/self/" + accessToken);
            }

            return CurrentUserSetRelationship(userid,recipient_userid, "unfollow", accessToken).meta.code == "200";
        }
        public bool CurrentUserBlock(string userid, string recipient_userid, string accessToken)
        {
            return CurrentUserSetRelationship(userid,recipient_userid, "block", accessToken).meta.code == "200";
        }
        public void CurrentUserApprove(string userid, string[] recipient_userids, string accessToken)
        {
            foreach (var recipient_userid in recipient_userids)
            {
                CurrentUserApprove(userid,recipient_userid, accessToken);
            }
        }
        public bool CurrentUserApprove(string userid, string recipient_userid, string accessToken)
        {
            return CurrentUserSetRelationship(userid,recipient_userid, "approve", accessToken).meta.code == "200";
        }
        public bool CurrentUserDeny(string userid, string recipient_userid, string accessToken)
        {
            return CurrentUserSetRelationship(userid,recipient_userid, "deny", accessToken).meta.code == "200";
        }
        public bool CurrentUserUnblock(string userid, string recipient_userid, string accessToken)
        {
            return CurrentUserSetRelationship(userid,recipient_userid,"unblock",accessToken).meta.code == "200";
        }
        public InstagramResponse<Relation> CurrentUserSetRelationship(string userid, string recipient_userid, string relationship_key, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "users/" + recipient_userid + "/relationship?access_token=" + accessToken;
            url = url + "&action=" + relationship_key;
            string json = RequestPostToUrl(url, new NameValueCollection { { "action", relationship_key } }, Configuration.Proxy);
            if (string.IsNullOrEmpty(json)) //error
                return new InstagramResponse<Relation>{meta = new Metadata{code = "400"}};

            InstagramResponse<Relation> res = DeserializeObject<InstagramResponse<Relation>>(json);

            if (_cache != null)
            {
                _cache.Remove("users/self/" + accessToken);
                _cache.Remove(userid + "/follows");
                _cache.Remove("users/" + recipient_userid);
            }

            return res;
        }

        public bool CurrentUserIsFollowing( string recipient_userid, string accessToken)
        {
            //outgoing_status: Your relationship to the user: "follows", "requested", "none". 
            //incoming_status: A user's relationship to you : "followed_by", "requested_by", "blocked_by_you", "none".
            Relation r = CurrentUserRelationshipWith(recipient_userid, accessToken).data;
            if (r.outgoing_status == "follows")
                return true;
            return false;
        }
        public bool CurrentUserIsFollowedBy( string recipient_userid, string accessToken)
        {
            //outgoing_status: Your relationship to the user: "follows", "requested", "none". 
            //incoming_status: A user's relationship to you : "followed_by", "requested_by", "blocked_by_you", "none".
            Relation r = CurrentUserRelationshipWith(recipient_userid, accessToken).data;
            if (r.incoming_status == "followed_by")
                return true;
            return false;
        }
        #endregion

        #region media
        public InstagramResponse<InstagramMedia> MediaDetails(string mediaid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "media/" + mediaid + "?client_id=" + Configuration.ClientId;

            if (_cache != null)
                if (_cache.Exists("media/" + mediaid))
                    return _cache.Get<InstagramResponse<InstagramMedia>>("media/" + mediaid);

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia> res = DeserializeObject<InstagramResponse<InstagramMedia>>(json);
            if (_cache != null)
                _cache.Add("media/" + mediaid, res, 60);
            
            return res;
        }
        public InstagramResponse<InstagramMedia[]> MediaSearch(string lat, string lng, string distance, string min_timestamp, string max_timestamp, string accessToken)
        {
            if (!string.IsNullOrEmpty(lat) && string.IsNullOrEmpty(lng) || !string.IsNullOrEmpty(lng) && string.IsNullOrEmpty(lat))
                throw new ArgumentException("if lat or lng are specified, both are required.");

            string url = Configuration.ApiBaseUrl + "media/search?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "media/search?client_id=" + Configuration.ClientId;

            if (!string.IsNullOrEmpty(lat)) url = url + "&lat=" + lat;
            if (!string.IsNullOrEmpty(lng)) url = url + "&lng=" + lng;
            if (!string.IsNullOrEmpty(distance)) url = url + "&distance=" + distance;
            if (!string.IsNullOrEmpty(min_timestamp)) url = url + "&min_timestamp=" + min_timestamp;
            if (!string.IsNullOrEmpty(max_timestamp)) url = url + "&max_timestamp=" + max_timestamp;

            if (_cache != null)
                if (_cache.Exists(url))
                    return _cache.Get<InstagramResponse<InstagramMedia[]>>(url);

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);

            if (_cache != null)
                _cache.Add(url, res, 60);
            
            return res;
        }
        public InstagramResponse<InstagramMedia[]> MediaPopular(string accessToken, bool usecache)
        {
            string url = Configuration.ApiBaseUrl + "media/popular?access_token=" + accessToken;
            if(string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "media/popular?client_id=" + Configuration.ClientId;

            if (_cache != null && usecache)
                if (_cache.Exists("media/popular"))
                    return _cache.Get<InstagramResponse<InstagramMedia[]>>("media/popular" );

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);
            if (_cache != null )
                _cache.Add("media/popular" , res, 600);

            return res;
        }
        #endregion

        #region comments
        public InstagramResponse<Comment[]> Comments(string mediaid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "/comments?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "media/" + mediaid + "/comments?client_id=" + Configuration.ClientId;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if(string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<Comment[]> res = DeserializeObject<InstagramResponse<Comment[]>>(json);
            return res;
        }
        public void CommentAdd(string[] mediaids, string text, string accessToken)
        {
            foreach (var mediaid in mediaids)
            {
                CommentAdd(mediaid, text,accessToken);
            }
        }
        public bool CommentAdd(string mediaid, string text, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "/comments?access_token=" + accessToken;
            NameValueCollection post = new NameValueCollection
                                       {
                                               {"text",text},
                                               {"access_token", accessToken}
                                       };
            string json = RequestPostToUrl(url, post, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return true;

            InstagramResponse<Comment> res = DeserializeObject<InstagramResponse<Comment>>(json);

            if (_cache != null)
                _cache.Remove("media/" + mediaid);

            return res.meta.code == "200";
        }
        public bool CommentDelete(string mediaid, string commentid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "/comments/"+ commentid +"?access_token=" + accessToken;
            string json = RequestDeleteToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return false;

            InstagramResponse<Comment> res = DeserializeObject<InstagramResponse<Comment>>(json);

            if (_cache != null)
                _cache.Remove("media/" + mediaid);

            return res.meta.code == "200";
        } 
        #endregion

        #region likes
        public InstagramResponse<User[]> Likes(string mediaid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "/likes?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "media/" + mediaid + "/likes?client_id=" + Configuration.ClientId;

            if (_cache != null)
                if (_cache.Exists("media/" + mediaid + "/likes"))
                    return _cache.Get<InstagramResponse<User[]>>("media/" + mediaid + "/likes");

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

            if (_cache != null)
                _cache.Add("media/" + mediaid + "/likes", res, 60);

            return res;
        }
        public void LikeAdd(string[] mediaids, string accessToken) {
            foreach(var mediaid in mediaids) {
                LikeAdd(mediaid,null, accessToken);
            }
        }
        public bool LikeAdd(string mediaid,string userid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "/likes?access_token=" + accessToken;
            NameValueCollection post = new NameValueCollection
                                       {
                                               {"access_token", accessToken}
                                       };
            string json = RequestPostToUrl(url, post, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return true;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

            if (_cache != null) {
                _cache.Remove("media/" + mediaid);
                _cache.Remove("media/popular" );
                _cache.Remove("media/" + mediaid + "/likes");
                _cache.Remove("users/self/" + accessToken);
                _cache.Remove("users/" + userid);
                if (!string.IsNullOrEmpty(userid)) {
                    _cache.Remove("users/" + userid + "/media/recent");
                    _cache.Remove("users/self/feed?access_token=" + accessToken);
                }
            }

            return res.meta.code == "200";
        }
        public void LikeDelete(string[] mediaids, string accessToken)
        {
            foreach (var mediaid in mediaids)
            {
                LikeDelete(mediaid,null, accessToken);
            }
        }
        public bool LikeDelete(string mediaid, string userid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "media/" + mediaid + "/likes?access_token=" + accessToken;

            string json = RequestDeleteToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return true;

            InstagramResponse<User[]> res = DeserializeObject<InstagramResponse<User[]>>(json);

            if (_cache != null)
            {
                _cache.Remove("media/popular");
                _cache.Remove("media/" + mediaid);
                _cache.Remove("media/" + mediaid + "/likes");
                _cache.Remove("users/self/" + accessToken);
                _cache.Remove("users/" + userid);
                if (!string.IsNullOrEmpty(userid))
                {
                    _cache.Remove("users/" + userid + "/media/recent");
                    _cache.Remove("users/self/feed?access_token=" + accessToken);
                }
            }

            return res.meta.code == "200";
        }
        public bool LikeToggle(string mediaid, string userid, string accessToken) {

            InstagramMedia media = MediaDetails(mediaid, accessToken).data;

            if (media.user_has_liked)
                return LikeDelete(mediaid, userid, accessToken);
            else
                return LikeAdd(mediaid, userid, accessToken);
            
        }
        public bool UserIsLiking(string mediaid, string userid, string accessToken)
        {

            User[] userlinking = Likes(mediaid, accessToken).data;
            foreach(User user in userlinking) 
                if (user.id.ToString().Equals( userid))
                    return true;

            return false;
        }
        #endregion

        #region tags
        public InstagramResponse<Tag> TagDetails(string tagname, string accessToken)
        {
            if (tagname.Contains("#"))
                tagname = tagname.Replace("#", "");

            string url = Configuration.ApiBaseUrl + "tags/" + tagname + "?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "tags/" + tagname + "?client_id=" + Configuration.ClientId;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<Tag> res = DeserializeObject<InstagramResponse<Tag>>(json);
            return res;
        }
        public InstagramResponse<Tag[]> TagSearch(string query, string accessToken)
        {
            if (query.Contains("#"))
                query = query.Replace("#", "");

            string url = Configuration.ApiBaseUrl + "tags/search?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "tags/search?client_id=" + Configuration.ClientId;

            if (!string.IsNullOrEmpty(query)) url = url + "&q=" + query;

            if (_cache != null)
                if (_cache.Exists(url))
                    return _cache.Get<InstagramResponse<Tag[]>>(url);

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<Tag[]> res = DeserializeObject<InstagramResponse<Tag[]>>(json);

            if (_cache != null)
                _cache.Add(url, res, 300);

            return res;
        }
        public Tag[] TagPopular(string accessToken) {
            InstagramMedia[] pop = MediaPopular(accessToken,true).data;
            return TagsInMediaList(pop);
        }
        public InstagramResponse<InstagramMedia[]> TagMedia(string tagname, string min_id, string max_id, string accessToken)
        {
            if (tagname.Contains("#"))
                tagname = tagname.Replace("#", "");

            string url = Configuration.ApiBaseUrl + "tags/" + tagname + "/media/recent?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "tags/" + tagname + "/media/recent?client_id=" + Configuration.ClientId;


            if (!string.IsNullOrEmpty(min_id)) url = url + "&min_id=" + min_id;
            if (!string.IsNullOrEmpty(max_id)) url = url + "&max_id=" + max_id;

            if (_cache != null)
                if (_cache.Exists(url))
                    return _cache.Get<InstagramResponse<InstagramMedia[]>>(url);


            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);

            if (_cache != null)
                _cache.Add(url, res, 300);
            
            return res;
        }
        public static Tag[] TagsInMediaList(InstagramMedia[] media) {
            List<string> t = new List<string>();
            foreach(var instagramMedia in media) {
                foreach(string tag in instagramMedia.tags) {
                    if(!t.Contains(tag))
                        t.Add(tag);
                }
            }

            return TagsFromStrings(t.ToArray());
        }
        public static Tag[] TagsFromStrings(string[] tags)
        {
            List<Tag> taglist = new List<Tag>(tags.Length);
            foreach (string s in tags)
            {
                Tag tag = new Tag
                {
                    media_count = 0,
                    name = s
                };
                taglist.Add(tag);
            }
            return taglist.ToArray();
        }
        #endregion

        #region locations
        public Location LocationDetails(string locationid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "locations/" + locationid + "?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "locations/" + locationid + "?client_id=" + Configuration.ClientId;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return null;

            InstagramResponse<Location> res = DeserializeObject<InstagramResponse<Location>>(json);
            return res.data;
        }
        public InstagramMedia[] LocationMedia(string locationid,string min_id,string max_id, string min_timestamp, string max_timestamp, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "locations/" + locationid + "/media/recent?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "locations/" + locationid + "/media/recent?client_id=" + Configuration.ClientId;
            
            if (!string.IsNullOrEmpty(min_id)) url = url + "&min_id=" + min_id;
            if (!string.IsNullOrEmpty(max_id)) url = url + "&max_id=" + max_id;
            if (!string.IsNullOrEmpty(min_timestamp)) url = url + "&min_timestamp=" + min_timestamp;
            if (!string.IsNullOrEmpty(max_timestamp)) url = url + "&max_timestamp=" + max_timestamp;
            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return new InstagramMedia[0];

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);
            return res.data;
        }
        public Location[] LocationSearch(string lat, string lng, string foursquare_id, string foursquare_v2_id, string distance, string accessToken)
        {
            if (!string.IsNullOrEmpty(lat) && string.IsNullOrEmpty(lng) || !string.IsNullOrEmpty(lng) && string.IsNullOrEmpty(lat))
                throw new ArgumentException("if lat or lng are specified, both are required.");

            string url = Configuration.ApiBaseUrl + "locations/search?access_token=" + accessToken;
            if (string.IsNullOrEmpty(accessToken))
                url = Configuration.ApiBaseUrl + "locations/search?client_id=" + Configuration.ClientId;

            if (!string.IsNullOrEmpty(lat)) url = url + "&lat=" + lat;
            if (!string.IsNullOrEmpty(lng)) url = url + "&lng=" + lng;
            if (!string.IsNullOrEmpty(foursquare_id)) url = url + "&foursquare_id=" + foursquare_id;
            if (!string.IsNullOrEmpty(foursquare_v2_id)) url = url + "&foursquare_v2_id=" + foursquare_v2_id;
            if (!string.IsNullOrEmpty(distance)) url = url + "&distance=" + distance;

            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return new Location[0];


            InstagramResponse<Location[]> res = DeserializeObject<InstagramResponse<Location[]>>(json);
            return res.data;
        }
        #endregion

        #region geography
        public InstagramMedia[] GeographyMedia(string geographyid, string accessToken)
        {
            string url = Configuration.ApiBaseUrl + "geographies/" + geographyid + "/media/recent?access_token=" + accessToken;
            string json = RequestGetToUrl(url, Configuration.Proxy);
            if (string.IsNullOrEmpty(json))
                return new InstagramMedia[0];

            InstagramResponse<InstagramMedia[]> res = DeserializeObject<InstagramResponse<InstagramMedia[]>>(json);
            return res.data;
        }
        #endregion



        
    }
}
