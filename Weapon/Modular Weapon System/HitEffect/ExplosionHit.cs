using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionHit : HitEffect
{
    public float explosionRadius;
    public float damage;
    
    public ExplosionHit(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
    }
    
    public override void Execute(Vector3 position = default, Transform transform = null)
    {
        Collider[] touched_enemies = Physics.OverlapSphere(position, explosionRadius, modularWeapon.enemyMask);
        
        //weapon.ExplosionFeedbacks?.PlayFeedbacks();

        if( TerrainManager.HasInstance() )
        {
            TerrainManager.Instance.StartTerraform(explosionRadius, -0.1f, position);
        }
        
        ElementEmitter.ElementPulse( position, explosionRadius, new ElementPacket( Element.Shockwave, damage ) );
        
        if( touched_enemies.Length <= 0 )
        {
            return;
        }

        foreach( Collider t in touched_enemies )
        {
            var enemy_base = t.GetComponent<EnemyBase>();

            if( enemy_base )
            {
                enemy_base.ReceiveDamage(damage, modularWeapon.parentPlayerController);
            }
        }
    }
}
