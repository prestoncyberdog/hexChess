using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineRook : piece
{
    public override void specificInit()
    {
        pieceName = "Wolf";
        moveType = LINE;
        moveRange = 4;
        cost = 4;
        maxHealth = 4;
        damage = 1;
        qualityBonus = 0;

        transform.localScale = new Vector3(.6f, .6f, 1);
    }

    public override void specificUpdate()
    {

    }
}
