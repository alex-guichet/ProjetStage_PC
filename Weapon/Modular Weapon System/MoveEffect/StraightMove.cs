using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightMove : MoveEffect
{
    public float moveSpeed;
    public float range;

    public StraightMove(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
    }
    
    public override Vector3 Move(PlayerModularAmmo modularAmmo)
    {
        return modularAmmo.transform.forward * ( (float)modularWeapon.moveSpeed.GetValue() * Time.deltaTime );
    }

}
