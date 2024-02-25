using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pieceBallista : piece
{
    public projectile launched;

    public int shootRange;
    public int shootDamage;

    public override void specificInit()
    {
        pieceName = "Ballista";
        moveType = STEP;
        moveRange = 1;
        cost = 6;
        maxHealth = 4;
        damage = 0;
        qualityBonus = 0;

        hasActivatedAbility = true;
        shootRange = 3;
        shootDamage = 2;
        desiredRange = shootRange;
        abilityText = "Shoot damage: " + shootDamage + " + push";
        abilityFearScore = shootDamage + 1;//roughly describes damage of the ability in general

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
        abilityTarget = target.realOrHypoPiece(real);
        attacking = null;
        capturing = null;

        tile currentTile = realOrHypoTile(real);
        int direction = -1;
        for (int i = 0; i < 6; i++)
        {
            if (isInDirection(abilityTarget, i, real))
            {
                direction = i;
                break;
            }
        }
        
        if (real)
        {
            reversableMove thisMove = new reversableMove(this, thisTile, null, null);
            bm.undoStack.Insert(0, thisMove);
            exhausted = true;
            launched = launchProjectile(abilityTarget, shootDamage);
            launched.applyPush = true;
            launched.pushDirection = direction;
        }
        else
        {
            //apply push now since we dont fire a projectile
            if (!abilityTarget.willGetKilled(shootDamage, real))
            {
                target.pushTile(direction, real);
                if (target.thisPushedPiece != null)
                {
                    pushedPieces.Add(target.thisPushedPiece);
                    target.thisPushedPiece = null;
                }
            }
            dealDamage(abilityTarget, shootDamage, real);
            hypoExhausted = true;
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
        unDealDamage(abilityTarget, shootDamage, real);
        abilityTarget = null;
    }
    
    public override bool isValidAbilityTarget(tile target, bool real)
    {
        if (target.realOrHypoPiece(real) != null && target.realOrHypoPiece(real) != this)//only target tiles with pieces we can hit
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
        bm.resetTiles();
        tile startingTile = realOrHypoTile(real);
        startingTile.distance = 0;
        tile activeTile;
        tile otherTile;
        bool continueSearch = true;
        for (int i = 0; i < startingTile.neighbors.Length; i++)
        {
            activeTile = startingTile;
            while (continueSearch)
            {
                continueSearch = false;
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < shootRange &&
                        otherTile.distance > activeTile.distance + 1 &&
                            ((real && (activeTile == thisTile || activeTile.thisPiece == null)) ||
                            (!real && (activeTile == hypoTile || activeTile.hypoPiece == null))))
                    {
                        continueSearch = true;
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= shootRange && ((real && !abilityTargets.Contains(otherTile)) ||
                                                               (!real && !hypoAbilityTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe shoot at
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
                        activeTile = otherTile;
                    }
                }
            }
            continueSearch = true;
        }
    }

    public override bool attackHasNoEffect(piece target, float damageAmount)
    {
        return false;
    }
}
