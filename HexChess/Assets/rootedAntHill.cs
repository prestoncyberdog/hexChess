using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rootedAntHill : piece
{
    //public piece summoned;
    public reversableMove summonMove;
    piece summoned;

    public override void specificInit()
    {
        pieceName = "Ant Queen";
        moveType = STEP;
        moveRange = 1;
        cost = 6;
        maxHealth = 5;
        damage = 0;
        qualityBonus = 0;

        hasActivatedAbility = true;
        desiredRange = 1;
        abilityText = "Summons Goliath Ants";
        abilityFearScore = 0;//roughly describes damage of the ability in general
        prepareSummon();

        transform.localScale = new Vector3(.5f, .5f, 1);
    }

    public override void specificUpdate()
    {
        
    }

    //deal damage to target piece
    public override void useActivatedAbility(tile target, bool real)
    {
        activatingAbility = false;

        if (real)
        {
            bm.placeNewPiece(summoned, target);//puts placement onto the undo stack
            summonMove = bm.undoStack[0];
            bm.undoStack.RemoveAt(0);
            reversableMove thisMove = new reversableMove(this, thisTile, null, null);
            bm.undoStack.Insert(0, thisMove);
            exhausted = true;
        }
        else
        {
            summoned.placePiece(target, real);
            summoned.useSummonAbility(real);
            hypoExhausted = true;
            summonMove = new reversableMove(summoned);
        }
    }

    public override void undoActivatedAbility(bool real)
    {
        bm.undoMove(summonMove, real);
        summonMove = null;
    }

    public override void useTurnChangeAbility()
    {
        prepareSummon();
    }

    public void prepareSummon()
    {
        if (summoned != null && !summoned.alive)
        {
            return;
        }
        summoned = Instantiate(gm.Pieces[9], gm.AWAY, Quaternion.identity).GetComponent<piece>();
        summoned.team = team;
        summoned.ephemeral = true;
        summoned.init();
    }

    public override void destroyAll()
    {
        summoned.destroyAll();
        if (thisHealthBar != null)
        {
            thisHealthBar.destroyAll();
        }
        Destroy(gameObject);
    }
    
    public override bool isValidAbilityTarget(tile target, bool real)
    {
        if (target.isOpen(real))//only empty tiles
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
        tile activeTile =  realOrHypoTile(real);
        tile otherTile;
        for (int i = 0;i<6;i++)
        {
            if (activeTile.neighbors[i] != null)
            {
                otherTile = activeTile.neighbors[i];
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
