using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class start : MonoBehaviour {
    public Texture btnretry;
	public Texture btnchahao;
	public Texture fivewen;
	public GUIStyle gooder;
	public GUIStyle bader;
    public Font fonter;
	// Use this for initialization
	int showing=0,sander=0;

    int big_button_size;
    int bar_height;
    int proper_fontsize;


    void Start () {

        big_button_size = this.GetComponent<proper_ui>().proper_big_button;
        bar_height = this.GetComponent<proper_ui>().proper_bar_height;
        proper_fontsize = this.GetComponent<proper_ui>().proper_font_size;
        this.GetComponent<proper_ui>().set_proper_ui_style(gooder);

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
	void OnGUI()
	{
	    GUI.skin.label.fontSize = 65; 
		
		if(GUI.Button(new Rect(0,0,100,100),btnchahao,gooder)||Input.GetKeyDown(KeyCode.Escape))
		{
			Quit();
	    }
		if(GUI.Button(new Rect(100,0,100,100),fivewen,gooder)&&sander>5)
		{
		    sander=0;
			if(showing==0)
			showing=1;
			else{
				showing=0;
			}
	    }
        GUI.skin.font = fonter;

        if (showing==1)
		{
            int font_origin = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = proper_fontsize; 
			GUI.skin.label.normal.textColor = new Vector4( 0.89f, 0.89f, 0.89f, 1.0f );
  
            GUI.Label(new Rect(10f, big_button_size * 1.7f + 10f, big_button_size * 6, big_button_size * 1.7f),"你可以与天赋秉异的Ai下五子棋了\nYou could play with smart gobang AI now");
			GUI.Label(new Rect(10f, big_button_size * 3.4f + 10f, big_button_size * 6, big_button_size * 1.7f),"围棋贼有趣,又贼复杂\nGo game is complicated but interesting");
			GUI.Label(new Rect(10f, big_button_size * 5.1f + 10f, big_button_size * 6, big_button_size * 1.7f),"象棋到底是怎么下的\nI still don't know how to play Chinese chess .");
			GUI.Label(new Rect(10f, big_button_size * 6.8f + 10f, big_button_size * 6, big_button_size * 1.7f),"数独需要很强的逻辑思维\nSudoku needs outstanding logical and analysis abilities.");
            GUI.Label(new Rect(10f, big_button_size * 8.5f + 10f, big_button_size * 6, big_button_size * 1.7f),"国际象棋好像很有意思的样子\nChess seems interesting as well");
            GUI.Label(new Rect(10f, big_button_size * 10.2f + 10f, big_button_size * 6, big_button_size * 1.7f),"国际跳棋好像比较容易上手\nDraughts seems not so difficult to learn");
            GUI.Label(new Rect(10f, big_button_size * 11.9f + 10f, big_button_size * 6, big_button_size * 1.7f), "识别分辨率 "+Screen.width+"*"+Screen.height);
            GUI.skin.label.fontSize = font_origin;
        }
	}
}
