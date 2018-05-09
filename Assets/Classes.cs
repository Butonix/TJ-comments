using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Site {
	public string name;
	public Sprite logo;
	public Rect logoPos;
	public Sprite header;
	public Color color;
	public bool login;
}

public class ErrorClass {
	int code;
	string message;
}

//Profile
[System.Serializable]
class ProfileMain {
	public Profile result;
	public ErrorClass error;
}

[System.Serializable]
class Profile {
	public int id;
	public string name;
	public string url;
	public ProfileAccess advanced_access;
	public Counters counters;
}

[System.Serializable]
class ProfileAccess {
	public bool is_needs_advanced_access;
	public TJSubscription tj_subscription;
}

[System.Serializable]
class TJSubscription {
	public bool is_active;
}
[System.Serializable]
class Counters {
	public int comments;
}

//Comments
[System.Serializable]
class CommentsMain {
	public List<Comment> result;
	public ErrorClass error;
}
[System.Serializable]
public class Comment {
	public int id;
}

//Comment likes

[System.Serializable]
public class UserLike {
	public int sign;
	public string user_name;
	public string avatar_url;
	public string user_url;
	public Texture2D t;
	public RectTransform[] r =  new RectTransform[3]{null, null, null};
	public User[] objs =  new User[3]{null, null, null};
	public int likes;
	public int dislikes;
}

[System.Serializable]
public class UserLikeMain {
	public Dictionary<int, UserLike> data = new Dictionary<int, UserLike>();
	//public List<UserLike> data;
	public ErrorClass error;
}
