using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitEffectType
{
    Normal,
    Explosion
}

public class HitEffect : WeaponEffect
{
    public HitEffect(ModularWeapon modular_weapon) : base(modular_weapon)
    {
        WeaponEffectType = WeaponEffectType.Hit;
    }
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        
    }
}
