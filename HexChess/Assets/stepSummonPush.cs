using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stepSummonPush : piece
{
    public override void specificInit()
    {
        pieceName = "Eidolon";
        moveType = STEP;
        moveRange = 1;
        cost = 2;
        maxHealth = 4;
        damage = 1;
        qualityBonus = -1;

        abilityText = "Pushes adjacent pieces when summoned";
        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push all adjacent pieces after moving
    public override void useSummonAbility(bool real)
    {
        pushedPieces = new List<pushedPiece>();
        tile targetTile;
        for (int i = 0; i < 6; i++)
        {
            targetTile = realOrHypoTile(real).neighbors[i];
            if (targetTile != null)
            {
                targetTile.pushTile(i, real);
                if (targetTile.thisPushedPiece != null)
                {
                    pushedPieces.Add(targetTile.thisPushedPiece);
                    targetTile.thisPushedPiece = null;
                }
            }
        }
        if (pushedPieces.Count == 0)
        {
            pushedPieces = null;
        }
    }

    public override void undoSummonAbility(bool real)
    {
        bm.undoPushes(pushedPieces, real);
        pushedPieces = null;
    }
}
