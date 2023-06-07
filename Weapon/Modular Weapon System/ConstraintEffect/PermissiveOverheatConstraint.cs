using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class PermissiveOverheatConstraint : ConstraintEffect
{
    public float coolingDelayTime;
    public float overheatTime;
    public float decreaseThresholdPercentage;
    
    private float _coolingDelayTimer;
    private float _overheatTimer;
    private float _decreaseThreshold;
    private float _decreaseTimer;
    private float _decreaseTime;
    private float _fireRate;

    private bool _isOverheating;
    private bool _isUsingFireRate;

    public PermissiveOverheatConstraint(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
        coolingDelayTime = (float)modularWeapon.coolingDelayTime.GetValue();
        overheatTime = (float)modularWeapon.overheatTime.GetValue();
        decreaseThresholdPercentage = (float)modularWeapon.decreaseThresholdPercentage.GetValue();

        if (modularWeapon.weaponEffectsDictionary[WeaponEffectType.Execution]
                .FindIndex(x => x.GetType() == typeof(FireRateExecution)) != -1)
        {
            _isUsingFireRate = true;
            _fireRate = (float)modularWeapon.fireRate.GetValue();
        }
        
        _decreaseThreshold = overheatTime * (decreaseThresholdPercentage/100f);
        _decreaseTime = overheatTime - _decreaseThreshold;
    }
    
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        modularWeapon.gaugeFillAmount = _overheatTimer / overheatTime;
        
        if (modularWeapon.isShooting)
        {
            _overheatTimer += Time.deltaTime;
            
            _coolingDelayTimer = 0f;
            
            if (!_isUsingFireRate)
                return;

            if (!(_overheatTimer > _decreaseThreshold))
                return;
            
            if (_decreaseTimer <= _decreaseTime)
            {
                _decreaseTimer += Time.deltaTime;
                UpdateFireRate();
            }
            return;
        }

        if (_overheatTimer <= 0f)
            return;

        if (_coolingDelayTimer < coolingDelayTime)
        {
            _coolingDelayTimer += Time.deltaTime;
        }
        else
        {
            _overheatTimer -= Time.deltaTime;
            
            if (_decreaseTimer <= 0f)
                return;
            _decreaseTimer -= Time.deltaTime;
            UpdateFireRate();
        }

        void UpdateFireRate()
        {
            float fire_rate = _fireRate * (1f - _decreaseTimer / _decreaseTime);
            modularWeapon.fireRate.SetValue(Mathf.Clamp(fire_rate, 0.1f, fire_rate ));
        }
    }
}
