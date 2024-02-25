﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stepArcher : piece
{
    public int shootRange;
    public int shootDamage;

    public override void specificInit()
    {
        pieceName = "Archer";
        moveType = STEP;
        moveRange = 1;
        cost = 4;
        maxHealth = 2;
        damage = 0;
        qualityBonus = 0;

        hasActivatedAbility = true;
        shootRange = 2;
        shootDamage = 1;
        desiredRange = shootRange;
        abilityText = "Shoot damage: " + shootDamage;
        abilityFearScore = shootDamage;//roughly describes damage of the ability in general

        transform.localScale = new Vector3(2.5f, 2.5f, 1);
    }

    public override void specificUpdate()
    {

    }

    //deal damage to target piece
    public override void useActivatedAbility(tile target, bool real)
    {
        activatingAbility = false;
        abilityTarget = target.realOrHypoPiece(real);
        attacking = null;
        capturing = null;
        if (real)
        {
            reversableMove thisMove = new reversableMove(this, thisTile, null, null);
            bm.undoStack.Insert(0, thisMove);
            exhausted = true;
            launchProjectile(abilityTarget, shootDamage);
        }
        else
        {
            dealDamage(abilityTarget, shootDamage, real);
            hypoExhausted = true;
            if (attackHasNoEffect(abilityTarget, shootDamage) && abilityTarget.team == team)
            {
                wastingAttackOnAlly = true;
            }
        }
    }

    public override void undoActivatedAbility(bool real)
    {
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
        Queue q = new Queue();
        bm.resetTiles();
        tile activeTile =  realOrHypoTile(real);
        tile otherTile;
        q.Enqueue(activeTile);
        activeTile.distance = 0;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            for (int i = 0; i < activeTile.neighbors.Length; i++)
            {
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < shootRange && otherTile.distance > activeTile.distance + 1)
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= shootRange && ((real && !abilityTargets.Contains(otherTile)) ||
                                                                    (!real && !hypoAbilityTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe attack
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
            }
        }
    }
}
