using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pieceSwapper : piece
{
    public override void specificInit()
    {
        pieceName = "Changeling";
        moveType = JUMP;
        moveRange = 3;
        cost = 5;
        maxHealth = 3;
        damage = 0;
        qualityBonus = 0;

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
