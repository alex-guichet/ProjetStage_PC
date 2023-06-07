using UnityEngine;

public class PlayerModularSword : PlayerModularAmmo
{
    public TrailRenderer trailRenderer;
    
    protected override void Start()
    {
        float range = (float)modularWeapon.range.GetValue();
        var blade_transform = transform;
        
        trailRenderer.widthMultiplier = range - 1.5f;
        Vector3 scale = blade_transform.localScale;
        blade_transform.localScale = new Vector3(scale.x, scale.y, range);
        TimeToLive = (float)modularWeapon.minimumTimeBetweenShot.GetValue();
    }

    protected override void FixedUpdate()
    {
    }
    
    protected override void OnTriggerEnter( Collider other )
    {
        var enemy = other.GetComponent<EnemyBase>();
        if(enemy)
        {
            foreach( HitEffect hit_effect in modularWeapon.weaponEffectsDictionary[ WeaponEffectType.Hit ] )
            {
                hit_effect.Execute(transform.position, other.transform );
            }

            if (!enemy.gameObject.activeInHierarchy)
            {
                modularWeapon.enemyKillCount++;
            }
            ImpactFeedbacks?.PlayFeedbacks();
        }
    }
}
