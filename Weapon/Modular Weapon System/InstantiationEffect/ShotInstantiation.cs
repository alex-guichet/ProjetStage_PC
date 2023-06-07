using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class ShotInstantiation : InstantiationEffect
{
    public float dispersionAngle;
    
    public ShotInstantiation(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
    }

    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        if (modularWeapon.hasStoppedShooting)
            return;
        
        float dispersion_angle = (float)modularWeapon.dispersionAngle.GetValue();
        UpdateAmmoLoad(false);
        GameObject bullet = Object.Instantiate( modularWeapon.bulletPrefab, modularWeapon.bulletSpawnPoint.position, modularWeapon.transform.rotation );
        bullet.transform.Rotate(0f,Random.Range(-dispersion_angle, dispersion_angle),0f);
        PlayerModularAmmo playerModularAmmo = bullet.GetComponent<PlayerModularAmmo>();
        playerModularAmmo.modularWeapon = modularWeapon;
    }
}