using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static InstructionType;

public class PlayerModularLandmine : PlayerModularAmmo
{
    [Tooltip("Detection decal")]
    public DecalProjector detectionDecal;
    
    [Tooltip("landmine Bip Mesh Renderer")]
    public MeshRenderer bipRenderer;
    
    private float _explosionRadius;
    private float _activationTime;
    private float _activationTimer;
    
    private bool _isActivated;
    
    public override void Kill( bool dealt_damage )
    {
        if( !isAlive )
        {
            return;
        }

        isAlive = false;
        Instantiate( dealt_damage ? damageImpactVFX : noDamageImpactVFX, transform.position, Quaternion.identity );
        
        modularWeapon.currentLoad++;
        modularWeapon.currentGrabbableAmmo.Remove( this );;
        modularWeapon.currentSetupAmmo.Remove( this );
        Destroy( gameObject );
    }
    
    protected override void OnTriggerEnter(Collider other)
    {
        if (_isActivated)
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy && other.gameObject.layer == Constants.enemyLayer)
            {
                foreach (HitEffect hit_effect in modularWeapon.weaponEffectsDictionary[WeaponEffectType.Hit])
                {
                    hit_effect.Execute(other.transform.position);
                }
            
                ImpactFeedbacks?.PlayFeedbacks();
                Kill(true);
                if (modularWeapon.explodeAmmo)
                {
                    modularWeapon.playerWeaponManager.UpdateInstructionText(Place, modularWeapon.triggerType);
                    modularWeapon.explodeAmmo = false;
                }
                return;
            }
        }
        
        var player = other.GetComponent<PlayerController>();
        if (player)
        {
            if (isAlive)
            {
                modularWeapon.currentGrabbableAmmo.Add(this);
                modularWeapon.playerWeaponManager.UpdateInstructionText(Grab, modularWeapon.triggerType);
            }
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player)
        {
            modularWeapon.currentGrabbableAmmo.Remove(this);
            if (!modularWeapon.explodeAmmo)
            {
                modularWeapon.playerWeaponManager.UpdateInstructionText(Place, modularWeapon.triggerType);
            }
            else
            {
                modularWeapon.playerWeaponManager.UpdateInstructionText(Explode, modularWeapon.triggerType);
            }
        }
    }
    
    protected override void Start()
    {
        float detectionRadius = (float)modularWeapon.plantAmmoDetectionRadius.GetValue();
        detectionDecal.size = new Vector3(detectionRadius*2, detectionRadius*2, detectionDecal.size.z);
        
        var sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = detectionRadius;
        _explosionRadius = (float)modularWeapon.explosionRadius.GetValue();
        _activationTime = (float)modularWeapon.mineActivationTime.GetValue();
    }

    protected override void Update()
    {
        if (_isActivated)
            return;
        
        if (_activationTimer < _activationTime)
        {
            _activationTimer += Time.deltaTime;
        }
        else
        {
            _isActivated = true;
            Material bip_material_copy = new Material(bipRenderer.material);
            bip_material_copy.color = Color.red;
            bipRenderer.material = bip_material_copy;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 minePosition = transform.position;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(minePosition, _explosionRadius);
    }
}
