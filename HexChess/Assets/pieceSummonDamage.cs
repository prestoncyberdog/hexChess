using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pieceSummonDamage : piece
{
    public int summonDamage;

    public override void specificInit()
    {
        pieceName = "Imp";
        moveType = LINE;
        moveRange = 2;
        cost = 3;
        maxHealth = 3;
        damage = 1;
        qualityBonus = -1;

        summonDamage = 1;
        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push all adjacent pieces after moving
    public override void useSummonAbility(bool real)
    {
        damagedPieces = new List<piece>();
        tile targetTile;
        piece targetPiece;
        for (int i = 0; i < 6; i++)
        {
            targetTile = realOrHypoTile(real).neighbors[i];
            if (targetTile != null)
            {
                targetPiece = targetTile.realOrHypoPiece(real);
                if (targetPiece != null && targetPiece.team != team)
                {
                    damagedPieces.Add(targetPiece);
                    dealDamage(targetPiece, summonDamage, real);
                }
            }
        }

        if (damagedPieces.Count == 0)
        {
            damagedPieces = null;
        }
    }

    public override void undoSummonAbility(bool real)
    {
        if (damagedPieces == null)
        {
            return;
        }
        for (int i = 0; i < damagedPieces.Count; i++)
        {
            unDealDamage(damagedPieces[i], summonDamage, real);
        }
        damagedPieces = null;
    }
}
