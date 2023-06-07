using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public class TargetExecution : ExecutionEffect
{
    public GameObject landingTargetPrefab;
    public float range;
    public float targetSpeed;
    public float targetMinimumDistance;
        
    private GameObject _target;
    private float _moveTime;
    private float _moveTimer;
    private ParabolicMove _parabolicMoveEffect;
    
    
    public TargetExecution(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList,
        List<WeaponEffect> addedEffectList) : base(modular_weapon, instantiationEffectList, addedEffectList)
    {
    }

    public override void InitializeVariables()
    {
        base.InitializeVariables();
        _moveTime = range / targetSpeed;
    }

    public override void OnPress()
    {
        modularWeapon.isHoldingShoot = true;
        OnStartShooting();
        _target = Object.Instantiate(modularWeapon.landingTargetPrefab, modularWeapon.transform.position + modularWeapon.transform.forward * targetMinimumDistance, modularWeapon.transform.rotation, modularWeapon.transform);
    }

    public override void OnRelease()
    {
        modularWeapon.targetRange = Vector3.Distance(modularWeapon.transform.position, _target.transform.position);
        Object.Destroy(_target);
        OnEndShooting();
        foreach (var instantiation_effect in InstantiationEffectList)
        {
            instantiation_effect.Execute();
        }
        modularWeapon.isHoldingShoot = false;
        _moveTimer = 0f;
    }

    public override void OnUpdate()
    {
        if (modularWeapon.hasStoppedShooting)
            return;
        
        if (_target)
        {
            if (_moveTimer < _moveTime)
            {
                _moveTimer += Time.deltaTime;
                _target.transform.position += _target.transform.forward * (Time.deltaTime * targetSpeed);
            }
        }
    }
}
