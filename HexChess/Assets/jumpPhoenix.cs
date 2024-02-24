using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpPhoenix : piece
{
    public override void specificInit()
    {
        pieceName = "Phoenix";
        moveType = JUMP;
        moveRange = 2;
        cost = 5;
        maxHealth = 2;
        damage = 2;
        qualityBonus = -5;

        resummonableByEnemy = true;

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void useCommittedDeathAblity()
    {
        cost = 0;
    }

    public override void specificUpdate()
    {

    }
}
