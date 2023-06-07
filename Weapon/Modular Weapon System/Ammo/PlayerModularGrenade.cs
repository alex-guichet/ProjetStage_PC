using System;
using System.Buffers.Text;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerModularGrenade : PlayerModularAmmo
{
    protected override void Update()
    {
        if (Mathf.Abs(Vector3.Distance(transform.position, endPosition)) <= 1f)
        {
            Kill( true );
        }
        transform.Rotate(3f,5f,4f);
    }
    
    protected override void OnTriggerEnter( Collider other )
    {
        if( other.gameObject.layer == Constants.enemyLayer || other.gameObject.layer == Constants.groundLayer )
        {
            ImpactFeedbacks?.PlayFeedbacks();
            Kill( true );
            Hit(other.transform);
        }
    }
    
}