using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReloadConstraint : ConstraintEffect
{
    public float clipSize;
    public float reloadTime;
    
    public ReloadConstraint(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }
    
    
    public override void InitializeVariables()
    {
        modularWeapon.currentLoad = (float)modularWeapon.clipSize.GetValue();
    }
    
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        if (modularWeapon.currentLoad <= 0)
        {
            if (!modularWeapon.isReloading)
            {
                modularWeapon.isReloading = true;
                modularWeapon.hasStoppedShooting = true;
            }
        }

        if (modularWeapon.isReloading)
        {
            modularWeapon.reloadTimer += Time.deltaTime;

            if (modularWeapon.reloadTimer > reloadTime)
            {
                if (modularWeapon.isReloading)
                {
                    modularWeapon.hasStoppedShooting = false;
                    modularWeapon.isReloading = false;
                    modularWeapon.currentLoad = clipSize;
                }
            }
        }
        else
        {
            modularWeapon.reloadTimer = 0;
        }
    }
}
