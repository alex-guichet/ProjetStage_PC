using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class ZigzagInstantiation : InstantiationEffect
{
    public float attackAngle;
    public float rotationDelta;
    
    private float _rotationAmmo;
    private float _currentAttackAngle;
    private bool _isGoingBackward;
    
    public ZigzagInstantiation(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
        _rotationAmmo = -attackAngle;
    }

    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        if (modularWeapon.hasStoppedShooting)
            return;
        
        UpdateAmmoLoad(false);
        attackAngle = (float)modularWeapon.attackAngle.GetValue();
        if (_rotationAmmo < attackAngle && !_isGoingBackward)
        {
            _rotationAmmo += rotationDelta;
        }
        else
        {
            _isGoingBackward = true;
        }
        
        if (_rotationAmmo > -attackAngle && _isGoingBackward)
        {
            _rotationAmmo -= rotationDelta;
        }
        else
        {
            _isGoingBackward = false;
        }
        
        GameObject bullet = Object.Instantiate(modularWeapon.bulletPrefab, modularWeapon.bulletSpawnPoint.position, modularWeapon.transform.rotation);
        bullet.transform.Rotate(0f, _rotationAmmo, 0f);
        PlayerModularAmmo playerModularAmmo = bullet.GetComponent<PlayerModularAmmo>();
        playerModularAmmo.modularWeapon = modularWeapon;
    }
}