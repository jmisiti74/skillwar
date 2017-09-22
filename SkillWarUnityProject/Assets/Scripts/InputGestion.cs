using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using SocketIO;

[System.Serializable]
public class UserDatas {
	public string username = null;
	public string elo = null;
	public string _id = null;
	public int experience = 0;
	public int[] items = null;

	public UserDatas create(string jsonString) {
		return JsonUtility.FromJson<UserDatas> (jsonString);
	}
}

public class InputGestion : MonoBehaviour {
	// Use this for initialization
	GameObject[] inputs;
	GameObject go;
	Renderer meRenderer;
	Renderer enemyRenderer;
	public GameObject waitForUser;
	public Transform me;
	public Transform enemy;
	bool btnSignupClicked = false;
	bool updateInfoDone = false;
	bool btnSigninClicked = false;
	bool btnPlayClicked = false;
	bool fightStarted = false;
	public int pm = 3;
	public int hp = 100;
	const int EXPERIENCELEVEL = 1000;
	Button playBtn;
	public UserDatas user = new UserDatas();
	public SocketIOComponent socket;
	Dictionary<string, string> data = new Dictionary<string, string>();
	public int urlType = 1;
	bool userIsDisconnected = false;
	public string serverAddress;

	void Start () {
		socket = GameObject.Find ("Network").GetComponent<SocketIOComponent> ();
		socket.On ("launch", launchFight);
		socket.On ("open", OnSocketOpen);
		socket.On ("top", updateEnemyPosition);
		socket.On ("bottom", updateEnemyPosition);
		socket.On ("left", updateEnemyPosition);
		socket.On ("right", updateEnemyPosition);
		socket.On ("rLeft", updateEnemyPosition);
		socket.On ("rRight", updateEnemyPosition);
		socket.On ("userLeft", userLeft);
		socket.On ("userIsBack", userIsBack);
		socket.On ("addPm", addPm);
		socket.On ("UpdateHp", updateHp);
		socket.On ("spellLaunched", onSpellShooted);
		inputs = GameObject.FindGameObjectsWithTag("Input");
		EventSystem.current.SetSelectedGameObject(inputs[1].gameObject, null);
	}

	public void onSpellShooted(SocketIOEvent ev) {
		string spellId = ev.data.ToString ().Split (',') [0].Split (':') [1];
		float posX = float.Parse(ev.data.ToString ().Split (',') [1].Split (':') [1]);
		float posZ = float.Parse(ev.data.ToString ().Split (',') [2].Split (':') [1]);
		string launcher = ev.data.ToString ().Split (',') [3].Split (':') [1];
		float rotation = float.Parse(ev.data.ToString ().Split (',') [4].Split (':') [1].Split('}')[0]);
		GameObject spell = Resources.Load ("Spell" + spellId) as GameObject;
		spell = Instantiate (spell, new Vector3 (posX + 0.5f, 0.25f, posZ + 0.5f), Quaternion.Euler (0, rotation, 0)) as GameObject;
	}

	public void updateHp(SocketIOEvent ev) {
		Debug.Log (ev.data.ToString ().Split(':')[1].Split('}')[0]);
		hp = int.Parse(ev.data.ToString ().Split(':')[1].Split('}')[0]);
	}

	public void userLeft(SocketIOEvent ev) {
		waitForUser = Instantiate (waitForUser);
		userIsDisconnected = true;
		StartCoroutine (setTimer (30));
	}

	public void userIsBack(SocketIOEvent ev) {
		if (userIsDisconnected == true) {
			userIsDisconnected = false;
			Destroy (waitForUser);
		}
	}

	IEnumerator setTimer(int seconds) {
		while (seconds > 0 && userIsDisconnected) {
			GameObject.Find("Timer").GetComponent<Text>().text = "Il lui reste " + seconds + " secondes avant d'être déclarer perdant.";
			yield return new WaitForSeconds (1);
			seconds--;
		}
	}

	public void updateInfos() {
		GameObject.Find ("PM").GetComponent<Text> ().text = "PM : " + pm + " HP : " + hp;
	}

	public void addPm(SocketIOEvent ev) {
		pm++;
		updateInfos ();
	}

	public void updateEnemyPosition(SocketIOEvent ev) {
		if (ev.name == "top") {
			enemy.position = new Vector3 (enemy.position.x, enemy.position.y, enemy.position.z + 1f);
		} else if (ev.name == "bottom") {
			enemy.position = new Vector3 (enemy.position.x, enemy.position.y, enemy.position.z - 1f);
		} else if (ev.name == "left") {
			enemy.position = new Vector3 (enemy.position.x - 1f, enemy.position.y, enemy.position.z);
		} else if (ev.name == "right") {
			enemy.position = new Vector3 (enemy.position.x + 1f, enemy.position.y, enemy.position.z);
		} else if (ev.name == "rLeft") {
			enemy.Find("Player").rotation = Quaternion.Euler (enemy.Find("Player").rotation.x, enemy.Find("Player").eulerAngles.y - 90, enemy.Find("Player").rotation.z);
		} else if (ev.name == "rRight") {
			enemy.Find("Player").rotation = Quaternion.Euler (enemy.Find("Player").rotation.x, enemy.Find("Player").eulerAngles.y + 90, enemy.Find("Player").rotation.z);
		}
	}

	public void OnSocketOpen(SocketIOEvent ev) {
		data ["socketid"] = socket.sid;
		Debug.Log ("Je suis : " + socket.sid);
		data ["userid"] = user._id;
	}

	void Awake() {
		serverAddress = (urlType == 1) ? "localhost:8081" : "https://skillwar.herokuapp.com";
		DontDestroyOnLoad (transform.gameObject);
	}

	public void onSignupClicked () {
		btnSignupClicked = true;
	}

	public void onSigninClicked () {
		btnSigninClicked = true;
	}

	public void onPlayClicked () {
		btnPlayClicked = true;
	}

	IEnumerator findFight() {
		WWWForm form = new WWWForm ();
		form.AddField("id", user._id);
		form.AddField("socketid", data["socketid"]);
		UnityWebRequest www = UnityWebRequest.Post(serverAddress + "/fight", form);
		yield return www.Send ();
		if (www.isNetworkError) {
			Debug.Log (www.error);
		} else {
			GameObject.Find ("Waiting").GetComponent<CanvasGroup>().alpha = 1f;
			GameObject.Find ("Link").GetComponent<SpriteRenderer> ().enabled = true;
			Debug.Log ("En attente d'un adversaire. . .");
		}
	}

	IEnumerator getPositions() {
		WWWForm form = new WWWForm ();
		form.AddField("id", user._id);
		UnityWebRequest www = UnityWebRequest.Post(serverAddress + "/getPositions", form);
		yield return www.Send ();
		if (www.isNetworkError) {
			Debug.Log (www.error);
		} else {
			string[] myPos = www.downloadHandler.text.Split ('|')[0].Split(',');
			string[] enemyPos = www.downloadHandler.text.Split ('|')[1].Split(',');
			me = Instantiate (me, new Vector3(float.Parse(myPos[0]), float.Parse(myPos[1]), float.Parse(myPos[2])), me.rotation) as Transform;
			me.Find("Player").rotation = Quaternion.Euler (me.Find("Player").rotation.x, float.Parse (myPos [3]), me.Find("Player").rotation.z);
			me.name = "Me";
			meRenderer = me.Find ("Player").GetComponent<Renderer> ();
			enemy = Instantiate (enemy, new Vector3(float.Parse(enemyPos[0]), float.Parse(enemyPos[1]), float.Parse(enemyPos[2])), enemy.rotation) as Transform;
			enemy.Find("Player").rotation = Quaternion.Euler (enemy.Find("Player").rotation.x, float.Parse (enemyPos [3]), enemy.Find("Player").rotation.z);
			enemy.name = "Enemy";
			enemyRenderer = enemy.Find ("Player").GetComponent<Renderer> ();
		}
	}

	public void launchFight(SocketIOEvent ev) {
		if (fightStarted == false) {
			StartCoroutine (getPositions ());
			fightStarted = true;
			SceneManager.LoadScene ("BattleMap", LoadSceneMode.Single);
		}
	}

	IEnumerator signup() {
		WWWForm form = new WWWForm ();
		form.AddField("username", inputs [1].GetComponentInChildren<InputField>().text);
		form.AddField("password", inputs [0].GetComponentInChildren<InputField>().text);
		UnityWebRequest www = UnityWebRequest.Post(serverAddress + "/signup", form);
		yield return www.Send ();
		if (www.isNetworkError) {
			Debug.Log (www.error);
		} else {
			if (user.username != null) {
				user = JsonUtility.FromJson<UserDatas>(www.downloadHandler.text);
				Dictionary<string, string> val = new Dictionary<string, string>();
				val["id"] = user._id;
				socket.Emit ("updateMySocketId", new JSONObject(val));
				SceneManager.LoadScene ("Homepage", LoadSceneMode.Single);
			}
		}
	}

	IEnumerator turnWhite(Renderer render) {
		yield return new WaitForSeconds (0.2f);
		render.material.color = Color.white;
	}

	IEnumerator signin() {
		WWWForm form = new WWWForm ();
		form.AddField("username", inputs [1].GetComponentInChildren<InputField>().text);
		form.AddField("password", inputs [0].GetComponentInChildren<InputField>().text);
		UnityWebRequest www = UnityWebRequest.Post(serverAddress + "/signin", form);
		yield return www.Send ();
		if (www.isNetworkError) {
			Debug.Log (www.error);
		} else {
			if (user.username != null) {
				user = user.create(www.downloadHandler.text);
				Dictionary<string, string> val = new Dictionary<string, string>();
				val["id"] = user._id;
				socket.Emit ("updateMySocketId", new JSONObject(val));
				SceneManager.LoadScene ("Homepage", LoadSceneMode.Single);
			}
		}
	}

	void UpdatePosition (string move) {
		Dictionary<string, string> val = new Dictionary<string, string>();
		val["id"] = user._id;
		socket.Emit (move, new JSONObject(val));
	}

	// Update is called once per frame
	void Update () {
		if (fightStarted && !userIsDisconnected) {
			if (meRenderer.material.color == Color.red) {
				StartCoroutine (turnWhite (meRenderer));
			} else if (enemyRenderer.material.color == Color.red) {
				StartCoroutine (turnWhite (enemyRenderer));
			}
			if (Input.GetKeyDown ("z") && me.position.z < 18 && pm > 0) {
				pm--;
				updateInfos ();
				me.position = new Vector3 (me.position.x, me.position.y, me.position.z + 1f);
				UpdatePosition ("top");
			} else if (Input.GetKeyDown ("s") && me.position.z > 0 && pm > 0) {
				pm--;
				updateInfos ();
				me.position = new Vector3 (me.position.x, me.position.y, me.position.z - 1f);
				UpdatePosition ("bottom");
			} else if (Input.GetKeyDown ("q") && me.position.x > 0 && pm > 0) {
				pm--;
				updateInfos ();
				me.position = new Vector3 (me.position.x - 1f, me.position.y, me.position.z);
				UpdatePosition ("left");
			} else if (Input.GetKeyDown ("d") && me.position.x < 32 && pm > 0) {
				pm--;
				updateInfos ();
				me.position = new Vector3 (me.position.x + 1f, me.position.y, me.position.z);
				UpdatePosition ("right");
			} else if (Input.GetKeyDown ("a")) {
				UpdatePosition ("rLeft");
				me.Find("Player").rotation = Quaternion.Euler (me.Find("Player").rotation.x, me.Find("Player").eulerAngles.y - 90, me.Find("Player").rotation.z);
			} else if (Input.GetKeyDown ("e")) {
				UpdatePosition ("rRight");
				me.Find("Player").rotation = Quaternion.Euler (me.Find("Player").rotation.x, me.Find("Player").eulerAngles.y + 90, me.Find("Player").rotation.z);
			} else if (Input.GetKeyDown ("&") || Input.GetKeyDown ("1")) {
				Dictionary<string, string> val = new Dictionary<string, string>();
				val["id"] = user._id;
				socket.Emit ("launchSpell1", new JSONObject(val));
			}
		}
		if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Homepage") && updateInfoDone == false) {
			GameObject.Find ("Link").GetComponent<SpriteRenderer> ().enabled = false;
			GameObject.Find ("Waiting").GetComponent<CanvasGroup>().alpha = 0f;
			socket.Connect ();
			playBtn = GameObject.Find ("PlayBtn").GetComponent<Button> ();
			playBtn.onClick.AddListener (onPlayClicked);
			updateInfoDone = true;
			GameObject.Find ("Username").GetComponentInChildren<Text> ().text = user.username;
			GameObject.Find ("Elo").GetComponentInChildren<Text> ().text = user.elo;
			GameObject.Find ("XpPercentage").GetComponentInChildren<Text> ().text = ((user.experience % EXPERIENCELEVEL) / (EXPERIENCELEVEL / 100)).ToString () + "% - Niveau " + ((user.experience - (user.experience % EXPERIENCELEVEL)) / EXPERIENCELEVEL).ToString ();
			float xpPercentage = (user.experience % EXPERIENCELEVEL);
			float xpPercentageFinal = xpPercentage / 1000;
			GameObject.Find ("XpBar").GetComponentInChildren<Slider> ().value = xpPercentageFinal;
		}
		if (btnPlayClicked) {
			btnPlayClicked = false;
			StartCoroutine (findFight());
		}
		if (Input.GetKeyDown ("tab")) {
			if (EventSystem.current.currentSelectedGameObject == inputs [0]) {
				EventSystem.current.SetSelectedGameObject (inputs [1].gameObject, null);
			} else {
				EventSystem.current.SetSelectedGameObject (inputs [0].gameObject, null);
			}
		} else if (btnSignupClicked || Input.GetKeyDown ("return") || btnSigninClicked || Input.GetKeyDown("enter")) {
			bool ok1 = false;
			bool ok2 = false;
			if (inputs[0].GetComponent<InputField> ().text.Length < 8) {
				GameObject.Find ("PasswordError").GetComponent<RectTransform>().sizeDelta = new Vector2 (300, 30);
			} else {
				GameObject.Find ("PasswordError").GetComponent<RectTransform>().sizeDelta = new Vector2 (300, 0);
				ok1 = true;
			}
			if (inputs[1].GetComponent<InputField> ().text.Length < 3) {
				GameObject.Find ("AccountError").GetComponent<RectTransform>().sizeDelta = new Vector2 (300, 30);
			} else {
				GameObject.Find ("AccountError").GetComponent<RectTransform>().sizeDelta = new Vector2 (300, 0);
				ok2 = true;
			}
			if (ok1 && ok2) {
				if (Input.GetKeyDown ("return") || btnSigninClicked || Input.GetKeyDown ("enter")) {
					btnSigninClicked = false;
					StartCoroutine (signin ());

				} else {
					btnSignupClicked = false;
					StartCoroutine (signup ());
				}
			} else {
				btnSignupClicked = false;
				btnSigninClicked = false;
			}
		}
	}
}
