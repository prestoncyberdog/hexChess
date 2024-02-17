using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stepAnt : piece
{
    public override void specificInit()
    {
        pieceName = "Goliath Ant";
        moveType = STEP;
        moveRange = 1;
        cost = 0;
        maxHealth = 1;
        damage = 1;
        qualityBonus = 0;

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }
}
