using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class championMarker : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public piece champ;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        float scale = bm.generator.tileScale;
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1,1,0);
        transform.localScale = new Vector3(scale, scale, 1);
        transform.Rotate(new Vector3(0, 0, 30));
        transform.position = gm.AWAY;
    }

    void Update()
    {
        if (champ != null && champ.alive)
        {
            transform.position = champ.thisTile.transform.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
