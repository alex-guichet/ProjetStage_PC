using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantInstantiation : InstantiationEffect
{
    public float plantAmmoDetectionRadius;
    public float clipSize;
    public float mineActivationTime;
    
    public PlantInstantiation(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
        modularWeapon.currentLoad = clipSize;
    }

    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        if (modularWeapon.hasStoppedShooting)
            return;

        if (modularWeapon.currentGrabbableAmmo.Count > 0)
        {
            GrabAmmo();
            return;
        }
        
        if (modularWeapon.currentLoad == 0f)
        {
            ExplodeAll();
            return;
        }
                
        Plant();

        void Plant()
        {
            if( Physics.Raycast( modularWeapon.transform.position, Vector3.down, out RaycastHit hit_info, 10.0f, 1 << Constants.groundLayer ) )
            {
                Vector3 rotation_vector = hit_info.normal - Vector3.up;
                Quaternion rotation = rotation_vector == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rotation_vector);
                GameObject ammo_object = Object.Instantiate( modularWeapon.bulletPrefab, hit_info.point, rotation);
            
                UpdateAmmoLoad(false);
            
                PlayerModularAmmo ammo = ammo_object.GetComponent<PlayerModularAmmo>();
                ammo.modularWeapon = modularWeapon;
            
                modularWeapon.currentlyUsedShootVFX = ( modularWeapon.currentlyUsedShootVFX + 1 ) % modularWeapon.shootVFX.Length;
                modularWeapon.shootVFX[ modularWeapon.currentlyUsedShootVFX ].Play();
            
                modularWeapon.currentSetupAmmo.Add(ammo);
                if (modularWeapon.currentLoad == 0)
                {
                    modularWeapon.playerWeaponManager.UpdateInstructionText(InstructionType.Explode, modularWeapon.triggerType);
                    modularWeapon.explodeAmmo = true;
                }
            }
        }

        void ExplodeAll()
        {
            var ammo_temp_list = new List<PlayerModularAmmo>(modularWeapon.currentSetupAmmo);
            foreach (var ammo in ammo_temp_list)
            {
                foreach (HitEffect hit_effect in modularWeapon.weaponEffectsDictionary[WeaponEffectType.Hit])
                {
                    hit_effect.Execute(ammo.transform.position);
                }
                ammo.Kill(true);
            }
            modularWeapon.currentSetupAmmo.Clear();
            modularWeapon.playerWeaponManager.UpdateInstructionText(InstructionType.Place, modularWeapon.triggerType);
            modularWeapon.explodeAmmo = false;
        }

        void GrabAmmo()
        {
            foreach (var mine in modularWeapon.currentGrabbableAmmo)
            {
                modularWeapon.currentSetupAmmo.Remove(mine);
                Object.Destroy(mine.gameObject);
                UpdateAmmoLoad( true);
            }

            if (modularWeapon.explodeAmmo)
            {
                modularWeapon.playerWeaponManager.UpdateInstructionText(InstructionType.Place, modularWeapon.triggerType);
                modularWeapon.explodeAmmo = false;
            }
            modularWeapon.currentGrabbableAmmo.Clear();
        }
    }

}
