using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AmmoChargeExecution : ExecutionEffect
{
    public float chargeTime;
    public float clipSize;
    public float fireRate;
    public float dispersionAngle;
    public float minimumTimeBeforeShot;
    public bool canCancelCharge;
    public GameObject chargingBar;

    private float _fireRateTime;
    private float _timerBeforeShot;
    private float _chargeTimer;
    private float _fireRateTimer;
    private float _initFireRateTime;
    private float _currentDispersionAngle;
    private int _currentAmmoAmount;
    
    private bool _isMaxCharged;
    private bool _isStartCharged;
    
    public AmmoChargeExecution(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList, List<WeaponEffect> addedEffectList) : base(modular_weapon, instantiationEffectList, addedEffectList)
    {
    }
    

    public override void InitializeVariables()
    {
        base.InitializeVariables();
        _fireRateTime = 1.0f / fireRate;
        _initFireRateTime = _fireRateTime;
        modularWeapon.chargingBar.gameObject.SetActive(true);
    }
    
    public override void OnPress()
    {
        modularWeapon.isHoldingShoot = true;
        
        if (modularWeapon.canCancelCharge)
        {
            _isStartCharged = false;
            _timerBeforeShot = 0f;
            _chargeTimer = 0f;
            _isMaxCharged = false;
            _currentAmmoAmount = 0;
        }
    }
    
    public override void OnRelease()
    {
        modularWeapon.isHoldingShoot = false;
    }

    public override void OnUpdate()
    {
        if (IsExecutionStopped)
            return;

        if (modularWeapon.hasStoppedShooting)
            return;

        _fireRateTime = 1.0f / (float)modularWeapon.fireRate.GetValue();
        _fireRateTimer += Time.deltaTime;

        if (_isStartCharged)
        {
            float charge_value = _currentAmmoAmount / clipSize;
            modularWeapon.gaugeFillAmount = charge_value;
            modularWeapon.chargingBar.UpdateFillAmount(charge_value, 1);
        }
        
        if (modularWeapon.isHoldingShoot)
        {
            if (_timerBeforeShot < minimumTimeBeforeShot)
            {
                if (_isStartCharged)
                {
                    _isStartCharged = false;
                }
                float charge_value = _timerBeforeShot / minimumTimeBeforeShot;
                modularWeapon.gaugeFillAmount = charge_value;
                modularWeapon.chargingBar.UpdateFillAmount(charge_value, 0);
                _timerBeforeShot += Time.deltaTime;
                return;
            }
            
            if (!_isStartCharged)
            {
                _isStartCharged = true;
            }
            
            _chargeTimer += Time.deltaTime;
            if (_chargeTimer <= chargeTime)
            {
                _currentAmmoAmount = Mathf.RoundToInt(clipSize * (_chargeTimer / chargeTime));
            }
            else
            {
                if (!_isMaxCharged)
                {
                    if (modularWeapon.instantiationEffectList.FindIndex(x => x == InstantiationEffectType.ZigZag) != -1)
                    {
                        _fireRateTime /= 2f;
                    }
                    else
                    {
                        _currentDispersionAngle = dispersionAngle * 0.5f;
                        modularWeapon.dispersionAngle.SetValue(_currentDispersionAngle);
                    }
                    _isMaxCharged = true;
                }
            }
            return;
        }

        if (_timerBeforeShot > 0f && !_isStartCharged && modularWeapon.canCancelCharge)
        {
            float charge_value = _timerBeforeShot / minimumTimeBeforeShot;
            modularWeapon.gaugeFillAmount = charge_value;
            modularWeapon.chargingBar.UpdateFillAmount(charge_value, 0);
            _timerBeforeShot -= Time.deltaTime;
        }

        if (_currentAmmoAmount <= 0f)
        {
            if (modularWeapon.instantiationEffectList.FindIndex(x => x == InstantiationEffectType.ZigZag) != -1)
            {
                _fireRateTime = _initFireRateTime;
            }
            else
            {
                if (Math.Abs(_currentDispersionAngle - dispersionAngle) > 0)
                {
                    modularWeapon.dispersionAngle.SetValue(dispersionAngle);
                }
            }

            if (modularWeapon.isShooting)
            {
                modularWeapon.isShooting = false;
                OnEndShooting();
            }

            if (!modularWeapon.canCancelCharge)
            {
                _timerBeforeShot = 0f;
            }
            _chargeTimer = 0f;
            _isMaxCharged = false;
            return;
        }
        
        _chargeTimer = chargeTime * (_currentAmmoAmount/clipSize);

        if (_fireRateTimer >= _fireRateTime)
        {
            if (!modularWeapon.isShooting)
            {
                modularWeapon.isShooting = true;
                OnStartShooting();
            }
            _fireRateTimer = 0f;
            foreach (var instantiation_effect in InstantiationEffectList)
            {
                instantiation_effect.Execute();
                _currentAmmoAmount--;
            }
        }
    }
}
