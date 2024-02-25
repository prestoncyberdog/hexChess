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

        canDisplaceOnAttack = true;
        abilityText = "Swaps places with another piece";
        attackFearScore = 2;//roughly describes damage of the ability in general

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //push target piece to our starting tile
    public override void useAttackBeginAbility(piece target, bool real)
    {
        abilityTarget = target;
        pushedPieces = new List<pushedPiece>();
        tile targetTile = abilityTarget.realOrHypoTile(real);
        abilityTarget.pushToTile(realOrHypoTile(real), real);
        pushedPieces.Add(targetTile.thisPushedPiece);
        targetTile.thisPushedPiece = null;
        swapping = real;
    }

    //reconnect target piece with its new tile
    public override void postMoveAbilityTrigger(bool real)
    {
        if (abilityTarget != null)
        {
            abilityTarget.realOrHypoTile(real).setRealOrHypoPiece(abilityTarget, real);
            abilityTarget.updateTargeting(real);
        }
    }

    //this happens first in undo move, so send target piece home here
    public override void undoPostMoveAbilityTrigger(bool real)
    {
        bm.undoPushes(pushedPieces, real);
        pushedPieces = null;
    }

    //this is clean up, reconnecting target piece to its tile
    public override void undoAttackBeginAbility(bool real)
    {
        if (abilityTarget != null)
        {
            abilityTarget.realOrHypoTile(real).setRealOrHypoPiece(abilityTarget, real);
            abilityTarget.updateTargeting(real);
            abilityTarget = null;
        }
    }

    public override bool attackHasNoEffect(piece target, float damageAmount)
    {
        return false;
    }
}
