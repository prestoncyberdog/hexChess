using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pieceDamageScaler : piece
{
    public int baseDamage;
    public float baseValue;
    public float valueGain;

    public override void specificInit()
    {
        pieceName = "Rampaging Bear";
        moveType = LINE;
        moveRange = 2;
        cost = 6;
        maxHealth = 5;
        damage = 1;
        qualityBonus = -2;

        abilityText = "Gains damage after a kill";
        valueGain = 1.5f;
        baseDamage = damage;
        baseValue = -1;
        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void useKillAbility(bool real)
    {
        if (baseValue == -1)
        {
            baseValue = value;//set after piece init is done but before any value changes
        }

        if (real)
        {
            damage++;
            if (team == 0)//enemy would update value from the hypo move
            {
                value += valueGain;
            }
            bm.um.pm.updateText();
        }
        else
        {
           hypoDamage++;
           value += valueGain;
        }
    }

    public override void undoKillAbility(bool real)
    {
        if (real)
        {
            damage--;
            if (team == 0)
            {
                value -= valueGain;
            }
            bm.um.pm.updateText();
        }
        else
        {
           hypoDamage--;
           value -= valueGain;
        }
    }

    public override void useCommittedDeathAblity()
    {
        damage = baseDamage;
        if (baseValue != -1)
        {
            value = baseValue;
        }
    }

    public override void specificUpdate()
    {

    }
}
