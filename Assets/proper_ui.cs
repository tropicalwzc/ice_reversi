using UnityEngine;
using System.Collections;

public class proper_ui : MonoBehaviour
{
    public int proper_big_button = 60;
    public int proper_bar_height = 35;
    public int proper_text_size = 20;
    public int proper_font_size = 20;
    public int tropicalside_sv = -1;
    // Use this for initialization
    void Awake()
    {
        if (Screen.height > 900)
        {
            proper_big_button = 60;
            proper_bar_height = 35;
            proper_text_size = 20;
            proper_font_size = 20;
        }
        if (Screen.height > 1200)
        {
            proper_big_button = 90;
            proper_bar_height = 40;
            proper_text_size = 25;
            proper_font_size = 25;
        }
    }
    private void Start()
    {
        if (Screen.height > 900)
        {
            proper_big_button = 60;
            proper_bar_height = 35;
            proper_text_size = 20;
            proper_font_size = 20;
        }
        if (Screen.height > 1200)
        {
            proper_big_button = 90;
            proper_bar_height = 45;
            proper_text_size = 25;
            proper_font_size = 25;
        }
    }
    public void set_proper_ui_style(GUIStyle good)
    {
        if (Screen.height <= 900)
            good.fontSize = 14;
        else
        if (Screen.height > 900 && Screen.height <= 1200)
            good.fontSize = 17;
        else
        if (Screen.height > 1200)
            good.fontSize = 20;
    }

}
