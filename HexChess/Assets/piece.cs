using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class piece : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public const int STEP = 0;
    public const int LINE = 1;
    public const int JUMP = 2;
    public const int ROOTED = 3;

    public Color playerColor;
    public Color exhaustedPlayerColor;
    public Color enemyColor;
    public Color exhaustedEnemyColor;

    public int moveType;
    public int moveRange;
    public int minAttackRange;
    public float value;
    public int cost;
    public float qualityBonus;
    public int team;
    public bool champion;
    public List<tile> intentions;//used by AI

    public tile thisTile;
    public bool exhausted;
    public bool alive;
    public List<tile> targets;
    public tile hypoTile;
    public bool hypoExhausted;
    public bool hypoAlive;
    public List<tile> hypoTargets;
    public List<tile> abilityTargets;
    public List<tile> hypoAbilityTargets;
    public float potentialIncomingDamage;
    public float pieceTargetingPenalty;

    public string pieceName;
    public int health;
    public int maxHealth;
    public int damage;
    public int hypoDamage;
    public int hypoHealth;
    public healthBar thisHealthBar;

    public teamSlot thisSlot;

    public tile newTile;
    public tile oldTile;
    public tile pushedTile;
    public tile hypoPushedTile;
    public List<pushedPiece> pushedPieces;
    public List<healedPiece> healedPieces;
    public List<piece> damagedPieces;
    public tile turnStartTile;
    public piece capturing;
    public piece attacking;
    public bool inactive;
    public bool wastingAttackOnAlly;
    public bool readyToSummon;
    public bool beingSummoned;
    public List<tile> stepPath;
    public bool ephemeral;//for pieces summoned once only by player
    public bool resummonableByEnemy;

    public bool hasActivatedAbility;
    public bool activatingAbility;
    public bool usedActivatedAbility;
    public string abilityText;
    public float abilityFearScore;
    public int desiredRange;
    public buttonAbility activateButton;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        playerColor = new Color(0.2f, 0.2f, 1f);
        exhaustedPlayerColor = playerColor;// new Color(0.4f, 0.4f, 1f);
        enemyColor = new Color(1f, 0.2f, 0.2f);
        exhaustedEnemyColor = enemyColor;// new Color(1f, 0.4f, 0.4f);
        List<tile> intentions = new List<tile>();

        cost = 1;
        maxHealth = 5;
        damage = 3;
        champion = false;
        exhausted = false;
        resummonableByEnemy = false;
        if (thisTile != null)
        {
            alive = true;
            if (!bm.alivePieces.Contains(this))
            {
                bm.alivePieces.Add(this);
            }
        }
        specificInit();
        value = Mathf.Max((cost + qualityBonus + 1), 1);//may want to randomize this slightly?
        health = maxHealth;
        hypoHealth = health;
        hypoDamage = damage;
        transform.localScale = new Vector3(transform.localScale.x * bm.generator.tileScale, transform.localScale.y * bm.generator.tileScale, 1);
        minAttackRange = 1;
        if (moveType == JUMP)
        {
            minAttackRange = moveRange;
        }

        if (hasActivatedAbility)
        {
            createAbilityButton();
        }

        updateTargeting(true);
        setColor();
    }

    void Update()
    {
        if (bm.selectedPiece != null && bm.selectedPiece == this && activateButton != null && canUseAbility() && !activatingAbility)
        {
            activateButton.reactivate();
            thisHealthBar.hideText();
        }
    }

    public virtual void specificInit(){}
    public virtual void specificUpdate(){}

    public virtual void useAttackAbility(piece target, bool real){}
    public virtual void undoAttackAbility(bool real){}

    public virtual void useMoveAbility(bool real){}
    public virtual void undoMoveAbility(bool real){}

    public virtual void useSummonAbility(bool real){}
    public virtual void undoSummonAbility(bool real){}

    public virtual void useActivatedAbility(tile target, bool real){}
    public virtual void undoActivatedAbility(bool real){}
    public virtual void findAbilityTargets(bool real){}
    public virtual bool isValidAbilityTarget(tile target, bool real){return false;}

    public virtual void useKillAbility(bool real){}
    public virtual void undoKillAbility(bool real){}

    public virtual void useDeathAbility(bool real){}
    public virtual void undoDeathAbility(bool real){}
    public virtual void useCommittedDeathAblity(){}

    public virtual void useTurnChangeAbility(){}

    public virtual void useAttackBeginAbility(piece target, bool real){}
    public virtual void undoAttackBeginAbility(bool real){}

    public virtual void destroyAll()
    {
        if (thisHealthBar != null)
        {
            thisHealthBar.destroyAll();
        }
        Destroy(gameObject);
    }

    public void placePiece(tile targetTile, bool real)
    {
        if (real)
        {
            exhausted = true;
            alive = true;
            if (!bm.alivePieces.Contains(this))
            {
                bm.alivePieces.Add(this);
            }
            //newTile = targetTile;
            thisTile = targetTile;
            thisTile.thisPiece = this;
            if (thisSlot != null)
            {
                thisSlot.thisPiece = null;
                thisSlot = null;
            }
            if (thisHealthBar == null)
            {
                thisHealthBar = Instantiate(gm.HealthBar, gm.AWAY, Quaternion.identity).GetComponent<healthBar>();
                thisHealthBar.owner = this;
            }
            else
            {
                thisHealthBar.reactivate();
                thisHealthBar.setPositions();
            }
        }
        else //here its a move on the hypo board
        {
            hypoAlive = true;
            hypoTile = targetTile;
            hypoTile.hypoPiece = this;
            hypoExhausted = true;
        }
        updateTargeting(real);
        if (real)
        {
            bm.resetHighlighting();
        }
    }

    //begins moving to new tile, updates location and targeting info
    public void moveToTile(tile targetTile, bool real)
    {
        tile possibleOldTile;
        if (real)
        {
            if (pushedTile == null && targetTile.thisPiece != null && targetTile.thisPiece != this && !targetTile.thisPiece.willGetCaptured(this, real))
            {
                //here, we will deal damage but not capture, so our resulting tile will not be the original target tile
                attacking = targetTile.thisPiece;
                targetTile = findEndingTile(attacking, real);
            }
            else if (pushedTile == null && targetTile.thisPiece != null && targetTile.thisPiece != this)
            {
                capturing = targetTile.thisPiece;
            }
            /*if (pushedTile == null)
            {
                exhausted = true;
            }*/
            possibleOldTile = thisTile;
            oldTile = possibleOldTile;
            possibleOldTile.thisPiece = null;
            possibleOldTile.setColor();
            newTile = targetTile;
            thisTile = targetTile;
            thisTile.thisPiece = this;
            thisTile.setColor();
        }
        else //here its a move on the hypo board
        {
            if (hypoPushedTile == null && targetTile.hypoPiece != null && targetTile.hypoPiece != this && !targetTile.hypoPiece.willGetCaptured(this, real))
            {
                //here, we will deal damage but not capture, so our resulting tile will not be the original target tile
                targetTile = findEndingTile(targetTile.hypoPiece, real);
                //enemy manager should handle the hypothetical damage and capturing
            }
            possibleOldTile = hypoTile;
            possibleOldTile.hypoPiece = null;
            hypoTile = targetTile;
            hypoTile.hypoPiece = this;
            if ((hypoPushedTile == null) && (hypoTile != possibleOldTile) && (hypoExhausted == false))//should not happen when pushed or undoing a move
            {
                useMoveAbility(false);
            }
            if (hypoPushedTile == null)
            {
                hypoExhausted = true;
            }
        }
        if (champion)
        {
            bm.generator.findDistsToChampions(real);
        }
        List<piece> retargeted = new List<piece>();
        updateTargeting(real, ref retargeted);
        possibleOldTile.updateTargeting(real, ref retargeted);
        targetTile.updateTargeting(real, ref retargeted);
    }

    //allows us to delay coloring changes, captures, and sound effects, our final location is already changed in moveToTile
    public IEnumerator moveTowardsNewTile()
    {
        bool temp = exhausted;
        exhausted = true;
        bm.resetHighlighting();
        exhausted = temp;
        float moveRateThisMovement = gm.moveRate;
        if (pushedTile != null)
        {
            moveRateThisMovement = gm.pushMoveRate;
        }
        else if (team == 0)
        {
            moveRateThisMovement = gm.moveRate * 2;
        }

        bm.movingPieces++;
        stepPath = new List<tile>();
        if (attacking != null)
        {
            stepPath.Add(attacking.thisTile);
        }
        if (pushedTile != null && pushedTile != newTile)//here we bumped into the pushedTile but didn't end up there
        {
            stepPath.Add(pushedTile);
        }
        if (moveType == JUMP || moveType == LINE)
        {
            stepPath.Add(newTile);
        }
        else
        {
            findStepPath(newTile);
        }
        while (stepPath.Count > 0)
        {
            Vector3 toNextTile = stepPath[0].transform.position - transform.position;
            if (moveRateThisMovement * Time.deltaTime > toNextTile.magnitude)//here, we've arrived at a waypoint
            {
                if (attacking != null && stepPath[0] == attacking.thisTile)
                {
                    dealDamage(attacking, damage, true);
                    //attacking = null;
                }
                if (pushedTile != null && pushedTile != newTile && stepPath[0] == pushedTile)
                {
                    collideWithPiece(pushedTile.thisPiece, true);
                    if (!alive)
                    {
                        bm.movingPieces--;
                        yield break;
                    }
                }
                stepPath.RemoveAt(0);
            }
            else
            {
                transform.position = transform.position + toNextTile.normalized * moveRateThisMovement * Time.deltaTime;
                thisHealthBar.setPositions();
            }
            yield return null;
        }
        arriveOnTile();
        bm.movingPieces--;
    }

    //take care of delayed effects of move like coloring, capturing, and sound effects. Damage/capturing happens here
    public void arriveOnTile()
    {
        transform.position = newTile.transform.position;
        setColor();

        if (pushedTile == null && exhausted == false && !bm.undoing)//make sure its a real forward move by us
        {
            exhausted = true;
            if (newTile != oldTile)//make sure to not use ability on move undo, when pushed, or when stationary
            {
                useMoveAbility(true);
            }
            //store info about move so it can be undone later
            reversableMove thisMove = new reversableMove(this, oldTile, attacking, capturing);
            bm.undoStack.Insert(0, thisMove);
        }

        if (capturing != null)
        {
            capturing.getCaptured(true);
            thisTile.thisPiece = this;
            updateTargeting(true);
            capturing = null;
            useKillAbility(true);
        }
        newTile = null;
        oldTile = null;
        pushedTile = null;
        attacking = null;
        bm.resetHighlighting();
        thisHealthBar.setPositions();
    }

    //finds the tile we'd end up on if we attacked target
    public tile findEndingTile(piece target, bool real)
    {
        if (target.willGetCaptured(this, real))
        {
            if (real)
            {
                return target.thisTile;
            }
            else
            {
                return target.hypoTile;
            }
        }

        tile startTile = realOrHypoTile(real);
        if (moveType != LINE)
        {
            return startTile;
        }
        for (int i = 0;i < 6;i++)
        {
            if (target.isInDirection(this, i, real))
            {
                return target.realOrHypoTile(real).neighbors[i];
            }
        }
        return startTile;
    }

    public void pushPiece(int direction, bool real)
    {
        if (moveType == ROOTED)
        {
            return;
        }
        tile currTile = realOrHypoTile(real);
        piece otherPiece = null;
        if (currTile.neighbors[direction] == null)
        {
            return;
        }
        
        if (real)
        {
            pushedTile = currTile.neighbors[direction];
            oldTile = thisTile;
            if (pushedTile.thisPiece == null && pushedTile.obstacle == 0)
            {
                newTile = pushedTile;
                moveToTile(pushedTile, real);
            }
            else // here we've bumped into pushedTile
            {
                newTile = thisTile;
                otherPiece = pushedTile.thisPiece;
            }
            StartCoroutine(this.moveTowardsNewTile());

            storePushedPieceInfo(currTile, otherPiece);
        }
        else
        {
            hypoPushedTile = currTile.neighbors[direction];//needed for moveToTile
            otherPiece = hypoPushedTile.hypoPiece;
            if ((otherPiece == null || (otherPiece.hypoAlive == false)) && hypoPushedTile.hypoObstacle == 0)
            {
                moveToTile(hypoPushedTile, real);
            }
            else if (otherPiece != null)
            {
                collideWithPiece(otherPiece, real);
            }
            storePushedPieceInfo(currTile, otherPiece);
            hypoPushedTile = null;
        }
    }

    public void collideWithPiece(piece other, bool real)
    {
        takeDamage(gm.pushDamage, real);
        other.takeDamage(gm.pushDamage, real);
    }

    public void dealDamage(piece target, int amount, bool real)
    {
        target.takeDamage(amount, real);
        useAttackAbility(target, real);
    }

    //this reverses our hypothetical attack
    public void unDealDamage(piece target, int amount, bool real)
    {
        undoAttackAbility(real);
        target.unTakeDamage(amount, real);
    }

    //returns amount of heal actually given, accounting for the other piece's max health
    public int giveHeal(piece target, int amount, bool real)
    {
        int healGiven = 0;
        if (real)
        {
            if (target.maxHealth - target.health >= amount)
            {
                healGiven = amount;
                target.health += amount;
            }
            else
            {
                healGiven = target.maxHealth - target.health;
                target.health = target.maxHealth;
            }
            target.thisHealthBar.setColors();
        }
        else
        {
            if (target.maxHealth - target.hypoHealth >= amount)
            {
                healGiven = amount;
                target.hypoHealth += amount;
            }
            else
            {
                healGiven = target.maxHealth - target.hypoHealth;
                target.hypoHealth = target.maxHealth;
            }
        }
        return healGiven;
    }

    //different from undealing damage because it ignores any armor or other effects
    public void unGetHeal(int amount, bool real)
    {
        if (real)
        {
            health -= amount;
            thisHealthBar.setColors();
        }
        else
        {
            hypoHealth -= amount;
        }
    }

    public void takeDamage(int amount, bool real)
    {
        if (real)
        {
            health -= amount;
            thisHealthBar.setColors();
            if (health <= 0)
            {
                getCaptured(real);
            }
        }
        else
        {
            hypoHealth -= amount;
            if (hypoHealth <= 0)
            {
                getCaptured(real);
            }
        }
    }
    
    public void unTakeDamage(int amount, bool real)
    {
        if (real)
        {
            health += amount;
            thisHealthBar.setColors();
            if (!alive)//come back to life
            {
                unGetCaptured(thisTile, real);
            }
        }
        else
        {
            hypoHealth += amount;
            if (!hypoAlive)
            {
                unGetCaptured(hypoTile, real);
            }
        }
    }

    //returns how much damage this piece would take after any damage reduction (in theory)
    public float expectedDamage(float amount)
    {
        return amount;//careful to not let this be negative
    }

    public bool isValidCandidate(tile target, bool real)
    {
        if (real)
        {
            return (target.obstacle == 0 &&
                    exhausted == false);
        }
        else
        {
            bool wastingAttackOnAlly = (target.hypoPiece != null && target.hypoPiece.team == team && attackHasNoEffect(target.hypoPiece, hypoDamage));
            return (target.hypoObstacle == 0 &&
                    hypoExhausted == false &&
                    !wastingAttackOnAlly);// && (target.hypoPiece == null || target.hypoPiece.team != team));
        }
    }

    //returns whether an attack will capture this piece
    public bool willGetCaptured(piece attacker, bool real)
    {
        return (( real && attacker.damage >= health) ||
                (!real && attacker.hypoDamage >= hypoHealth));
    }

    //returns whether an attack will capture this piece
    public bool willGetKilled(int incomingDamage, bool real)
    {
        return (( real && incomingDamage >= health) ||
                (!real && incomingDamage >= hypoHealth));
    }

    //starts from this piece and looks in a direction for another piece, only looking along a straight line
    public bool isInDirection(piece otherPiece, int dir, bool real)
    {
        tile currTile = realOrHypoTile(real);
        while (currTile.neighbors[dir] != null)
        {
            currTile = currTile.neighbors[dir];
            if (otherPiece.realOrHypoTile(real) == currTile)
            {
                return true;
            }
        }
        return false;
    }

    public bool canAfford()
    {
        return ((team == 0 && bm.playerEnergy >= cost) || (team == 1 && bm.enemyEnergy >= cost));
    }

    public void storePushedPieceInfo(tile formerTile, piece otherPiece)
    {
        pushedPiece storage = new pushedPiece();
        storage.thisPiece = this;
        storage.startingTile = formerTile;
        storage.pushedInto = otherPiece;
        formerTile.thisPushedPiece = storage;
    }

    public void payEnergyCost()
    {
        bm.playsRemaining--;
        if (team == 0)
        {
            bm.playerEnergy -= cost;
        }
        else if (team == 1)
        {
            bm.enemyEnergy -= cost;
        }
    }

    public void refundEnergyCost()
    {
        bm.playsRemaining++;
        if (team == 0)
        {
            bm.playerEnergy += cost;
        }
        else if (team == 1)
        {
            bm.enemyEnergy += cost;
        }
    }

    //updates target list and targetedBy list for each affected tile
    public void updateTargeting(bool real)
    {
        List<piece> retargeted = new List<piece>();
        updateTargeting(real, ref retargeted);
    }

    //updates target list and targetedBy list for each affected tile
    public void updateTargeting(bool real, ref List<piece> retargeted)
    {
        if (retargeted != null && retargeted.Contains(this))
        {
            return;
        }
        clearTargets(real);
        if (realOrHypoTile(real) == null)
        {
            return;
        }
        findAllCandidates(real);
        retargeted.Add(this);
        realOrHypoTile(real).updateTargeting(real, ref retargeted);
        findAbilityTargets(real);
    }

    //causes this piece to die, has nothing to do with capturing piece
    public void getCaptured(bool real)
    {
        if (!bm.undoing)
        {
            useDeathAbility(real);
        }
        if (real)
        {
            alive = false;
            thisTile.thisPiece = null;
            updateTargeting(real);//removes all targeting
            bm.alivePieces.Remove(this);
            bm.recentlyCaptured.Add(this);
            if (champion)//shouldnt matter once battle end is implemented
            {
                bm.generator.findDistsToChampions(real);
                if (real)
                {
                    bm.generator.findDistsToChampions(false);
                }
            }
            if (team == 0)
            {
                //thisHealthBar.destroyAll();
                bm.em.playerPieces.Remove(this);
                //cost = Mathf.Min(cost * 2, 999);
                //moveToSlot(bm.um.findOpenSlot());
            }
            if (team == 1)
            {
                bm.em.enemyPieces.Remove(this);
                //Destroy(gameObject);
            }
            transform.position = gm.AWAY;
            thisHealthBar.setPositions();
            bm.resetHighlighting();
        }
        else
        {
            hypoAlive = false;
            hypoTile.hypoPiece = null;
            updateTargeting(real);//removes all hypoTargeting
        }
    }

    public void unGetCaptured(tile location, bool real)
    {
        bool oldExhausted;
        if (real)
        {
            oldExhausted = exhausted;
        }
        else
        {
            oldExhausted = hypoExhausted;
        }
        placePiece(location, real);//unfortunately sets exhausted now
        if (real)
        {
            bm.recentlyCaptured.Remove(this);
            transform.position = thisTile.transform.position;
            thisHealthBar.setPositions();
            thisHealthBar.setColors();
            exhausted = oldExhausted;
        }
        else
        {
            hypoExhausted = oldExhausted;
        }
        undoDeathAbility(real);
    }

    public int getDesiredRange()
    {
        if (desiredRange == 0)
        {
            return minAttackRange;
        }
        return desiredRange;
    }

    //moves piece to other slot, swapping if it was occupied
    public void moveToSlot(teamSlot newSlot)
    {
        if (thisSlot != null)
        {
            teamSlot oldSlot = thisSlot;
            oldSlot.thisPiece = newSlot.thisPiece;
            if (newSlot.thisPiece != null)
            {
                newSlot.thisPiece.thisSlot = oldSlot;
                newSlot.thisPiece.transform.position = oldSlot.transform.position;
            }
        }
        thisSlot = newSlot;
        newSlot.thisPiece = this;
        transform.position = newSlot.transform.position;
        health = maxHealth;
        if (thisHealthBar != null)
        {
            thisHealthBar.destroyAll();
        }
    }

    public void setColor()
    {
        if (team == 0 && !exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = playerColor;
        }
        else if (team == 0 && exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = exhaustedPlayerColor;
        }
        else if (team == 1 && !exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = enemyColor;
        }
        else if (team == 1 && exhausted)
        {
            this.GetComponent<SpriteRenderer>().color = exhaustedEnemyColor;
        }
    }

    public void deleteInfo()
    {
        thisTile = null;
        hypoTile = null;
        pushedTile = null;
        hypoPushedTile = null;
        targets = new List<tile>();
        hypoTargets = new List<tile>();
        health = maxHealth;
        hypoHealth = maxHealth;
    }

    public void highlightAbilityCandidates()
    {
        if (!alive)
        {
            return;
        }
        findAbilityTargets(true);
        thisTile.gameObject.GetComponent<SpriteRenderer>().color = thisTile.selectedColor;
        for (int i = 0;i< abilityTargets.Count;i++)
        {
            if (isValidAbilityTarget(abilityTargets[i], true))
            {
                abilityTargets[i].gameObject.GetComponent<SpriteRenderer>().color = abilityTargets[i].candidateColor;
            }
        }
    }

    public void highlightCandidates()
    {
        if (!alive)
        {
            return;
        }
        thisTile.gameObject.GetComponent<SpriteRenderer>().color = thisTile.selectedColor;
        for (int i = 0;i< targets.Count;i++)
        {
            if (isValidCandidate(targets[i], true))
            {
                targets[i].gameObject.GetComponent<SpriteRenderer>().color = targets[i].candidateColor;
            }
        }
    }

    private void findAllCandidates(bool real)
    {
        if ((real && !alive) || (!real && !hypoAlive))
        {
            return;
        }
        bm.resetTiles();
        if (real)
        {
            targets = new List<tile>();
        }
        else
        {
            hypoTargets = new List<tile>();
        }

        if (moveRange == 1)
        {
            planPathsRange1(real);
        }
        else if (moveType == ROOTED)
        {
            //don't need to do anything, no target tiles
        }
        else if (moveType == STEP)
        {
            planPathsWithObtacles(real); 
        }
        else if (moveType == JUMP)
        {
            planPathsWithoutObtacles(real);
        }
        else if (moveType == LINE)
        {
            planPathsInALine(real);
        }
    }

    public void startUsingAbility()
    {
        if (canUseAbility())
        {
            for (int i = 0; i<bm.allTiles.Length; i++)
            {
                bm.allTiles[i].setColor();
            }
            highlightAbilityCandidates();
            activatingAbility = true;
            //activateButton.deactivate();
        }
    }

    public void createAbilityButton()
    {
        activateButton = Instantiate(gm.ButtonAbility, gm.AWAY, Quaternion.identity).GetComponent<buttonAbility>();
        activateButton.owner = this;
        activateButton.init();
    }

    public bool canUseAbility()
    {
        return bm != null && bm.selectedPiece == this && bm.playersTurn && team == 0 && alive && !exhausted && hasActivatedAbility;
    }

    public projectile launchProjectile(piece target, int projectileDamage)
    {
        projectile launch;
        launch = Instantiate(gm.Projectile, gm.AWAY, Quaternion.identity).GetComponent<projectile>();
        launch.source = this;
        launch.target = target;
        launch.damage = projectileDamage;
        bm.movingPieces++;
        launch.init();
        return launch;
    }

    public virtual bool attackHasNoEffect(piece target, float damageAmount)
    {
        return target.expectedDamage(damageAmount) == 0;
    }

    //fills targets or hypo targets with adjacent tiles, since any move type works the same at range 1
    public void planPathsRange1 (bool real)
    {
        tile activeTile = realOrHypoTile(real);
        tile otherTile;
        for (int i = 0; i < activeTile.neighbors.Length; i++)
        {
            if (activeTile.neighbors[i] != null)
            {
                otherTile = activeTile.neighbors[i];
                if (real && !targets.Contains(otherTile))
                {
                    otherTile.targetedBy.Add(this);
                    targets.Add(otherTile);
                }
                else if (!real && !hypoTargets.Contains(otherTile))
                {
                    otherTile.hypoTargetedBy.Add(this);
                    hypoTargets.Add(otherTile);
                }
            }
        }
    }

    //breadth first search of tiles not recursing on unavailable tiles
    //outputs targets or hypoTargets
    private void planPathsWithObtacles(bool real)
    {
        Queue q = new Queue();
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
                    if (activeTile.distance < moveRange && 
                        otherTile.distance > activeTile.distance + 1 && 
                            ((real && (activeTile == thisTile || activeTile.thisPiece == null)) ||
                            (!real && (activeTile == hypoTile || activeTile.hypoPiece == null))))
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange && ((real && !targets.Contains(otherTile)) ||
                                                               (!real && !hypoTargets.Contains(otherTile))))  // here, otherTile is a target we can maybe move to
                        {
                            if (real)
                            {
                                otherTile.targetedBy.Add(this);
                                targets.Add(otherTile);
                            }
                            else
                            {
                                otherTile.hypoTargetedBy.Add(this);
                                hypoTargets.Add(otherTile);
                            }
                        }
                    }
                }
            }
        }
    }

    //breadth first search of tiles that will recurse on unavailable tiles
    private void planPathsWithoutObtacles(bool real)
    {
        Queue q = new Queue();
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
                    if (activeTile.distance < moveRange && otherTile.distance > activeTile.distance + 1)
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance == moveRange && ((real && !targets.Contains(otherTile)) ||
                                                                (!real && !hypoTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe move to
                        {
                            if (real)
                            {
                                otherTile.targetedBy.Add(this);
                                targets.Add(otherTile);
                            }
                            else
                            {
                                otherTile.hypoTargetedBy.Add(this);
                                hypoTargets.Add(otherTile);
                            }
                        }
                    }
                }
            }
        }
    }

    //depth first search of tiles that will only recurse in one direction at a time, blocked by unavailable tiles
    private void planPathsInALine(bool real)
    {
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
                    if (activeTile.distance < moveRange &&
                        otherTile.distance > activeTile.distance + 1 &&
                            ((real && (activeTile == thisTile || activeTile.thisPiece == null)) ||
                            (!real && (activeTile == hypoTile || activeTile.hypoPiece == null))))
                    {
                        continueSearch = true;
                        otherTile.distance = activeTile.distance + 1;
                        if (otherTile.distance <= moveRange && ((real && !targets.Contains(otherTile)) ||
                                                               (!real && !hypoTargets.Contains(otherTile)))) // here, otherTile is a target we can maybe move to
                        {
                            if (real)
                            {
                                otherTile.targetedBy.Add(this);
                                targets.Add(otherTile);
                            }
                            else
                            {
                                otherTile.hypoTargetedBy.Add(this);
                                hypoTargets.Add(otherTile);
                            }
                        }
                        activeTile = otherTile;
                    }
                }
            }
            continueSearch = true;
        }
    }

    //breadth first search of tiles not recursing on unavailable tiles
    private void findStepPath(tile targetTile)
    {
        for (int i = 0; i < bm.allTiles.Length; i++)
        {
            bm.allTiles[i].previousTile = null;
            bm.allTiles[i].distance = 1000;
        }
        Queue q = new Queue();
        tile activeTile = oldTile;
        tile otherTile;
        q.Enqueue(activeTile);
        activeTile.distance = 0;
        while (q.Count > 0)
        {
            activeTile = (tile)q.Dequeue();
            if (activeTile == targetTile)  // here, we have found the path to our target
            {
                while (activeTile.previousTile != null)
                {
                    stepPath.Insert(0,activeTile);
                    activeTile = activeTile.previousTile;
                }
                return;
            }
            for (int i = 0; i < activeTile.neighbors.Length; i++)
            {
                if (activeTile.neighbors[i] != null)
                {
                    otherTile = activeTile.neighbors[i];
                    if (activeTile.distance < moveRange &&
                        otherTile.distance > activeTile.distance + 1 &&
                        (activeTile == oldTile || activeTile.thisPiece == null))
                    {
                        q.Enqueue(otherTile);
                        otherTile.distance = activeTile.distance + 1;
                        otherTile.previousTile = activeTile;
                    }
                }
            }
        }
    }

    //handles move and ability targets, real and hypo
    public void clearTargets(bool real)
    {
        if (real)
        {
            while (targets.Count > 0)
            {
                targets[0].targetedBy.Remove(this);
                targets.RemoveAt(0);
            }
            while (hasActivatedAbility && abilityTargets.Count > 0)
            {
                abilityTargets[0].abilityTargetedBy.Remove(this);
                abilityTargets.RemoveAt(0);
            }
        }
        else
        {
            while (hypoTargets.Count > 0)
            {
                hypoTargets[0].hypoTargetedBy.Remove(this);
                hypoTargets.RemoveAt(0);
            }
            while (hasActivatedAbility && hypoAbilityTargets.Count > 0)
            {
                hypoAbilityTargets[0].abilityHypoTargetedBy.Remove(this);
                hypoAbilityTargets.RemoveAt(0);
            }
        }
    }

    public tile realOrHypoTile(bool real)
    {
        if (real)
        {
            return thisTile;
        }
        else
        {
            return hypoTile;
        }
    }

    public int realOrHypoDamage(bool real)
    {
        if (real)
        {
            return damage;
        }
        else
        {
            return hypoDamage;
        }
    }
}
