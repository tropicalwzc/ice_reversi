using UnityEngine;
using System.Collections;

public class namered : MonoBehaviour {

	// Use this for initialization

	float posx,posy;
    public int final_x=0;
    public int final_y=0;
    public int chesscolor = -1;
    int sander = 0;
    private void Start()
    {
        chesscolor *= tag_to_int(this.gameObject.tag);
        flush_position();
    }
    int tag_to_int(string tagger)
    {
        switch (tagger)
        {
            case "zuzi": return 1;
            case "ju": return 2;
            case "ma": return 3;
            case "pao": return 4;
            case "xiang": return 5;
            case "shi": return 6;
            case "jiang": return 7;
            case "shuai": return 8;
        }
        return 0;
    }
    public void move_to(int p_x,int p_y)
    {
        posx = p_x * 10 - 45f;
        posy = p_y * 10 - 45f;
        this.transform.position = new Vector3(posx, posy, this.transform.position.z);
    }
    void flush_position()
    {
        posx = this.transform.localPosition.x;
        posy = this.transform.localPosition.y;
        final_x = (int)((posx + 45f) / 9.99f);
        final_y = (int)((posy + 45f) / 9.99f);
        if (chesscolor < 0)
            this.name = "red" + final_x + "," + final_y;
        else
        {
            this.name = "black" + final_x + "," + final_y;
        }
    }
    // Update is called once per frame
    void Update () {
            flush_position();
	}
}
