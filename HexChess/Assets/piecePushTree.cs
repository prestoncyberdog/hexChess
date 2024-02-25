using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class piecePushTree : piece
{
    public override void specificInit()
    {
        pieceName = "Authoritree";
        moveType = ROOTED;
        moveRange = 0;
        cost = 4;
        maxHealth = 6;
        damage = 0;
        qualityBonus = 0;

        hasActivatedAbility = true;
        abilityText = "Pushes an adjacent piece";
        abilityFearScore = 1;//roughly describes damage of the ability in general

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //deal damage to target piece
    public override void useActivatedAbility(tile target, bool real)
    {
        pushedPieces = new List<pushedPiece>();
        activatingAbility = false;
        attacking = null;
        capturing = null;
        if (real)
        {
            reversableMove thisMove = new reversableMove(this, thisTile, null, null);
            bm.undoStack.Insert(0, thisMove);
            exhausted = true;
        }
        else
        {
            hypoExhausted = true;
        }

        piece targetPiece = target.realOrHypoPiece(real);
        for (int i = 0; i < 6; i++)
        {
            if (isInDirection(targetPiece, i, real))
            {
                target.pushTile(i, real);
                if (target.thisPushedPiece != null)
                {
                    pushedPieces.Add(target.thisPushedPiece);
                    target.thisPushedPiece = null;
                }
                return;
            }
        }
        if (pushedPieces.Count == 0)
        {
            pushedPieces = null;
        }
    }

    public override void undoActivatedAbility(bool real)
    {
        bm.undoPushes(pushedPieces, real);
        pushedPieces = null;
    }
    
    public override bool isValidAbilityTarget(tile target, bool real)
    {
        if (target.realOrHypoPiece(real) != null && target.realOrHypoPiece(real) != this  && target.realOrHypoPiece(real).moveType != ROOTED)//only target tiles with pieces we can push
        {
            return true;
        }
        return false;
    }

    public override void findAbilityTargets(bool real)
    {
        if (real)
        {
            abilityTargets = new List<tile>();
        }
        else
        {
            hypoAbilityTargets = new List<tile>();
        }

        tile startingTile = realOrHypoTile(real);
        tile otherTile;
        for (int i = 0; i < startingTile.neighbors.Length; i++)
        {
            otherTile = startingTile.neighbors[i];
            if (otherTile != null)
            {
                if (real)
                {
                    abilityTargets.Add(otherTile);
                    otherTile.abilityTargetedBy.Add(this);
                }
                else
                {
                    hypoAbilityTargets.Add(otherTile);
                    otherTile.abilityHypoTargetedBy.Add(this);
                }
            }
        }
    }

    public override bool attackHasNoEffect(piece target, float damageAmount)
    {
        return false;
    }
}
