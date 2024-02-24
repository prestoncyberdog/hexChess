using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class projectile : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public piece target;
    public int damage;
    public piece source;
    public bool applyPush;
    public int pushDirection;

    public float moveSpeed;
    public float targetAngle;

    public void init()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;
        
        transform.position = source.transform.position;
        transform.localScale = new Vector3(.3f, .3f, 1);
        this.GetComponent<SpriteRenderer>().color =  new Color(0f, 0f, 0f);
        setTargetAngle();
        transform.Rotate(new Vector3(0, 0, targetAngle));

        moveSpeed = gm.projectileMoveRate;
    }

    void Update()
    {
        Vector3 toTarget = target.transform.position - transform.position;
        float distToMove = moveSpeed * Time.deltaTime;
        if (toTarget.magnitude <= distToMove)
        {
            arrive();
            return;
        }
         transform.position = transform.position + toTarget.normalized * distToMove;
    }

    public void arrive()
    {
        if (applyPush && !target.willGetKilled(damage, true))
        {
            tile targetTile = target.thisTile;
            targetTile.pushTile(pushDirection, true);
            if (targetTile.thisPushedPiece != null)
            {
                if (source.pushedPieces == null)
                {
                    source.pushedPieces = new List<pushedPiece>();
                }
                source.pushedPieces.Add(targetTile.thisPushedPiece);
                targetTile.thisPushedPiece = null;
                source.updateTargeting(true);
            }
        }
        target.takeDamage(damage, true);
        deleteProjectile();
    }

    public void setTargetAngle()
    {
        Vector2 toTarget = (target.transform.position - transform.position).normalized;
        targetAngle = Mathf.Atan(toTarget.y / toTarget.x);
        if (toTarget.x < 0)
        {
            targetAngle += Mathf.PI;
        }
        targetAngle = Mathf.Rad2Deg * targetAngle - 90;
    }

    public void deleteProjectile()//manages global count
    {
        bm.movingPieces--;
        Destroy(gameObject);
    }
}
