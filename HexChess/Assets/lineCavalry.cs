using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineCavalry : piece
{
    public override void specificInit()
    {
        pieceName = "Knight";
        moveType = LINE;
        moveRange = 2;
        cost = 5;
        maxHealth = 4;
        damage = 2;
        qualityBonus = 0;

        transform.localScale = new Vector3(.6f, .6f, 1);
    }

    public override void specificUpdate()
    {

    }
}
