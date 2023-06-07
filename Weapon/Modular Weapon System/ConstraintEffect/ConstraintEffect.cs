using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ContraintEffectType
{
    RestrictiveOverheat,
    PermissiveOverheat,
    Reload
}

public class ConstraintEffect : WeaponEffect
{
    
    public ConstraintEffect(ModularWeapon modular_weapon) : base(modular_weapon)
    {
        WeaponEffectType = WeaponEffectType.Constraint;
    }
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        
    }
}
