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

    public override void useDeathAbility(bool real)
    {
        if (real)
        {
            if (team == 0)
            {
                cost = -1;//will be increased by 1 when piece capture is fully processed
            }
            else
            {
                cost = 0;
            }
        }
    }

    public override void undoDeathAbility(bool real)
    {
        if (real)
        {
            cost = 0;//allows us to undo capture and summon and be at 0 cost
        }
    }

    public override void specificUpdate()
    {

    }
}
