using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class quitbtn : MonoBehaviour
{

    private GameObject finder;
    public GameObject recev;
    public Texture btnretry;
    public Texture backtomenu;
    public Font fonter;
    public GUIStyle gooder;
    public string restart_to_scene;
    private int sander = 0;
    public int challenge_on = 0;
    int big_button_size;
    int proper_fontsize;
    int bar_height;
    // Use this for initialization
    void Start()
    {
        big_button_size = this.GetComponent<proper_ui>().proper_big_button;
        proper_fontsize = this.GetComponent<proper_ui>().proper_font_size;
        bar_height = this.GetComponent<proper_ui>().proper_bar_height;
    }

    // Update is called once per frame
    void Update()
    {
        sander++;

    }
    void challenge_game(int np)
    {
        challenge_on = np;
    }
    public void Quit()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    float oldx, oldy;

    void Restartnow(int op)
    {
        SceneManager.LoadScene(restart_to_scene);
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 40;
        GUI.skin.button.fontSize = 40;
        GUI.skin.font = fonter;

        if (GUI.Button(new Rect(big_button_size * 0.7f, 0, big_button_size, big_button_size), btnretry, gooder) || Input.GetKeyDown(KeyCode.R) && sander > 10)
        {
            sander = 0;
            if (challenge_on == 0)
                recev.SendMessage("Restartnow", 1, SendMessageOptions.DontRequireReceiver);
            else
            {
                SceneManager.LoadScene(restart_to_scene);
            }
        }

    }
}
