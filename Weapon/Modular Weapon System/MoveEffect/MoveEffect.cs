using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveEffectType
{
    Straight,
    Guided,
    Parabolic
}

public class MoveEffect : WeaponEffect
{
    public MoveEffectType MoveEffectType;
    public MoveEffect(ModularWeapon modular_weapon) : base(modular_weapon)
    {
        WeaponEffectType = WeaponEffectType.Move;
    }
    
    public virtual void RotateWeapon(ModularWeapon modularWeapon)
    {
    }

}
