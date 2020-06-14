using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Threading;

public class mousecatch : MonoBehaviour
{

    private GameObject finder, finderanother, chess, nextchess, followchess, cbase;
    public Texture btnback, btnhelp;

    public GUIStyle gooder;
    public GUIStyle challenger;
    public GameObject[] prefab = new GameObject[6];
    public GameObject recev;
    public AudioClip AC, BC, CC, DC, endAC, returnAC, chalengewinAC, normalwinAC;
    public Texture restarting, blackandwhitepic, trainingpic;
    public Font fonter;
    public int vsmode = 0;
    private int sander = 0, poping_history = 0, dosander = 0;
    private int sending = 0;
    int lx = -1, ly = -1;
    int challenge_mode = 0;
    // 翻转对方棋子数量,行动力权重
    // 100, 1000, -40000, 100007, -17000, 9000, -100000
    // 0,   545,  -32374, 43942, -14379, 10186, -76971
    int[] score_scale = { 100, 3000, -40000, 100007, -17000, 9000, -300000 };
    int[] scale_first = { 100, 3000, -40000, 100007, -17000, 9000, -300000 };
    int[] scale_second = { 100, 3000, -40000, 100007, -17000, 9000, -300000 };
    int[] scale_global = new int[10];

    Stack<Vector2Int> flipstack = new Stack<Vector2Int>();
    Stack<Thread> thread_actnow = new Stack<Thread>();
    int[,] chessboard = new int[8, 8];
    int[,] imagineboard = new int[8, 8];
    string output_str;
    string trainer_str;
    bool[,] flip_possible_map = new bool[8, 8];
    bool door_lock = false;
    bool calculating = false;
    Stack<Stack<Vector2Int>> history = new Stack<Stack<Vector2Int>>();
    files filer = new files();
    Stack<Vector2Int>[,] nowpossible_global;
    int[,] cpboard_global = new int[8, 8];

    int wait_num_global = 0;
    int current_return_global = 0;
    int[,] score_map_global = new int[8, 8];

    int next_trainer_ready = 0;
    int nextcolor = 1;
    int train_side = 1;
    int tropical_side = -1;
    int total_step_now = 0;
    int wait_is_on = 0;
    int is_my_turn = 0;
    int gameover = 0;
    int big_button_size;
    int bar_height;
    int proper_fontsize;
    int whitescore, blackscore;
    int process_my_step_now;
    int strong_clock;
    bool training_on = false;
    // Use this for initialization
    void Start()
    {

        big_button_size = this.GetComponent<proper_ui>().proper_big_button;
        bar_height = this.GetComponent<proper_ui>().proper_bar_height;
        proper_fontsize = this.GetComponent<proper_ui>().proper_font_size;
        this.GetComponent<proper_ui>().set_proper_ui_style(gooder);
        this.GetComponent<proper_ui>().set_proper_ui_style(challenger);
        string trop_txt = filer.ReadTextFile("tropcial_side");
        if (trop_txt == "1")
        {
            tropical_side = 1;
        }
        else
        {
            tropical_side = -1;
        }


        if (Screen.width < 1200)
        {
            recev.transform.localPosition = new Vector3(recev.transform.localPosition.x, recev.transform.localPosition.y, recev.transform.localPosition.z - 40f);
        }
        else if (Screen.width < 1500)
        {
            recev.transform.localPosition = new Vector3(recev.transform.localPosition.x, recev.transform.localPosition.y, recev.transform.localPosition.z - 30f);
        }

        cbase = GameObject.FindGameObjectWithTag("camerabase");
        initboard();
        process_my_step_now = 0;
    }
    void initboard()
    {
        clear_board();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                chessboard[i, j] = 0;
            }
        dosander = 0;
        nextcolor = 1;

        for (int i = 3; i < 5; i++)
            for (int j = 3; j < 5; j++)
            {
                if (i == j)
                {
                    put_a_chess_to_board(i, j, -1);
                }
                else
                {
                    put_a_chess_to_board(i, j, 1);
                }
            }
        if (tropical_side == 1)
        {

            if (training_on)
            {
                train_play(1);
            }
            else
            {
                trainer_str = "";
                let_me_play();
            }
        }
        else
        {
            search_possible_position_and_flushlight(nextcolor);
        }

    }
    void clear_board()
    {
        clear_light();
        GameObject[] remains = GameObject.FindGameObjectsWithTag("chesser");
        foreach (GameObject re in remains)
        {
            Destroy(re.gameObject);
        }
        whitescore = 0;
        blackscore = 0;
    }
    void clear_light()
    {
        GameObject[] remains = GameObject.FindGameObjectsWithTag("lighter");
        foreach (GameObject re in remains)
        {
            Destroy(re.gameObject);
        }

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                flip_possible_map[i, j] = false;
    }
    void copyboard(int[,] destination_board, int[,] from_board)
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                destination_board[i, j] = from_board[i, j];
            }
    }
    void initboard_from(int[,] from_board)
    {
        clear_board();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                chessboard[i, j] = from_board[i, j];
                if (chessboard[i, j] != 0)
                    put_a_chess_to_board(i, j, chessboard[i, j]);
            }
    }
    void push_current_to_history(Vector2Int clicker, Stack<Vector2Int> flipper)
    {
        Stack<Vector2Int> currentop = new Stack<Vector2Int>();

        foreach (Vector2Int nc in flipper)
        {
            currentop.Push(nc);
        }
        currentop.Push(clicker);

        history.Push(currentop);
    }
    void pop_history()
    {
        if (history.Count <= 0)
            return;
        Stack<Vector2Int> currentop = history.Peek();
        Vector2Int delaim = currentop.Peek();
        currentop.Pop();
        GameObject lastchess = GameObject.Find(chessname(delaim.x, delaim.y));
        Destroy(lastchess.gameObject);
        if (chessboard[delaim.x, delaim.y] == 1)
        {
            blackscore--;
        }
        else
        {
            whitescore--;
        }
        chessboard[delaim.x, delaim.y] = 0;

        foreach (Vector2Int flipagain in currentop)
        {
            flip_the_chess_at(flipagain);
        }
        nextcolor *= -1;
        search_possible_position_and_flushlight(nextcolor);
        history.Pop();
    }
    int search_possible_map(int color)
    {
        int possible_num = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (chessboard[i, j] == 0)
                {
                    Stack<Vector2Int> rev = could_flip_at(chessboard, i, j, color);
                    if (rev.Count > 0)
                    {
                        flip_possible_map[i, j] = true;
                        possible_num++;
                    }
                }
            }
        }
        return possible_num;
    }
    void light_possible_map()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (flip_possible_map[i, j])
                {
                    GameObject lightobj = Instantiate(prefab[4]) as GameObject;
                    lightobj.transform.localPosition = array_pos_to_vector_light(i, j);
                    lightobj.name = "light" + i + "," + j;
                }
            }
        }
    }
    Stack<Vector2Int> could_flip_at(int[,] chessboard_t, int posx, int posy, int color)
    {
        // print("checking " + posx + "," + posy);
        Stack<Vector2Int> res = new Stack<Vector2Int>();
        if (posx < 6)
        {
            if (chessboard_t[posx + 1, posy] == -color)
            {
                int fini = -1;
                for (int i = posx + 2; i < 8; i++)
                {
                    if (chessboard_t[i, posy] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[i, posy] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posx + 1; j < fini; j++)
                {
                    res.Push(new Vector2Int(j, posy));
                }
            }
        }

        if (posx > 1)
        {
            if (chessboard_t[(posx - 1), posy] == -color)
            {
                int fini = 11;
                for (int i = posx - 2; i >= 0; i--)
                {
                    if (chessboard_t[i, posy] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[i, posy] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posx - 1; j > fini; j--)
                {
                    res.Push(new Vector2Int(j, posy));
                }
            }
        }

        if (posy < 6)
        {
            if (chessboard_t[posx, (posy + 1)] == -color)
            {
                int fini = -1;
                for (int i = posy + 2; i < 8; i++)
                {
                    if (chessboard_t[posx, i] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[posx, i] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posy + 1; j < fini; j++)
                {
                    res.Push(new Vector2Int(posx, j));
                }
            }
        }

        if (posy > 1)
        {
            if (chessboard_t[posx, (posy - 1)] == -color)
            {
                int fini = 11;
                for (int i = posy - 2; i >= 0; i--)
                {
                    if (chessboard_t[posx, i] == 0)
                    {
                        break; // no space is tolerated
                    }
                    if (chessboard_t[posx, i] == color)
                    {
                        fini = i; // find another same color chess
                        break;
                    }
                }
                for (int j = posy - 1; j > fini; j--)
                {
                    res.Push(new Vector2Int(posx, j));
                }
            }
        }

        if (posx < 6 && posy < 6)
        {
            int spx = 1;
            int spy = 1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx >= 8 || ny >= 8)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        if (posx > 1 && posy > 1)
        {
            int spx = -1;
            int spy = -1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx < 0 || ny < 0)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        if (posx > 1 && posy < 6)
        {
            int spx = -1;
            int spy = 1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx < 0 || ny >= 8)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        if (posx < 6 && posy > 1)
        {
            int spx = 1;
            int spy = -1;
            if (chessboard_t[posx + spx, posy + spy] == -color)
            {
                int fini = -1;
                for (int i = 2; ; i++)
                {
                    int nx = posx + spx * i;
                    int ny = posy + spy * i;
                    if (nx >= 8 || ny < 0)
                        break;
                    if (chessboard_t[nx, ny] == 0)
                        break;
                    if (chessboard_t[nx, ny] == color)
                    {
                        fini = i;
                        break;
                    }
                }
                for (int j = 1; j < fini; j++)
                {
                    int nx = posx + spx * j;
                    int ny = posy + spy * j;
                    res.Push(new Vector2Int(nx, ny));
                }
            }
        }

        return res;
    }

    //---------------------- Mouse Control -----------------------------------------------

    Vector3 mousetracker()
    {
        Vector3 pos = new Vector3(200, 200, 0);
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo))
            {
                if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    // trainer_str = "Hit " + hitInfo.collider.gameObject.transform.position;
                    pos = hitInfo.collider.gameObject.transform.position;
                    if (Input.GetTouch(0).tapCount == 2)
                    {
                        Debug.Log("双击操作");
                    }
                    return pos;
                }

            }
        }
        return pos;
    }
    Vector2Int catchmouse()
    {
        Vector3 pos = mousetracker();
        float cursor_x = pos.x;
        float cursor_y = pos.y;
        int cursor_array_x = 3 - (int)(cursor_y / 9.99f);
        int cursor_array_y = (int)((cursor_x - 5f) / 9.99f) + 4;
        if (pos.x > 5)
            cursor_array_y++;
        if (pos.y < 0)
            cursor_array_x++;

        return new Vector2Int(cursor_array_x, cursor_array_y);
        //   print("mouse at " + cursor_array_x + "," + cursor_array_y);

    }
    Vector3 array_pos_to_vector(int array_x, int array_y)
    {
        return new Vector3((array_y - 4) * 10f, (3 - array_x) * 10f + 5f, 79.5f);
    }
    Vector3 array_pos_to_vector_light(int array_x, int array_y)
    {
        return new Vector3((array_y - 4) * 10f, (3 - array_x) * 10f + 5f, 78f);
    }
    string chessname(int posx, int posy)
    {
        return "ch" + posx + "," + posy;
    }
    void put_a_chess_to_board(int posx, int posy, int color)
    {
        if (posx >= 0 && posx < 8 && posy >= 0 && posy < 8)
        {
            int whicher;
            if (color == -1)
            {
                whicher = 0;
                whitescore++;
            }
            else
            {
                whicher = 1;
                blackscore++;
            }
            GameObject newchess = Instantiate(prefab[whicher]);
            newchess.gameObject.name = chessname(posx, posy);
            chessboard[posx, posy] = color;
            newchess.transform.localPosition = array_pos_to_vector(posx, posy);
        }
    }
    void flip_the_chess_at(Vector2Int aimpos)
    {
        AudioSource.PlayClipAtPoint(DC, transform.localPosition);
        //   print("flip " + aimpos.x + "," + aimpos.y);
        GameObject thischess = GameObject.Find(chessname(aimpos.x, aimpos.y));
        thischess.GetComponent<flip>().flipping_op = 1;
        chessboard[aimpos.x, aimpos.y] *= -1;
        if (chessboard[aimpos.x, aimpos.y] == 1)
        {
            blackscore++;
            whitescore--;
        }
        else
        {
            whitescore++;
            blackscore--;
        }
    }

    void search_possible_position_and_flushlight(int color)
    {
        clear_light();
        int posinum = search_possible_map(color);
        if (posinum > 0)
        {
            light_possible_map();
        }
        else
        {
            int othercolor = -color;
            int ano = search_possible_map(othercolor);
            if (ano > 0)
            {
                output_str = "PASS";
                if (!training_on)
                {
                    if (is_my_turn == 0)
                    {
                        calculating = false;
                        nextcolor *= -1;
                        is_my_turn = 30;
                    }
                }
                else
                {
                    if (is_my_turn == 0)
                    {
                        nextcolor *= -1;
                        next_trainer_ready = 60;
                        train_side *= -1;
                    }
                }

            }
            else
            {
                if (whitescore > blackscore)
                {
                    gameover = -1;
                }
                if (blackscore > whitescore)
                {
                    gameover = 1;
                }
                if (blackscore == whitescore)
                {
                    gameover = 2;
                }
            }
        }
    }


    //---------------------- Update 60 times per second -----------------------------------------
    // Update is called once per frame
    private void FixedUpdate()
    {
        if (calculating)
        {
            strong_clock++;
        }

    }
    void Update()
    {
        dosander++;

        if (is_my_turn > 0)
        {
            is_my_turn--;
            if (is_my_turn == 1)
                let_me_play();
        }

        if (dosander > 30)
            if (flipstack.Count > 0 && wait_is_on == 1)
            {
                foreach (Vector2Int flippos in flipstack)
                {
                    flip_the_chess_at(flippos);
                }
                flipstack.Clear();
                dosander = 0;
                search_possible_position_and_flushlight(nextcolor);
                wait_is_on = 0;
            }

        if (next_trainer_ready > 0)
        {
            next_trainer_ready--;
            if (next_trainer_ready == 0)
            {
                train_side *= -1;
                train_play(train_side);
            }
        }

        if (process_my_step_now > 0)
        {
            process_my_step_now--;
            if (process_my_step_now == 0)
            {

                Vector2Int next_pos = new Vector2Int(0, 0);
                float maxscore = score_map_global[next_pos.x, next_pos.y];
                bool possi = false;
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                    {
                        if (score_map_global[i, j] == -2000000000)
                            continue;

                        possi = true;
                        score_map_global[i, j] += Random.Range(0, 1000);
                        if (score_map_global[i, j] > score_map_global[next_pos.x, next_pos.y])
                        {
                            next_pos.x = i;
                            next_pos.y = j;
                        }
                    }
                if (possi == true && next_pos.x >= 0)
                {
                    flipstack = could_flip_at(chessboard, next_pos.x, next_pos.y, nextcolor);
                    dosander = 0;
                    put_a_chess_to_board(next_pos.x, next_pos.y, nextcolor);
                    push_current_to_history(new Vector2Int(next_pos.x, next_pos.y), flipstack);
                    wait_is_on = 1;
                    nextcolor *= -1;
                    output_str += " ->" + next_pos.x + "," + next_pos.y;
                }

                foreach (Thread ttr in thread_actnow) // 回收所有子线程
                {
                    ttr.Abort();
                }
                thread_actnow.Clear();

                if (training_on)
                {
                    next_trainer_ready = 60;
                }
            }
        }
        if ((current_return_global == wait_num_global && wait_num_global > 0) || strong_clock > 750 && calculating == true)
        {
            door_lock = true;
            calculating = false;
            process_my_step_now = 30;
            wait_num_global = 0;
        }

        if (gameover == 0)
        {

            if (poping_history > 0)
            {
                poping_history--;
                dosander = 0;
                if (poping_history == 39 || poping_history == 1)
                {
                    pop_history();
                }
            }
            else
            {
                sander++;
            }
        }

        if (!training_on)
        {
            if (Input.touchCount > 0 && sander > 7 && gameover == 0 && wait_num_global == 0)
            {
                sander = 0;
                Vector2Int nowposition = catchmouse();
                int xx = nowposition.x;
                int yy = nowposition.y;
                if (xx >= 0 && xx < 8 && yy >= 0 && yy < 8)
                    if (flip_possible_map[xx, yy] == true)
                    {
                        flipstack = could_flip_at(chessboard, xx, yy, nextcolor);
                        if (flipstack.Count > 0)
                        {
                            put_a_chess_to_board(xx, yy, nextcolor);
                            foreach (Vector2Int flippos in flipstack)
                            {
                                flip_the_chess_at(flippos);
                            }
                            push_current_to_history(new Vector2Int(xx, yy), flipstack);
                            flipstack.Clear();
                            nextcolor *= -1;

                            is_my_turn = 30;
                        }
                    }
            }
        }
    }
    void let_me_play()
    {
        if (followchess != null)
            Destroy(followchess.gameObject);

        if (blackscore + whitescore == 64)
        {
            if (blackscore > whitescore)
                gameover = 1;
            if (whitescore > blackscore)
                gameover = -1;
            if (whitescore == blackscore)
                gameover = 2;

            return;
        }
        search_possible_position_and_flushlight(nextcolor);
        door_lock = false;
        calculating = true;
        strong_clock = 0;
        bool possible = analysis_board(chessboard, nextcolor, score_scale);
        if (!possible)
        {
            nextcolor *= -1;
        }
    }

    void train_play(int which_trainer)
    {
        if (followchess != null)
            Destroy(followchess.gameObject);

        if (blackscore + whitescore == 64)
        {
            if (blackscore > whitescore)
                gameover = 1;
            if (whitescore > blackscore)
                gameover = -1;
            if (whitescore == blackscore)
                gameover = 2;

            return;
        }
        search_possible_position_and_flushlight(nextcolor);
        bool possible;
        strong_clock = 0;
        door_lock = false;
        calculating = true;

        if (which_trainer == 1)
            possible = analysis_board(chessboard, nextcolor, scale_first);
        else
        {
            possible = analysis_board(chessboard, nextcolor, scale_second);
        }

        if (!possible)
        {
            nextcolor *= -1;
        }
    }

    void gene_mutation()
    {
        int id = 0;
        foreach (int scl in scale_second)
        {
            int chrate = Random.Range(0, 40) - 20;
            float rate_ch = (float)chrate / 100.0f;
            int newscl = (int)((float)scl * (float)(1 + rate_ch));
            scale_second[id] = newscl;
            id++;
        }
        id = 0;
        foreach (int scl in scale_second) // 返祖
        {
            if (Random.Range(0, 100) == 1)
            {
                scale_second[id] = score_scale[id];
            }
            if (Random.Range(0, 5) == 1) // 获取对方基因
            {
                scale_second[id] = scale_first[id];
            }
            id++;
        }
    }
    void gene_recombination()
    {
        float secondrate = 0.01f;
        float firstrate = 0.99f;
        if (gameover == -1)
        {
            secondrate = 0.5f;
            firstrate = 0.5f;
        }
        int id = 0;
        foreach (int scl in scale_first)
        {
            int newscl = (int)(firstrate * (float)scl + secondrate * (float)scale_second[id]);
            if (Random.Range(0, 4) != 1) // 拒绝遗传概率
            {
                scale_first[id] = newscl;
            }
            id++;
        }
    }
    //---------------------- GUI -----------------------------------------

    void OnGUI()
    {
        GUI.skin.label.fontSize = proper_fontsize + 10;
        GUI.skin.label.normal.textColor = new Vector4(0.05f, 0.05f, 0.0f, 1.0f);
        GUI.Label(new Rect(big_button_size * 5.9f, 30, big_button_size * 4, 70), "Black : " + blackscore);
        GUI.skin.label.normal.textColor = new Vector4(0.95f, 0.95f, 1.0f, 1.0f);
        GUI.Label(new Rect(big_button_size * 5.9f, 100, big_button_size * 4, 100), "White : " + whitescore);
        GUI.Label(new Rect(0, Screen.height - Screen.height / 10, Screen.width, Screen.height / 11), output_str);

        GUI.skin.label.fontSize = proper_fontsize;
        GUI.Label(new Rect(20, big_button_size * 1.8f, Screen.width / 4 - 20, Screen.height / 16 * 13.0f), trainer_str + " " + strong_clock * 20 + " ms");
        GUI.skin.label.normal.textColor = new Vector4(0.85f, 0.85f, 0.95f, 1.0f);

        GUI.Label(new Rect(big_button_size * 0.7f, big_button_size, big_button_size, big_button_size), "重玩");
        GUI.Label(new Rect(big_button_size * 1.8f, big_button_size, big_button_size, big_button_size), "撤销");
        GUI.Label(new Rect(big_button_size * 2.9f, big_button_size, big_button_size, big_button_size), "换棋子");
        GUI.Label(new Rect(big_button_size * 4.0f, big_button_size, big_button_size, big_button_size), "观战");
        GUI.skin.label.normal.textColor = new Vector4(0.95f, 0.95f, 1.0f, 1.0f);
        if (gameover != 0)
        {
            GUI.skin.label.fontSize = proper_fontsize + 20;
            if (gameover == 1)
            {
                GUI.skin.label.normal.textColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
                GUI.Label(new Rect(big_button_size * 2.4f, 130, Screen.width / 4, Screen.height / 3 * 2), "Black win");
            }
            else if (gameover == -1)
            {
                GUI.skin.label.normal.textColor = new Vector4(0.98f, 0.98f, 0.98f, 1.0f);
                GUI.Label(new Rect(big_button_size * 2.4f, 130, Screen.width / 4, Screen.height / 3 * 2), "White win");
            }
            else
            {
                GUI.skin.label.normal.textColor = new Vector4(0.98f, 0.98f, 0.98f, 1.0f);
                GUI.Label(new Rect(big_button_size * 2.4f, 130, Screen.width / 4, Screen.height / 3 * 2), "Noun");
            }
            if (training_on)
            {
                gene_recombination();
                gene_mutation();
                gameover = 0;

                tropical_side = 1;
                train_side = 1;
                training_on = true;
                initboard();
            }
            GUI.skin.label.fontSize = proper_fontsize;
        }

        if (((GUI.Button(new Rect(big_button_size * 1.8f, 0, big_button_size, big_button_size), btnback, gooder) || Input.GetKeyDown(KeyCode.Space)) && dosander > 10) && is_my_turn == 0 && poping_history == 0)
        {
            dosander = 0;
            poping_history = 40;
        }

        if ((GUI.Button(new Rect(big_button_size * 2.9f, 0, big_button_size, big_button_size), blackandwhitepic, gooder) || Input.GetKeyDown(KeyCode.Space)) && dosander > 10)
        {
            this.GetComponent<proper_ui>().tropicalside_sv *= -1;
            tropical_side *= -1;
            if (tropical_side == 1)
            {
                filer.WriteTextFile("tropcial_side", "1");
            }
            else
            {
                filer.WriteTextFile("tropcial_side", "-1");
            }
            initboard();
            recev.SendMessage("Restartnow", 1, SendMessageOptions.DontRequireReceiver);
        }

        if ((GUI.Button(new Rect(big_button_size * 4.0f, 0, big_button_size, big_button_size), trainingpic, gooder) || Input.GetKeyDown(KeyCode.T)) && dosander > 10)
        {
            if (training_on == false)
            {
                tropical_side = 1;
                train_side = 1;
                training_on = true;
                initboard();
            }
            else
            {
                recev.SendMessage("Restartnow", 1, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    //---------------------- Core ---------------------------------------------------------------

    //计算可以行棋的点,返回棋盘上每个点如果落子可以翻转的棋子坐标栈
    Stack<Vector2Int>[,] basic_valid_map(int[,] chessboard_t, int color)
    {
        Stack<Vector2Int>[,] res = new Stack<Vector2Int>[8, 8];
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                res[i, j] = new Stack<Vector2Int>();
            }
        int possible_num = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (chessboard_t[i, j] == 0)
                {
                    Stack<Vector2Int> thc = could_flip_at(chessboard_t, i, j, color);
                    if (thc.Count > 0)
                    {
                        res[i, j] = thc;
                        flip_possible_map[i, j] = true;
                        possible_num++;
                    }
                }
            }
        }
        return res;
    }
    //计算稳定点
    int stable_point_number(int[,] chessboard_t, int color)
    {
        if (chessboard[0, 0] == 0 && chessboard[0, 7] == 0 && chessboard[7, 7] == 0 && chessboard[7, 0] == 0)
            return 0;
        bool[,] finer = new bool[8, 8];
        int res = 0;
        if (chessboard[0, 0] == color)
        {
            int x = 0, y = 0;
            for (int i = 1; i <= 6; i++)
            {
                if (chessboard_t[x, i] == color)
                    finer[0, i] = true;
                if (chessboard_t[i, y] == color)
                    finer[0, i] = true;
            }
            if (chessboard[1, 1] == color)
                finer[1, 1] = true;
        }
        if (chessboard[7, 7] == color)
        {
            int x = 7, y = 7;
            for (int i = 1; i <= 6; i++)
            {
                if (chessboard_t[x, i] == color)
                    finer[0, i] = true;
                if (chessboard_t[i, y] == color)
                    finer[0, i] = true;
            }
            if (chessboard[6, 6] == color)
                finer[6, 6] = true;
        }
        if (chessboard[0, 7] == color)
        {
            int x = 0, y = 7;
            for (int i = 1; i <= 6; i++)
            {
                if (chessboard_t[x, i] == color)
                    finer[0, i] = true;
                if (chessboard_t[i, y] == color)
                    finer[0, i] = true;
            }
            if (chessboard[1, 6] == color)
                finer[1, 6] = true;
        }
        if (chessboard[7, 0] == color)
        {
            int x = 7, y = 0;
            for (int i = 1; i <= 6; i++)
            {
                if (chessboard_t[x, i] == color)
                    finer[0, i] = true;
                if (chessboard_t[i, y] == color)
                    finer[0, i] = true;
            }
            if (chessboard[6, 1] == color)
                finer[6, 1] = true;
        }

        if (chessboard[0, 0] == -color && chessboard[0, 7] == -color)
        {
            bool allop = true;
            for (int i = 1; i < 7; i++)
            {
                if (chessboard[0, i] != color)
                {
                    allop = false;
                    break;
                }
            }
            if (allop)
                res += 6;
        }
        if (chessboard[7, 0] == -color && chessboard[7, 7] == -color)
        {
            bool allop = true;
            for (int i = 1; i < 7; i++)
            {
                if (chessboard[7, i] != color)
                {
                    allop = false;
                    break;
                }
            }
            if (allop)
                res += 6;
        }
        if (chessboard[0, 0] == -color && chessboard[7, 0] == -color)
        {
            bool allop = true;
            for (int i = 1; i < 7; i++)
            {
                if (chessboard[i, 0] != color)
                {
                    allop = false;
                    break;
                }
            }
            if (allop)
                res += 6;
        }
        if (chessboard[0, 7] == -color && chessboard[7, 7] == -color)
        {
            bool allop = true;
            for (int i = 1; i < 7; i++)
            {
                if (chessboard[i, 7] != color)
                {
                    allop = false;
                    break;
                }
            }
            if (allop)
                res += 6;
        }
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (finer[i, j])
                    res++;

        return res;
    }
    //特殊点的加分
    int specialmap_score(int posx, int posy)
    {
        if ((posx == 0 || posx == 7) && (posy == 0 || posy == 7)) // 角
        {
            return 10000000;
        }
        if ((posx == 1 || posx == 6) && (posy == 1 || posy == 6)) // 垃圾点
        {
            return scale_global[6];
        }
        if (((posx == 0 || posx == 7) && (posy == 2 || posy == 5)) || ((posx == 2 || posx == 5) && (posy == 0 || posy == 7))) // 有利边界
        {
            return scale_global[5];
        }

        if (((posx == 0 || posx == 7) && (posy == 1 || posy == 6)) || ((posx == 1 || posx == 6) && (posy == 0 || posy == 7))) // 打赌边界
        {
            return scale_global[4];
        }

        return 0;
    }
    //主要的搜索函数
    int score_analysis(int[,] board, int color, int[] scale, int tower = 3)
    {
        if (tower <= 0)
            return 0;

        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, board);
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(cpboard, color);
        bool[,] ispossible = new bool[8, 8];
        int total_possible_number = 0;
        int keynum = 0;
        int myscore = 0;
        //把可能下的点放到ispossible记录中
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (nowpossible[i, j].Count > 0)
                {
                    total_possible_number++;
                    ispossible[i, j] = true;
                }
                if (cpboard[i, j] != 0)
                {
                    keynum++;
                    if (cpboard[i, j] == color)
                        myscore++;
                }
            }
        if (myscore == 0 || keynum > 7 && myscore <= 1)
        {
            return (myscore - 32) * 20000007;
        }
        //结束时哪一方胜利
        if (keynum >= 64)
        {
            return (myscore - 32) * 20000007;
        }

        int maxscore = scale[2]; // 没有自由点的惩罚
        bool fir = true;

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (ispossible[i, j])
                {
                    int nowscore = specialmap_score(i, j);
                    int eatscore = nowpossible[i, j].Count * scale[0];
                    if (myscore < 4) // 别死绝了
                        eatscore *= 15;
                    nowscore += eatscore;

                    //假设下在这里
                    //想象此时的棋盘
                    cpboard[i, j] = color;
                    foreach (Vector2Int aa in nowpossible[i, j])
                    {
                        cpboard[aa.x, aa.y] *= -1;
                    }

                    // 计算对方行动力
                    int enemymov_ablity = calculate_move_ability(cpboard, -color);
                    // 计算我方行动力
                    int mymov_ability = calculate_move_ability(cpboard, color);
                    // 计算活力值
                    int energy = mymov_ability - enemymov_ablity;
                    // 计算稳定点
                    int stable_point = stable_point_number(cpboard, color);
                    // 稳定点加分
                    nowscore += stable_point * scale[3];
                    // 行动力加分
                    int cut_score = energy * scale[1];
                    nowscore += cut_score;
                    // 特殊局面权重
                    nowscore += scan_pattern(cpboard, color) * 100005;
                    //开始递归搜索
                    if (keynum < 55 || total_step_now < 55) // 终盘搜索是否进行
                    {
                        // 前期中期搜索
                        if (energy >= 0)
                        {
                            int nextscore = score_analysis(cpboard, -color, scale, tower - 1);//对高活力点仔细搜索
                            nowscore -= nextscore;

                        }
                        else
                        {
                            int nextscore = score_analysis(cpboard, -color, scale, tower - 3); //对不太可能的点降低搜索深度
                            nowscore += scale[2];  // 活力低于对方给予惩罚性扣分             
                            nowscore -= nextscore;
                        }
                    }
                    else
                    {
                        //终盘搜索
                        int nextscore = score_analysis(cpboard, -color, scale, tower);//对高活力点仔细搜索
                        nowscore -= nextscore;
                        // nowscore += 1000 * energy; // 活力高奖励
                    }


                    // 结束想象
                    // 恢复现场
                    cpboard[i, j] = 0;
                    foreach (Vector2Int aa in nowpossible[i, j])
                    {
                        cpboard[aa.x, aa.y] *= -1;
                    }
                    // 获取最高分

                    if (fir)
                    {
                        maxscore = nowscore;
                        fir = false;
                    }
                    else
                    {
                        if (nowscore > maxscore)
                        {
                            maxscore = nowscore;
                        }
                    }

                }
            }

        if (fir == true)
        {
            return scale[2] - score_analysis(cpboard, -color, scale, tower - 1);
        }
        return maxscore;
    }
    //进行下一步的预测
    bool analysis_board(int[,] board, int color, int[] scale)
    {
        door_lock = false;
        Vector2Int nextstep = new Vector2Int(-1, -1);
        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, board);
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(cpboard, color);
        bool[,] ispossible = new bool[8, 8];
        nowpossible_global = nowpossible;
        cpboard_global = cpboard;
        bool havepos = false;
        for (int i = 0; i < scale.Length; i++)
        {
            scale_global[i] = scale[i];
        }

        int total_possible_number = 0;
        wait_num_global = 0;
        current_return_global = 0;
        output_str = "";
        total_step_now = 0;
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                score_map_global[i, j] = -2000000000;
                if (nowpossible[i, j].Count > 0)
                {
                    total_possible_number++;
                    ispossible[i, j] = true;
                    wait_num_global++;
                }
                if (chessboard[i, j] != 0)
                {
                    total_step_now++;
                }
            }

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (ispossible[i, j])  // 为每一个可能的点分配一个线程去搜索
                {
                    havepos = true;

                    Thread thr = new Thread(Thread_score_analysis);
                    thread_actnow.Push(thr);
                    //启动线程,传入参数
                    thr.Start("" + i + "," + j);

                }
            }
        return havepos;
    }
    //线程控制函数
    void Thread_score_analysis(object data)
    {
        string recev = (string)data;
        int sx = recev[0] - '0';
        int sy = recev[2] - '0';
        int nowscore = center_scorer(sx, sy, nowpossible_global[sx, sy], cpboard_global, nextcolor, scale_global);
        output_str += "(" + sx + "," + sy + ")";
        score_map_global[sx, sy] = nowscore;

        if (!door_lock)
            current_return_global++;

        // print("fin search " + sx + "," + sy);
    }

    //每个线程指派任务搜索(i,j)处得分
    int center_scorer(int i, int j, Stack<Vector2Int> possiblepoint, int[,] cpboard_out, int color, int[] scale)
    {
        int[,] cpboard = new int[8, 8];
        copyboard(cpboard, cpboard_out);

        int nowscore = specialmap_score(i, j);
        int eatscore = possiblepoint.Count * scale[0];
        nowscore += eatscore;

        cpboard[i, j] = color;
        foreach (Vector2Int aa in possiblepoint)
        {
            cpboard[aa.x, aa.y] *= -1;
        }
        // imagine area

        // 计算对方行动力
        int enemymov_ablity = calculate_move_ability(cpboard, -color);
        // 计算我方行动力
        int mymov_ability = calculate_move_ability(cpboard, color);
        // 计算活力值
        int energy = mymov_ability - enemymov_ablity;
        // 计算稳定点
        int stable_point = stable_point_number(cpboard, color);
        // 稳定点加分
        nowscore += stable_point * scale[3];
        // 行动力加分
        int cut_score = energy * scale[1];
        nowscore += cut_score;

        nowscore += scan_pattern(cpboard, color) * 100005;
        // if (energy < 0)
        //    nowscore += scale[2];

        // 开始递归搜索
        nowscore -= score_analysis(cpboard, -color, scale, 5);
        //nowscore += Random.Range(0, 400);
        // end imagine
        cpboard[i, j] = 0;
        foreach (Vector2Int aa in possiblepoint)
        {
            cpboard[aa.x, aa.y] *= -1;
        }
        return nowscore;
    }
    int scan_pattern(int[,] board, int color)
    {
        int res = 0;
        for (int bgl = 0; bgl < 8; bgl += 7)
        {
            bool tt = false;
            if ((board[bgl, 1] == color || board[bgl, 6] == color) && (board[bgl, 1] != color || board[bgl, 6] != color))
                tt = true;
            if (tt == false)
                continue;

            for (int j = 2; j <= 5; j++)
            {
                if (board[bgl, j] != 0)
                {
                    tt = false;
                    break;
                }
            }
            if (tt == true)
            {
                if ((board[bgl, 1] == color && board[bgl, 0] == color) || (board[bgl, 6] == color && board[bgl, 7] == color))
                {
                    res += 1;
                }
                else
                {
                    res -= 2;
                }
            }

        }
        for (int bgl = 0; bgl < 8; bgl += 7)
        {
            bool tt = false;
            if ((board[1, bgl] == color || board[6, bgl] == color) && (board[6, bgl] != color || board[1, bgl] != color))
                tt = true;
            if (tt == false)
                continue;
            for (int j = 2; j <= 5; j++)
            {
                if (board[j, bgl] != 0)
                {
                    tt = false;
                    break;
                }
            }
            if (tt == true)
            {
                if ((board[1, bgl] == color && board[0, bgl] == color) || (board[6, bgl] == color && board[7, bgl] == color))
                {
                    res += 1;
                }
                else
                {
                    res -= 2;
                }
            }

        }
        return res;
    }
    // 计算当前行动力
    int calculate_move_ability(int[,] board, int color)
    {
        int res = 0;
        Stack<Vector2Int>[,] nowpossible = basic_valid_map(board, color);
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (nowpossible[i, j].Count > 0)
                {
                    res++;
                    if ((i == 0 || i == 7) && (j == 0 || j == 7))
                    {
                        res += 1;
                    }
                    if ((i == 1 || i == 6) && (j == 1 || j == 6))
                    {
                        res -= 1;
                    }
                }

            }
        for (int bgl = 0; bgl < 8; bgl += 7)
        {
            bool tt = false;
            if (board[bgl, 0] == 0 && board[bgl, 1] == 0 && board[bgl, 6] == 0 && board[bgl, 7] == 0)
                tt = true;
            if (tt == false)
                continue;
            for (int j = 2; j <= 5; j++)
            {
                if (board[bgl, j] != color)
                {
                    tt = false;
                    break;
                }
            }
            if (tt == true)
                res += 2;
        }
        for (int bgl = 0; bgl < 8; bgl += 7)
        {
            bool tt = false;
            if (board[0, bgl] == 0 && board[1, bgl] == 0 && board[6, bgl] == 0 && board[7, bgl] == 0)
                tt = true;
            if (tt == false)
                continue;
            for (int j = 2; j <= 5; j++)
            {
                if (board[j, bgl] != color)
                {
                    tt = false;
                    break;
                }
            }
            if (tt == true)
                res += 2;
        }

        return res;
    }

}
