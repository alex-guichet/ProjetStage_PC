using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerWheelIK : MonoBehaviour
{
    [Header("Wheel")]
    public Transform wheel;
    public float rotationRatio;
    public float wheelHeight;
    [Header("Piston")]
    public Transform piston;
    public float pistonGroundCheckDistance;
    public Vector3 pistonRotationDirection;
    public float pistonRotationSpeed = 10.0f;
    [Header( "Wheel hinge" )]
    public Transform wheelHinge;
    [Space]
    public bool updateVisualEffect;
    public VisualEffect[] wheelEffectPerTerrainType;
    public PlayerController playerController;

    private Vector3 _pistonStartLocalPosition;
    private Quaternion _pistonStartLocalRotation;
    private Vector3 _pistonCurrentLocalPosition;
    private Vector3 _pistonTargetLocalPosition;

    private void Start()
    {
        _pistonStartLocalPosition = piston.localPosition;
        _pistonCurrentLocalPosition = _pistonStartLocalPosition;
        _pistonStartLocalRotation = piston.localRotation;
    }
    
    private void LateUpdate()
    {
        piston.localPosition = _pistonStartLocalPosition;
        Vector3 leg_world_position = piston.position;

        if( Physics.Raycast( leg_world_position, piston.up, out RaycastHit leg_hit_info, pistonGroundCheckDistance, 1 << Constants.groundLayer ) )
        {
            Debug.DrawLine( leg_hit_info.point, leg_hit_info.point - piston.up, Color.magenta );

            var direction = piston.localRotation * Vector3.up;
            
            _pistonTargetLocalPosition = piston.localPosition + direction * (Vector3.Distance(leg_world_position, leg_hit_info.point) * 0.01f); //skeletal mesh has a 100 upscale

            if( Physics.Raycast( wheel.position, Vector3.down, out RaycastHit wheel_hit_info, pistonGroundCheckDistance, 1 << Constants.groundLayer ) )
            {
                _pistonTargetLocalPosition.y += (wheel_hit_info.point.y - wheel.position.y + wheelHeight) * 0.01f;

                if( wheelHinge )
                {
                    //wheelHinge.localRotation = Quaternion.LookRotation( wheel_hit_info.normal, Vector3.forward );
                }
            }

            piston.localRotation = MathHelper.Smooth( piston.localRotation, _pistonStartLocalRotation, 1.0f );
        }
        else
        {
            piston.localRotation *= Quaternion.Euler( pistonRotationDirection * (pistonRotationSpeed * Time.deltaTime) );
        }

        _pistonCurrentLocalPosition = MathHelper.Smooth( _pistonCurrentLocalPosition, _pistonTargetLocalPosition, 10.0f );
        piston.localPosition = _pistonCurrentLocalPosition;
        
        float current_velocity = rotationRatio * playerController.currentVelocity.magnitude;
        
        wheel.localRotation *= Quaternion.Euler(0.0f, -current_velocity * Time.deltaTime, 0f);

        if( !updateVisualEffect )
        {
            return;
        }
        
        var terrain_type = TerrainManager.GetTerrainType( leg_world_position );
        
        for( var i = 0; i < wheelEffectPerTerrainType.Length; i++ )
        {
            if( !wheelEffectPerTerrainType[ i ] )
            {
                continue;
            }

            if( ( int )terrain_type == i && current_velocity > 0)
            {
                List<string> names = new List<string>();
                wheelEffectPerTerrainType[ i ].GetSpawnSystemNames( names );

                bool is_playing = false;

                foreach( string spawner_name in names )
                {
                    if( wheelEffectPerTerrainType[ i ].GetSpawnSystemInfo( spawner_name ).playing )
                    {
                        is_playing = true;
                        break;
                    }
                }

                if( !is_playing )
                {
                    wheelEffectPerTerrainType[ i ].Play();
                }
            }
            else
            {
                wheelEffectPerTerrainType[ i ].Stop();
            }
        }
    }
}
