using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public class TimeChargeExecution : ExecutionEffect
{
    public float chargeTime;
    public float shootingTime;
    public float range;
    public float laserWidth;
    public float roverRotationMultiplicator;
    public GameObject chargingBar;
    
    private float _chargeTimer;
    private float _shootingTimer;
    private float _chargeValue;
    private float _initRoverRotationSpeed;

    private bool _isShooting;
    
    public TimeChargeExecution(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList, List<WeaponEffect> addedEffectList) : base(modular_weapon, instantiationEffectList, addedEffectList)
    {
    }

    public override void InitializeVariables()
    {
        base.InitializeVariables();
        modularWeapon.chargingBar.gameObject.SetActive(true);
        if (modularWeapon.parentPlayerController)
        {
            _initRoverRotationSpeed = modularWeapon.parentPlayerController.bodyRotationSpeed;
        }
    }
    
    public override void OnPress()
    {
        modularWeapon.isHoldingShoot = true;
        if (_isShooting)
        {
            OnEndShooting();
            foreach (var ammo in modularWeapon.currentSetupAmmo)
            {
                Object.Destroy(ammo.gameObject);
            }
            modularWeapon.currentSetupAmmo.Clear();
            modularWeapon.parentPlayerController.bodyRotationSpeed = _initRoverRotationSpeed;
            _chargeTimer = 0f;
        }
    }
    
    public override void OnRelease()
    {
        modularWeapon.isHoldingShoot = false;
        if (_chargeTimer >= chargeTime)
        {
            OnStartShooting();
            foreach (var instantiation_effect in InstantiationEffectList)
            {
                instantiation_effect.Execute();
            }
            _isShooting = true;
            modularWeapon.parentPlayerController.bodyRotationSpeed *= roverRotationMultiplicator;
            _shootingTimer = shootingTime;
        }
    }

    public override void OnUpdate()
    {
        if (IsExecutionStopped)
            return;
        
        if (modularWeapon.hasStoppedShooting)
            return;
        
        modularWeapon.gaugeFillAmount = _chargeValue;
        modularWeapon.chargingBar.UpdateFillAmount(_chargeValue, 1);
        
        if (modularWeapon.isHoldingShoot)
        {
            if (_chargeTimer < chargeTime)
            {
                _chargeTimer += Time.deltaTime;
            }
            _chargeValue = _chargeTimer / chargeTime;
            return;
        }

        if (!_isShooting)
        {
            if (_chargeTimer >= 0f)
            {
                _chargeValue = _chargeTimer / chargeTime;
                _chargeTimer -= Time.deltaTime;
            }
        }
        else
        {
            if (_shootingTimer >= 0f)
            {
                _chargeValue = _shootingTimer / shootingTime;
                _shootingTimer -= Time.deltaTime;
            }
            else
            {
                modularWeapon.parentPlayerController.bodyRotationSpeed = _initRoverRotationSpeed;
                _isShooting = false;
                _chargeTimer = 0f;
            }
        }
    }
}
