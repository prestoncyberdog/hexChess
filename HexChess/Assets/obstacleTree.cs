using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class obstacleTree : obstacle
{
    public override void specificInit()
    {
        transform.localScale = scale * new Vector3(1f, 1f, 1);
        defaultColor = new Color(0, .8f, 0);
        transform.Rotate(new Vector3(0, 0, Random.Range(0,360)));
    }

    public override void specificUpdate()
    {
        
    }
}
