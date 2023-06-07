using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RestrictiveOverheatConstraint : ConstraintEffect
{
    public float coolingDelayTime;
    public float overheatTime;
    
    private float _coolingDelayTimer;
    private float _overheatTimer;
    private bool _isOverheating;
    private bool _isUsingFireRate;

    public RestrictiveOverheatConstraint(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
    }
    
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        modularWeapon.gaugeFillAmount = _overheatTimer / overheatTime;
        
        if (modularWeapon.isShooting && !_isOverheating)
        {
            _overheatTimer += Time.deltaTime;
            _coolingDelayTimer = 0f;

            if (_overheatTimer > overheatTime)
            {
                _isOverheating = true;
                modularWeapon.hasStoppedShooting = true;
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
            if (_overheatTimer <= 0f)
            {
                _isOverheating = false;
                modularWeapon.hasStoppedShooting = false;
            }
        }
    }
}
