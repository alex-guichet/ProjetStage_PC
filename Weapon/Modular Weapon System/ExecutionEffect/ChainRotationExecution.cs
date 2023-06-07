using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class ChainRotationExecution : ExecutionEffect
{
    public float range;
    public float minimumKillCount;
    public float maximumKillCount;
    public float minimumTimeBetweenShot;
    public float maximumTimeBetweenShot;
    public float attackAngle;
    public float dashDuration;
    public float dashDistance;
    public float fireRate;
    public float minimumAttackCount;
    
    private float _timeBetweenShot;
    private float _timerBetweenShot;
    private float _timerBetweenShotOnHold;
    private float _rotationSpeed;
    private float _initRangeValue;
    private float _initDamageValue;
    private float _initMinimumTimeShot;
    private float _fireRateTime;
    private float _fireRateTimer;
    private float _currentAttackCount;
    private float _dashSpeed;
    
    private bool _startTimerBetweenShot;
    private bool _isShooting;
    private bool _isSpinning;
    private bool _isUsingSword;
    private bool _isShotBuffered;
    
    public ChainRotationExecution(ModularWeapon modular_weapon, List<WeaponEffect> instantiationEffectList, List<WeaponEffect> addedEffectList) : base(modular_weapon, instantiationEffectList, addedEffectList)
    {
    }
    
    public override void InitializeVariables()
    {
        base.InitializeVariables();
        _dashSpeed = dashDistance / dashDuration;
        _initMinimumTimeShot = minimumTimeBetweenShot;
        modularWeapon.isRotating = false;
        _rotationSpeed = (attackAngle * 2f / minimumTimeBetweenShot);
        modularWeapon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        
        _isUsingSword = modularWeapon.instantiationEffectList.FindIndex(x => x == InstantiationEffectType.FixedToWeapon) != -1;
        if (!_isUsingSword)
        {
            _fireRateTime = minimumTimeBetweenShot / fireRate;
        }
    }
    
    public override void OnPress()
    {
        modularWeapon.isHoldingShoot = true;
        if (!_isShooting)
        {
            if (!_startTimerBetweenShot)
            {
                Shoot();
            }

            if (_timerBetweenShot > minimumTimeBetweenShot && _timerBetweenShotOnHold == 0f)
            {
                Shoot();
            }
        }
        else
        {
            _isShotBuffered = true;
        }
    }
    
    public override void OnRelease()
    {
        modularWeapon.isHoldingShoot = false;
        _timerBetweenShotOnHold = 0f;
    }
    
    public override void OnUpdate()
    {
        switch (_isShooting)
        {
            case true when _timerBetweenShot <= minimumTimeBetweenShot && _startTimerBetweenShot:
                modularWeapon.transform.parent.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
                if (!_isUsingSword)
                {
                    _fireRateTimer += Time.deltaTime;
                    if (_fireRateTimer > _fireRateTime)
                    {
                        _fireRateTimer = 0f;
                        InstantiateAmmo();
                    }
                }
                break;
            case true:
                _isShooting = false;
                if (_isSpinning)
                {
                    UpdateWeapon(_initRangeValue, _initDamageValue , _initMinimumTimeShot);
                    _rotationSpeed = (attackAngle * 2f / minimumTimeBetweenShot);
                    modularWeapon.enemyKillCount = 0;
                    _isSpinning = false;
                }
                if (_isShotBuffered)
                {
                    Shoot();
                    _isShotBuffered = false;
                }
                break;
        }
        
        if (modularWeapon.isHoldingShoot && !_isSpinning)
        {
            _timerBetweenShotOnHold += Time.deltaTime;
            if (_timerBetweenShotOnHold > maximumTimeBetweenShot)
            {
                Shoot();
                return;
            }
        }
        
        if (!_startTimerBetweenShot)
            return;

        _timerBetweenShot += Time.deltaTime;

        if (_timerBetweenShot > maximumTimeBetweenShot && !_isSpinning)
        {
            float range_multiplier = 0f;
            float damage_multiplier = 0f;
            int kill_count = modularWeapon.enemyKillCount;

            if (kill_count < minimumKillCount)
            {
                if (_currentAttackCount < minimumAttackCount)
                {
                    _isShooting = false;
                    _startTimerBetweenShot = false;
                    modularWeapon.enemyKillCount = 0;
                    _currentAttackCount = 0f;
                    return;
                }
                range_multiplier = 1f;
                damage_multiplier = 1f;
            }

            _isSpinning = true;
            
            if (kill_count >= minimumKillCount && kill_count < maximumKillCount)
            {
                range_multiplier = (modularWeapon.enemyKillCount - minimumKillCount) * ((Constants._spinRangeMultiplier - 1f) / (maximumKillCount-minimumKillCount)) + 1f;
                damage_multiplier = (modularWeapon.enemyKillCount - minimumKillCount) * ((Constants._spinDamageMultiplier - 1f) / (maximumKillCount-minimumKillCount)) + 1f;
            }

            if (kill_count >= maximumKillCount)
            {
                range_multiplier = Constants._spinRangeMultiplier;
                damage_multiplier = Constants._spinDamageMultiplier;
            }
            
            UpdateWeapon(_initRangeValue * range_multiplier, _initDamageValue * damage_multiplier, _initMinimumTimeShot * 6f);
            _rotationSpeed = (480f / minimumTimeBetweenShot);
            _rotationSpeed = (attackAngle < 0) ? _rotationSpeed : -_rotationSpeed;
            _currentAttackCount = 0f;
            Shoot();
        }
    }
    
    private void Shoot()
    {
        var parent = modularWeapon.transform.parent;
        if (!_isSpinning)
        {
            modularWeapon.parentPlayerController.ForwardDash(-modularWeapon.transform.parent.transform.parent.right.normalized, dashDuration, _dashSpeed);
        }
        Quaternion rotation = parent.localRotation;
        parent.localRotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, attackAngle);
        _rotationSpeed = -_rotationSpeed;
        attackAngle = -attackAngle;
        _isShooting = true;
        _timerBetweenShot = 0f;
        _timerBetweenShotOnHold = 0f;
        _startTimerBetweenShot = true;
        _currentAttackCount++;
        InstantiateAmmo();
    }

    private void InstantiateAmmo()
    {
        foreach (var instantiation_effect in InstantiationEffectList)
        {
            instantiation_effect.Execute();
        }
    }

    private void UpdateWeapon(float range, float damage, float minimum_shot_time)
    {
        modularWeapon.minimumTimeBetweenShot.SetValue(minimum_shot_time);
        modularWeapon.range.SetValue(range);
        modularWeapon.damage.SetValue(damage);
        minimumTimeBetweenShot = minimum_shot_time;
        _fireRateTime = minimumTimeBetweenShot / fireRate;
    }
}
