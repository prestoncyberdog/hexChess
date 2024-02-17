using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stepGolem : piece
{
    public override void specificInit()
    {
        pieceName = "Golem";
        moveType = STEP;
        moveRange = 1;
        cost = 6;
        maxHealth = 5;
        damage = 3;
        qualityBonus = 0;

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }
}
