using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class step1pawn : piece
{
    public override void specificInit()
    {
        moveType = LINE;
        moveRange = 3;

        transform.localScale = new Vector3(0.3f, 0.3f, 1);
    }

    public override void specificUpdate()
    {

    }
}
