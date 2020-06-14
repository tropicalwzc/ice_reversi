using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flip : MonoBehaviour
{
    public int flipping_op = 0;
    public float rotater = 1.0f;
    int sander = 0;
    int timer = 0;
    float toangle = 0;
    // Start is called before the first frame update
    void Start()
    {

    }
    void flipnow()
    {
        flipping_op = 1;
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        if (flipping_op != 0)
        {
            if (sander == 0)
            {
                sander = 18;
                flipping_op = 0;
            }
            else
            {
                if (timer == 0)
                    timer++;
            }
        }
        if (sander > 0)
        {
            float rotatechange = rotater;
            toangle += rotatechange;

            this.transform.localEulerAngles = new Vector3(0f, toangle, 180f);
            if (sander > 9)
            {
                this.transform.localPosition = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, this.transform.localPosition.z - 0.5f);
            }
            else
            {
                this.transform.localPosition = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, this.transform.localPosition.z + 0.5f);
            }
            sander--;
        }
        else
        {
            if (timer > 0)
            {
                sander = 18;
                timer--;
            }
        }
    }
    void Update()
    {

    }
}
