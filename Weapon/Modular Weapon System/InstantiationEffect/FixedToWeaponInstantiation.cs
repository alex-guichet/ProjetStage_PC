using UnityEngine;
using Object = UnityEngine.Object;

public class FixedToWeaponInstantiation : InstantiationEffect
{
    public FixedToWeaponInstantiation(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }
    
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        if (modularWeapon.hasStoppedShooting)
            return;
        
        UpdateAmmoLoad(false);
        GameObject bullet = Object.Instantiate( modularWeapon.bulletPrefab, modularWeapon.bulletSpawnPoint.position, Quaternion.identity );
        bullet.transform.SetParent(modularWeapon.bulletSpawnPoint, true);
        bullet.transform.localPosition = Vector3.zero;
        bullet.transform.localScale = Vector3.one;
        bullet.transform.localRotation = Quaternion.identity;
        PlayerModularAmmo playerModularAmmo = bullet.GetComponent<PlayerModularAmmo>();
        modularWeapon.currentSetupAmmo.Add(playerModularAmmo);
        playerModularAmmo.modularWeapon = modularWeapon;
    }
}