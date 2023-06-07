using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InstantiationEffectType
{
    Shoot,
    AutoAimShoot,
    Plant,
    FixedToWeapon,
    ZigZag
}

public class InstantiationEffect : WeaponEffect
{
    public InstantiationEffect(ModularWeapon modular_weapon) : base(modular_weapon)
    {
        WeaponEffectType = WeaponEffectType.Instantiation;
    }
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        
    }

    public void UpdateAmmoLoad(bool increment)
    {
        if (increment)
        {
            modularWeapon.currentLoad++;
        }
        else
        {
            modularWeapon.currentLoad--;
        }
    }
}
