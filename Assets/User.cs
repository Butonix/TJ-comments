using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class User : MonoBehaviour {

	string url;
	public Text username;
	public Text rating;
	public RawImage avatar;
	bool first = false;
	int key;
	// Use this for initialization
	public void SetData(KeyValuePair<int, UserLike> u, int s){
		if (!first){
			username.text = u.Value.user_name;
			url = u.Value.user_url;
			Color c;
			if (ColorUtility.TryParseHtmlString(s == 0 ? "#000000FF" : (s == 1 ? "#07A23BFF" : "#CD192EFF"), out c))
			rating.color = c;
			if (u.Value.t == null){
				StartCoroutine(LoadAvatar(u.Value.avatar_url));
			}
			else {
				avatar.texture = u.Value.t;
			}
			first = true;
			key = u.Key;
		}
		rating.text = (s == 0 ? u.Value.likes-u.Value.dislikes : (s == 1 ? u.Value.likes : -u.Value.dislikes)).ToString();
	}

	IEnumerator LoadAvatar(string aUrl){
        using (WWW www = new WWW(aUrl)) {
			yield return www;
			Texture2D tex = new Texture2D(1, 1);
		    www.LoadImageIntoTexture(tex);
		    tex = ScaleTexture(tex, 128,128);
		    tex.Compress(true);
 			avatar.texture = tex;
 			Main.use.users[key].t = tex;
 			Destroy(www.texture);
		}
    }

	public void OpenUser(){
		Application.OpenURL(url);
	}

	Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
	     Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,false);
	     float incX=(1.0f / (float)targetWidth);
	     float incY=(1.0f / (float)targetHeight);
	     for (int i = 0; i < result.height; ++i) {
	         for (int j = 0; j < result.width; ++j) {
	             Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
	             result.SetPixel(j, i, newColor);
	         }
	     }
	     result.Apply();
	     return result;
	 }
}
