using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpPulse : piece
{
    public override void specificInit()
    {
        moveType = JUMP;
        moveRange = 2;
        cost = 20;
        qualityBonus = 10;

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push all adjacent pieces after moving
    public override void useMoveAbility(bool real)
    {
        pushedPieces = null;
        tile targetTile;
        for (int i = 0; i < 6; i++)
        {
            targetTile = realOrHypoTile(real).neighbors[i];
            if (targetTile != null)
            {
                targetTile.pushTile(i, real);
                if (!real && targetTile.thisPushedPiece != null)
                {
                    pushedPieces = new pushedPiece[6];
                    pushedPieces[i] = targetTile.thisPushedPiece;
                    targetTile.thisPushedPiece = null;
                }
            }
        }
    }

    public override void undoMoveAbility()
    {
        bm.undoPushes(pushedPieces);
        pushedPieces = null;
    }
}
