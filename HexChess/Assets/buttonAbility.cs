using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buttonAbility : button
{
    public piece owner;

    public Vector3 posOffset;

    public override void specificInit()
    {
        fontSize = 12;
        posOffset = new Vector3(0, -1f * gm.bm.generator.tileScale, 0);
    }

    public override void specificUpdate()
    {
        if (gm.bm.selectedPiece == null || gm.bm.selectedPiece != owner || !owner.canUseAbility() || owner.activatingAbility)
        {
            if (owner.thisHealthBar != null)
            {
                owner.thisHealthBar.updateText();
            }
            deactivate();
        }
        transform.position = owner.transform.position + posOffset;
    }

    public override void doSomething()
    {
        owner.startUsingAbility();
    }

    public override void setText()
    {
        labelText.text = "Use Ability";
    }
}

