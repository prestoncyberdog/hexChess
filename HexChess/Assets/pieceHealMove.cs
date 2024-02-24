using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pieceHealMove : piece
{
    public int healAmount;
    public override void specificInit()
    {
        pieceName = "Nymph";
        moveType = STEP;
        moveRange = 1;
        cost = 4;
        maxHealth = 3;
        damage = 0;
        qualityBonus = 0;

        healAmount = 1;
        abilityText = "Heal amount: " + healAmount;
        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push all adjacent pieces after moving
    public override void useMoveAbility(bool real)
    {
        healedPieces = new List<healedPiece>();
        tile targetTile;
        piece targetPiece;
        int healResult;
        for (int i = 0; i < 6; i++)
        {
            targetTile = realOrHypoTile(real).neighbors[i];
            if (targetTile != null)
            {
                targetPiece = targetTile.realOrHypoPiece(real);
                if (targetPiece != null && targetPiece.team == team)//only heal allies
                {
                    healResult = giveHeal(targetPiece, healAmount, real);
                    if (healResult > 0)
                    {
                        healedPiece healed = new healedPiece(targetPiece, healResult);
                        healedPieces.Add(healed);
                    }
                }
            }
        }
        if (healedPieces.Count == 0)
        {
            healedPieces = null;
        }
    }

    public override void undoMoveAbility(bool real)
    {
        bm.undoHeals(healedPieces, real);
        healedPieces = null;
    }
}
