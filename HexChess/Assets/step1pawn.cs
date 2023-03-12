using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class step1pawn : piece
{
    public override void specificInit()
    {
        moveType = STEP;
        moveRange = 3;

        transform.localScale = new Vector3(1.8f, 1.8f, 1);
    }

    public override void specificUpdate()
    {

    }
}
