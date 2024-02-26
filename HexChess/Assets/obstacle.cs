using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class obstacle : MonoBehaviour
{
    public gameManager gm;
    public battleManager bm;

    public tile thisTile;
    public tile hypoTile;

    public float scale;
    public Color defaultColor;

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("gameManager").GetComponent<gameManager>();
        bm = gm.bm;

        scale = bm.generator.tileScale;
        transform.localScale = scale * new Vector3(1, 1, 1);
        defaultColor = new Color(0, 0, 0);

        specificInit();

        this.GetComponent<SpriteRenderer>().color = defaultColor;
    }

    public virtual void specificInit(){}
    public virtual void specificUpdate(){}

    void Update()
    {
        
    }
}
