using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerWheel : MonoBehaviour
{
    [Tooltip("height offset applied when moving the wheel to the floor")]
    public float yPivotOffset;
    public float rotationRatio;
    public bool updateVisualEffect;
    public VisualEffect[] wheelEffectPerTerrainType;

    private Vector3 _startLocalPos;
    public PlayerController playerController;

    private void Start()
    {
        _startLocalPos = transform.localPosition;
    }
    
    private void LateUpdate()
    {
        transform.localPosition = _startLocalPos;
        Vector3 position = transform.position;

        if( Physics.Raycast( position + Vector3.up * 2.0f, Vector3.down, out RaycastHit hit_info, 4.0f, 1 << Constants.groundLayer ) )
        {
            position.y = hit_info.point.y + yPivotOffset;
        }

        transform.position = position;
        float current_velocity = rotationRatio * playerController.currentVelocity.magnitude;
        
        transform.localRotation *= Quaternion.Euler((current_velocity * Time.deltaTime), 0f, 0f);

        if( !updateVisualEffect )
        {
            return;
        }
        
        var terrain_type = TerrainManager.GetTerrainType( position );
        
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
