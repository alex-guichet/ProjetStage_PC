using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidedMove : MoveEffect
{
    public float moveSpeed;
    public LayerMask enemyMask;
    public float range;

    public GuidedMove(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }

    public override void InitializeVariables()
    {
        enemyMask = modularWeapon.enemyMask;
    }
    
    public override Vector3 Move(PlayerModularAmmo modularAmmo)
    {
        EnemyBase target;
        Vector3 target_direction;
        Vector3 position = new();

        Collider[] nearby_enemies = Physics.OverlapBox(modularAmmo.transform.position + modularAmmo.transform.forward * Constants.DetectionGuidedAmmoForwardOffset, Vector3.one * Constants.DetectionGuidedAmmoBoxExtent, modularAmmo.transform.rotation, enemyMask);
        EnemyBase closest_enemy = null;
        float closest_distance = 100f;
        
        if (nearby_enemies.Length > 0)
        {
            foreach( Collider t in nearby_enemies )
            {
                if( !( Vector3.Distance( modularAmmo.transform.position, t.transform.position ) < closest_distance ) )
                    continue;

                closest_distance = Vector3.Distance(modularAmmo.transform.position, t.transform.position);
                closest_enemy = t.GetComponent<EnemyBase>();
            }

            target = closest_enemy;
        }
        else
        {
            target = null;
        }
        
        if(target)
        {
            target_direction = target.transform.position + (Vector3.up * 1) - modularAmmo.transform.position;
            target_direction.Normalize();
            modularAmmo.direction += target_direction * (moveSpeed * Time.deltaTime);
            modularAmmo.direction.Normalize();
        }
        else
        {
            modularAmmo.direction = modularAmmo.transform.forward;
        }
        
        position = modularAmmo.direction * (modularAmmo.speedOverTime.Evaluate(modularAmmo.liveTimer) * Time.deltaTime);
        modularAmmo.transform.forward = modularAmmo.direction;
        
        return position;
    }

}
