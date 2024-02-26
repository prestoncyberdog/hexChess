using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class obstacleTree : obstacle
{
    public override void specificInit()
    {
        transform.localScale = scale * new Vector3(.6f, .6f, 1);
        defaultColor = new Color(0, .8f, 0);
    }

    public override void specificUpdate()
    {
        
    }
}
