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
        maxHealth = 12;
        damage = 3;
        qualityBonus = 0;

        transform.localScale = new Vector3(2.4f, 2.4f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push target piece when attacking
    public override void useAttackAbility(piece target, bool real)
    {
        pushedPieces = null;
        tile targetTile;
        for (int i = 0; i < 6; i++)
        {
            if (isInDirection(target, i, real))
            {
                targetTile=target.realOrHypoTile(real);

                targetTile.pushTile(i, real);
                if (!real && targetTile.thisPushedPiece != null)
                {
                    pushedPieces = new pushedPiece[1];
                    pushedPieces[0] = targetTile.thisPushedPiece;
                    targetTile.thisPushedPiece = null;
                }
                return;
            }
        }
    }

    public override void undoAttackAbility()
    {
        bm.undoPushes(pushedPieces);
        pushedPieces = null;
    }
}
