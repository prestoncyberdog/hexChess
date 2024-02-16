using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpKnight : piece
{
    public override void specificInit()
    {
        pieceName = "Chimera";
        moveType = JUMP;
        moveRange = 2;
        cost = 4;
        maxHealth = 4;
        damage = 2;
        qualityBonus = 0;

        transform.localScale = new Vector3(.6f, .6f, 1);
    }

    public override void specificUpdate()
    {

    }
}
