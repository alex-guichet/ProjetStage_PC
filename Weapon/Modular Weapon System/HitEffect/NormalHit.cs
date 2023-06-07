using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalHit : HitEffect
{
    public float damage;
    
    public NormalHit(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
    }
    
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        var enemy_base = transform.GetComponent<EnemyBase>();
        if (enemy_base)
        {
            enemy_base.ReceiveDamage((float)modularWeapon.damage.GetValue(), modularWeapon.parentPlayerController);
        }
    }
}
