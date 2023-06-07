using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerModularLaser : PlayerModularAmmo
{
    public LineRenderer lineRenderer;
    public float laserSpeed;
    public float damageTime = 1f;
    
    private float _laserMaxDistance;
    private float _laserDistance;
    private BoxCollider _boxCollider;
    private float _laserWidth;

    protected void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
    }

    protected override void Start()
    {
        TimeToLive = (float)modularWeapon.shootingTime.GetValue();
        _laserWidth = (float)modularWeapon.laserWidth.GetValue();
        _laserMaxDistance = (float)modularWeapon.range.GetValue();
        Vector3 collider_size = _boxCollider.size;
        _boxCollider.size = new Vector3(collider_size.x * _laserWidth, collider_size.y, collider_size.z );
        lineRenderer.widthMultiplier = _laserWidth;
    }

    protected override void Update()
    {
        base.Update();
        if (_laserDistance < _laserMaxDistance)
        {
            _laserDistance += Time.deltaTime * laserSpeed;
            Vector3 collider_size = _boxCollider.size;
            _boxCollider.size = new Vector3(collider_size.x, collider_size.y, _laserDistance);
            _boxCollider.center = new Vector3(0f, 0f, _laserDistance / 2f);
        }

        Transform current_transform = transform;
        var position = current_transform.position;
        lineRenderer.SetPosition(0, position);
        lineRenderer.SetPosition(1, position + (current_transform.forward * _laserDistance));
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit_info, _laserDistance, Constants.obstacleLayer))
        {
            lineRenderer.SetPosition(1, hit_info.point);
        }
    }

    protected override void FixedUpdate()
    {
    }
    
    protected override void OnTriggerEnter( Collider other )
    {
        if( other.gameObject.layer == Constants.enemyLayer)
        {
            ImpactFeedbacks?.PlayFeedbacks();
            var enemy = other.GetComponent<EnemyBase>();
            if (!enemy)
                return;
            
            Hit(other.transform);
        }
    }
}