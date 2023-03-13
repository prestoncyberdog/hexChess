using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpKnightLong : piece
{
    public override void specificInit()
    {
        moveType = JUMP;
        moveRange = 3;
        cost = 30;
        qualityBonus = 0;

        transform.localScale = new Vector3(.6f, .6f, 1);
    }

    public override void specificUpdate()
    {

    }
}
