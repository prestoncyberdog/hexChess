using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stepPawn : piece
{
    public override void specificInit()
    {
        moveType = STEP;
        moveRange = 1;
        cost = 10;
        qualityBonus = 0;

        transform.localScale = new Vector3(1.8f, 1.8f, 1);
    }

    public override void specificUpdate()
    {

    }
}
