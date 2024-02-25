using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineBishop : piece
{
    public override void specificInit()
    {
        pieceName = "Ram";
        moveType = LINE;
        moveRange = 2;
        cost = 5;
        maxHealth = 5;
        damage = 1;
        qualityBonus = 0;

        abilityText = "Pushes target";
        attackFearScore = 1;
        transform.localScale = new Vector3(2.4f, 2.4f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push target piece when attacking
    public override void useAttackAbility(piece target, bool real)
    {
        pushedPieces = new List<pushedPiece>();
        tile targetTile;
        for (int i = 0; i < 6; i++)
        {
            if (isInDirection(target, i, real))
            {
                targetTile=target.realOrHypoTile(real);
                targetTile.pushTile(i, real);
                if (targetTile.thisPushedPiece != null)
                {
                    pushedPieces.Add(targetTile.thisPushedPiece);
                    targetTile.thisPushedPiece = null;
                }
                return;
            }
        }
        if (pushedPieces.Count == 0)
        {
            pushedPieces = null;
        }
    }

    public override void undoAttackAbility(bool real)
    {
        bm.undoPushes(pushedPieces, real);
        pushedPieces = null;
    }

    public override bool attackHasNoEffect(piece target, float damageAmount)
    {
        return false;
    }
}
