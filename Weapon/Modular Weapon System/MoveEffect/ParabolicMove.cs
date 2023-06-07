using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParabolicMove : MoveEffect
{
    public float moveSpeed;
    private float _animationTime = 5f;
    
    public ParabolicMove(ModularWeapon modular_weapon) : base(modular_weapon)
    {
    }
    

    public override void InitializeVariables()
    {
    }

    public override Vector3 Move(PlayerModularAmmo modular_ammo)
    {
        float animation_time = _animationTime * modular_ammo.speedMultiplicator;
        float animation_time_clamped = Mathf.Clamp(animation_time, 1f, animation_time);
        
        if (modular_ammo.animationTimer <= animation_time_clamped)
        {
            modular_ammo.animationTimer += Time.deltaTime * moveSpeed;
        }
        return MathHelper.Parabola(modular_ammo.startPosition, modular_ammo.endPosition, 5f, modular_ammo.animationTimer /animation_time_clamped) - modular_ammo.transform.position;
    }
}