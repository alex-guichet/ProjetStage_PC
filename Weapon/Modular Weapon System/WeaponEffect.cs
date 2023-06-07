using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public enum WeaponEffectType
{
    Hit,
    Instantiation,
    Execution,
    Move,
    Constraint
}

public class WeaponEffect
{
    public WeaponEffectType WeaponEffectType;
    public ModularWeapon modularWeapon;
    public float roverShootingSpeedModifier;
    protected bool IsInitialized;

    public WeaponEffect(ModularWeapon modular_weapon)
    {
        modularWeapon = modular_weapon;
    }
    
    public virtual void InitializeVariables()
    {
    }
    
    public virtual void Execute(Vector3 position = default, Transform transform = null)
    {
    }

    public virtual Vector3 Move(PlayerModularAmmo modularAmmo)
    {
        return default;
    }
}

