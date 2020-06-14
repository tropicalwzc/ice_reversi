using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class quitgo : MonoBehaviour {

GameObject finder;
	public GameObject recev;
    public Texture btnchahao;
	public Texture btnretry;
	public Texture backtomenu;
	public GUIStyle gooder;
	public string backlevel="gochess";
	int sander=0;
	// Use this for initialization
	void Start () {
	
	}

	// Update is called once per frame
	void Update () {
	sander++;

	}
    public void Quit (){
  
    #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    #else
    Application.Quit();
    #endif
    }

	float oldx,oldy;


	void OnGUI()
	{
	    GUI.skin.label.fontSize = 40;  
	    GUI.skin.button.fontSize = 40;  


        if(GUI.Button(new Rect(0,0,50,50),btnchahao,gooder)||Input.GetKeyDown(KeyCode.Escape)&&sander>10)
		{
		    sander=0;
			Quit();
	    }

		if(GUI.Button(new Rect(50,0,50,50),backtomenu,gooder)&&sander>3)
		{
		    sander=0;
            SceneManager.LoadScene("starter");
	    }

	    if(GUI.Button(new Rect(100,0,50,50),btnretry,gooder)||Input.GetKeyDown(KeyCode.R)&&sander>10)
		{
		 sander=0;
		 recev.SendMessage("Restartnow",1,SendMessageOptions.DontRequireReceiver);
         SceneManager.LoadScene(backlevel);
		}

		GUI.skin.label.normal.textColor = new Vector4( 0.9f, 0.75f, 0.5f, 1.0f ); 
		GUI.skin.label.fontSize = 25;  
		GUI.Label(new Rect(145,Screen.height-50,1030,100)," "+System.DateTime.Now.ToString("ss"));
		GUI.skin.label.normal.textColor = new Vector4( 0.95f, 0.95f, 1.0f, 1.0f ); 
		GUI.skin.label.fontSize = 40;  
        GUI.Label(new Rect(10,Screen.height-70,1030,100)," "+System.DateTime.Now.ToString("HH : mm "));
		GUI.skin.label.fontSize = 20;  
		GUI.skin.label.normal.textColor = new Vector4( 0.61f, 0.72f, 0.89f, 1.0f ); 
        GUI.Label(new Rect(10,Screen.height-100,1030,100)," "+System.DateTime.Now.ToString("MMMM  dd "));
  }
}
