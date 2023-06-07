using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public enum ExecutionEffectType
{
    Single,
    FireRate,
    AmmoCharge,
    ChainRotation,
    TimeCharge,
    Target
}

public class ExecutionEffect : WeaponEffect
{
    public List<WeaponEffect> InstantiationEffectList;
    public List<WeaponEffect> AddedEffectList;
    public bool IsExecutionStopped;
    
    protected event onShootHandler onStartShooting;
    protected event onShootHandler onEndShooting;
    protected delegate void onShootHandler();

    public ExecutionEffect(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList, List<WeaponEffect> addedEffectList) : base(modular_weapon)
    {
        InstantiationEffectList = instantiationEffectList;
        AddedEffectList = addedEffectList;
    }
    
    
    public override void InitializeVariables()
    {
        onStartShooting += () => modularWeapon.parentPlayerController.shootingSpeedModifier =  (float)modularWeapon.roverShootingSpeedModifier.GetValue();
        onEndShooting += () => modularWeapon.parentPlayerController.shootingSpeedModifier = 1f;
    }
    
    public virtual void OnPress()
    {
        
    }
    
    public virtual void OnRelease()
    {
        
    }
    
    public virtual void OnUpdate()
    {
        
    }

    protected void OnStartShooting()
    {
        onStartShooting?.Invoke();
    }

    protected void OnEndShooting()
    {
        onEndShooting?.Invoke();
    }
}

[System.Serializable]
public struct ExecutionBehaviorEffect
{
    public ExecutionEffectType executionEffectType;
    [HideInInspector] public HitEffectType[] hitEffectType;
    [HideInInspector] public MoveEffectType[] moveEffectType;
    [HideInInspector] public bool onStart;
    [HideInInspector] public bool onEnd;

}
