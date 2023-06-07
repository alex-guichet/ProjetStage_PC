using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class AutoAimShotInstantiation : InstantiationEffect
{
    public float dispersionAngle;
    
    public AutoAimShotInstantiation(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }
    
    
    public override void InitializeVariables()
    {
    }

    public override void Execute(Vector3 position = default, Transform transform = null )
    {
        if (modularWeapon.hasStoppedShooting)
            return;

        UpdateAmmoLoad(false);
        RaycastHit[] hit_infos = Physics.CapsuleCastAll( position - Vector3.down * Constants.autoAimBottomDistance, position - Vector3.up * Constants.autoAimTopDistance, Constants.autoAimRadius, modularWeapon.currentDirection, 150.0f, LayerMask.GetMask( "Enemy" ) ) ?? throw new ArgumentNullException( "Physics.CapsuleCastAll( position - Vector3.down * bottomDistance, position - Vector3.up * topDistance, shootingAngle, currentDirection, 150.0f, LayerMask.GetMask( \"Enemy\" ) )" );

        foreach( RaycastHit hit_info in hit_infos )
        {
            var enemy = hit_info.transform.GetComponent<EnemyBase>();

            if( !enemy )
            {
                continue;
            }

            modularWeapon.currentDirection = ( enemy.transform.position + Vector3.up * 0.75f - modularWeapon.transform.position ).normalized;
            break;
        }

        Quaternion bullet_rotation = (modularWeapon.currentDirection != Vector3.zero) ? Quaternion.LookRotation(modularWeapon.currentDirection) : modularWeapon.transform.rotation;
        float dispersion_angle = (float)modularWeapon.dispersionAngle.GetValue();
        
        GameObject bullet = Object.Instantiate( modularWeapon.bulletPrefab, modularWeapon.bulletSpawnPoint.position, bullet_rotation );
        bullet.transform.Rotate(0f,Random.Range(-dispersion_angle, dispersion_angle),0f);
        PlayerModularAmmo playerModularAmmo = bullet.GetComponent<PlayerModularAmmo>();
        playerModularAmmo.modularWeapon = modularWeapon;

        modularWeapon.currentlyUsedShootVFX = ( modularWeapon.currentlyUsedShootVFX + 1 ) % modularWeapon.shootVFX.Length;
        modularWeapon.shootVFX[ modularWeapon.currentlyUsedShootVFX ].Play();
    }

}
