using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using ZXing;
using ZXing.QrCode;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Main : MonoBehaviour {
	public Site[] sites;
	string xDeviceToken;
	public GameObject menuBtn;
	public Image header;
	public GameObject loginBtn;
	public GameObject enter;
	public InputField enterID;
	public InputField limitInput;
	public GameObject loadingObj;
	public GameObject user;
	float targetLoading = 0;
	public Image loading;
	public Text loadText;
	public Text allInfo;
	Profile profile = new Profile();
	public GameObject Info;
	public RectTransform[] userGroups;
	string answer;
	static public Main use;
	public List<Comment> comments;
	public int limit;
	int cSite;
	string cID;
	public Dictionary<int, UserLike> users = new Dictionary<int, UserLike>();

	void Start (){
		use = this;
	}

	public void Restart(){
		Application.LoadLevel(0);
	}

	public void SelectSite(int v){
		cSite = v;
		header.sprite = sites[v].header;
		RectTransform logo = header.transform.Find("Logo").GetComponent<RectTransform>();
		menuBtn.SetActive(true);
		logo.GetComponent<Image>().sprite = sites[v].logo;
		logo.anchoredPosition = new Vector2(sites[v].logoPos.x, sites[v].logoPos.y);
		logo.sizeDelta = new Vector2(sites[v].logoPos.width, sites[v].logoPos.height);
		header.transform.Find("active").GetComponent<Image>().color = sites[v].color;
		header.gameObject.SetActive(true);
		GameObject.Find("Sites").SetActive(false);
		loading.color = sites[v].color;
		if (sites[v].login){
			xDeviceToken = PlayerPrefs.GetString("xtoken");
			if (xTokenStat()){
				Enter();
			}
			else {
				loginBtn.SetActive(true);
			}
		}
		else {
			Enter();
		}
	}

	void Update (){
		loading.fillAmount = Mathf.MoveTowards(loading.fillAmount, targetLoading, Time.deltaTime/2);
	}

	public void Login (){
		SimpleFileBrowser.FileBrowser.ShowLoadDialog((path) => {
				StartCoroutine(StartLogin(path));
			}, 
			null, false, null, "Select File", "Select" );
	}
	
	IEnumerator StartLogin (string v, string token = ""){
		Debug.Log(v);
        if (File.Exists(v)){
        	Texture2D tex = null;
        	byte[] fileData;
            fileData = File.ReadAllBytes(v);
            tex = new Texture2D(1, 1);
            tex.LoadImage(fileData);
			IBarcodeReader barcodeReader = new BarcodeReader ();
			var result = barcodeReader.Decode(tex.GetPixels32(), tex.width, tex.height);
	      	if (result != null) {
	      		Debug.Log("QR: "+ result.Text);
	      		string[] resultText = result.Text.Split('|');
	      		if (resultText.Length > 1){
		      		yield return StartCoroutine(Load("https://api."+sites[cSite].name+"/v1.3/auth/qr", resultText[1]));
		      		if (SetProfile(answer)){
		      			//StartCoroutine(Continue());
		      			Enter();
		      		}
		    	}
	    	}
    	}
	}

	bool SetProfile (string v){
		ProfileMain cAnswer = JsonUtility.FromJson<ProfileMain>(v);
  		if (cAnswer.error == null && cAnswer.result != null){
  			profile = cAnswer.result;
      		return true;
  		}
  		return false;
	}

	void Enter (){
		loginBtn.SetActive(false);
		enter.SetActive(true);
		if (sites[cSite].login){
			enterID.GetComponent<RectTransform>().anchoredPosition = new Vector2(-135, 0);
			limitInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(-135, -50);
			enter.transform.Find("Title").GetComponent<RectTransform>().anchoredPosition = new Vector2(-135, -205);
			enter.transform.Find("My").gameObject.SetActive(true);
		}
	}

	public void SetMyID(){
		enterID.text = "me";
	}

	public void Action(){
		cID = enterID.text;
		enter.SetActive(false);
		limit = string.IsNullOrEmpty(limitInput.text) ? 0 : Convert.ToInt32(limitInput.text);
		StartCoroutine(Continue());
	}

	IEnumerator Continue(){
		bool error = false;
		if (profile.id == 0 || cID != "me" && profile.id.ToString() != cID){
			yield return StartCoroutine(Load("https://api."+sites[cSite].name+"/v1.3/user/"+cID));
			SetProfile(answer);
		}
		loadingObj.SetActive(true);
		comments = new List<Comment>();
		int commentCount = limit != 0 && limit < profile.counters.comments ? limit: profile.counters.comments;
		for (int i = 0; i < commentCount; i+=50){
			loadText.text = "Загрузка комментариев: "+ (commentCount > i+50 ? i+50: commentCount) + "/"+ commentCount + LastTime(commentCount, i, 0);
			targetLoading = (i+50f)/commentCount;
			yield return StartCoroutine(Load("https://api."+sites[cSite].name+"/v1.3/user/"+profile.id+"/comments?count="+(commentCount > i+50 ? 50: commentCount-i)+"&offset="+i));
			try {
				CommentsMain cAnswer = JsonUtility.FromJson<CommentsMain>(answer);
				if (cAnswer.error == null){
					comments = comments.Union(cAnswer.result).ToList();
				}
			} catch {
				i-=50;
				loadText.text+="\nОшибка 503. Ожидание 5 секунд...";
				error = true;
			}
			yield return new WaitForSeconds(error ? 5f: 0.75f);
			error = false;
		}
		loading.fillAmount = 0;
		Info.SetActive(true);
		for (int c = 0; c < comments.Count; c++){
			loadText.text = "Загрузка лайков комментария: "+ (c+1) + "/"+ comments.Count + LastTime(commentCount, commentCount, c);
			targetLoading = (c+1f)/comments.Count;
			yield return StartCoroutine(Load("https://"+sites[cSite].name+"/vote/get_likers?id="+comments[c].id+"&mode=raw&type=comment"));
			try {
				UserLikeMain cAnswer = JsonConvert.DeserializeObject<UserLikeMain>(answer);
				if (cAnswer.data.Count > 0){
					foreach (KeyValuePair<int, UserLike> l in cAnswer.data){
						if (!users.ContainsKey(l.Key)){
							users.Add(l.Key, l.Value);
						}
						if (l.Value.sign == 1){
							users[l.Key].likes++;
						}
						else {
							users[l.Key].dislikes++;
						}
					}
					MainDisplay(c);
				}
			} catch (Exception e){
				if (e.Message.Contains("Unexpected")){
					loadText.text+="\nОшибка 503. Ожидание 5 секунд...";
					c--;
					error = true;
				}
			}
			yield return new WaitForSeconds(error ? 5f: 0.75f);
			error = false;
		}
		loadingObj.SetActive(false);
	}

	void MainDisplay(int c){
		allInfo.text = string.Format("За последние {0} комментариев пользователя \"{1}\" оценили {2} человек", c+1, profile.name, users.Count);
		users = users.OrderBy(x => -x.Value.likes+x.Value.dislikes).ToDictionary(x => x.Key, x => x.Value);
		Display(0);
		users = users.OrderBy(x => -x.Value.likes).ToDictionary(x => x.Key, x => x.Value);
		Display(1);
		users = users.OrderBy(x => -x.Value.dislikes).ToDictionary(x => x.Key, x => x.Value);
		Display(2);
	}

	void Display(int v){
		float yBlock = 1940;
		int c = 0;
		foreach (KeyValuePair<int, UserLike> u in users){
			if (v == 0 ||v == 1 && u.Value.likes != 0 || v == 2 && u.Value.dislikes != 0){
				if (c < 50){
					if (u.Value.objs[v] == null){
						GameObject uObj = Instantiate(user);
						uObj.transform.parent = userGroups[v];
						u.Value.r[v] = uObj.GetComponent<RectTransform>();
						u.Value.r[v].localScale = new Vector3(1, 1, 1);
						u.Value.objs[v] = uObj.GetComponent<User>();
					}
					u.Value.r[v].anchoredPosition = new Vector2(0, yBlock);
					u.Value.objs[v].SetData(u, v);
					yBlock-=40;
					c++;
					//Debug.Log(u.Value.user_name + "\nЛайков: "+ u.Value.likes + ", Дизлайков: "+ u.Value.dislikes);
				}
				else if (u.Value.objs[v] != null){
					Destroy(u.Value.objs[v].gameObject);
				}
			}
		}
	}

	string LastTime(int all, int c, int l, string t = " (осталось "){
		bool p = false;
		all = (int)(all+Mathf.Floor(all/50)-c/50-l);
		int h = (int)Mathf.Ceil(all/3600);
		int m = (int)Mathf.Ceil((all-h*60)/60);
		int s = (int)Mathf.Ceil((all-h*3600-m*60));
		if (h > 0){
			t += h +" ч";
			p = true;
		}
		if (m > 0){
			if (p){
				t+=", ";
			}
			t += m +" м";
			p = true;
		}
		if (!p && s > 0){
			t += s +" с";
			p = true;
		}
		return p ? t + ")": string.Empty;
	}

	IEnumerator Load(string url, string token = ""){
		WWWForm form = new WWWForm();
		bool tokenStat = !string.IsNullOrEmpty(token);
		if (tokenStat){
        	form.AddField("token", token);
    	}
    	//Debug.Log((tokenStat ? "POST: " : "GET: ") + url);
        UnityWebRequest www = tokenStat ? UnityWebRequest.Post(url, form) : UnityWebRequest.Get(url);
        if (xTokenStat()){
        	www.SetRequestHeader("X-Device-Token", xDeviceToken);
    	}
        using (www)
        {
            yield return www.SendWebRequest();
            string newToken = www.GetResponseHeader("X-Device-Token");
            if (!string.IsNullOrEmpty(newToken)){
            	xDeviceToken = newToken;
            	PlayerPrefs.SetString("xtoken", xDeviceToken);
        	}
            answer = www.downloadHandler.text;
            //Debug.Log(answer);
        }
	}

	bool xTokenStat(){
		return !string.IsNullOrEmpty(xDeviceToken);
	}

	public void About(){
		Application.OpenURL("https://tjournal.ru/70460");
	}
}
