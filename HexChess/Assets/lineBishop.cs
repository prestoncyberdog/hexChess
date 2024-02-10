using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineBishop : piece
{
    public override void specificInit()
    {
        moveType = LINE;
        moveRange = 3;
        cost = 25;
        health = 12;
        damage = 3;
        qualityBonus = 0;

        transform.localScale = new Vector3(2.4f, 2.4f, 1);
    }

    public override void specificUpdate()
    {

    }
}
