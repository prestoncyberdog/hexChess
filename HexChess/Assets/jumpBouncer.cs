using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpBouncer : piece
{
    public override void specificInit()
    {
        pieceName = "Bloodwing";
        moveType = JUMP;
        moveRange = 2;
        cost = 7;
        maxHealth = 3;
        damage = 2;
        qualityBonus = 0;

        abilityText = "Can move again after a kill";
        transform.localScale = new Vector3(.7f, .7f, 1);
    }

    public override void useKillAbility(bool real)
    {
        if (real)
        {
            exhausted = false;
        }
        else
        {
            hypoExhausted = false;
        }
    }

    public override void undoKillAbility(bool real)
    {
        //actually don't need to do anything, since we won't be exhausted after move undo
    }

    public override void specificUpdate()
    {

    }
}
