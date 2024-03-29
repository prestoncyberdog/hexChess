﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jumpPulse : piece
{
    public override void specificInit()
    {
        pieceName = "Roc";
        moveType = JUMP;
        moveRange = 2;
        cost = 7;
        maxHealth = 4;
        damage = 0;
        qualityBonus = 1;

        abilityText = "Pushes adjacent pieces after moving";
        transform.localScale = new Vector3(.65f, .65f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push all adjacent pieces after moving
    public override void useMoveAbility(bool real)
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

    public override void undoMoveAbility(bool real)
    {
        bm.undoPushes(pushedPieces, real);
        pushedPieces = null;
    }
}
