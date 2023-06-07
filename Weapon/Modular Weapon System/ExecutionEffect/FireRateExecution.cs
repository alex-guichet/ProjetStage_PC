using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireRateExecution : ExecutionEffect
{
    public float fireRate;
    
    private float _fireRateTime;
    private float _fireRateTimer;
    
    public FireRateExecution(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList, List<WeaponEffect> addedEffectList) : base(modular_weapon, instantiationEffectList, addedEffectList)
    {
    }
    

    public override void InitializeVariables()
    {
        base.InitializeVariables();
        _fireRateTime = 1.0f / fireRate;
    }
    
    public override void OnPress()
    {
        modularWeapon.isHoldingShoot = true;
        modularWeapon.isShooting = true;
        OnStartShooting();
    }
    
    public override void OnRelease()
    {
        modularWeapon.isHoldingShoot = false;
        modularWeapon.isShooting = false;
        OnEndShooting();
    }

    public override void OnUpdate()
    {
        if (IsExecutionStopped)
            return;
        
        _fireRateTime = 1.0f / (float)modularWeapon.fireRate.GetValue();
        _fireRateTimer += Time.deltaTime;
        
        if (modularWeapon.isHoldingShoot)
        {
            if (_fireRateTimer >= _fireRateTime)
            {
                _fireRateTimer = 0f;
                foreach (var instantiation_effect in InstantiationEffectList)
                {
                    instantiation_effect.Execute();
                }
            }
        }
    }
}
