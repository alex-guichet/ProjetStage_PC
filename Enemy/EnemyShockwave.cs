using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(TargetPoint))]
public class EnemyShockwave : MonoBehaviour
{
    [Tooltip("Full size of the shockwave")]
    public Vector3 shockwaveSize;
    [Tooltip("Charging Time")]
    public float chargingTime;
    [Tooltip("Distance of the Shockwave")]
    public float shockwaveDistance;
    [Tooltip("Speed shockwave")]
    public float shockwaveSpeed;
    [Tooltip("Damage of the shockwave")]
    public float shockwaveDamage;
    [Tooltip("Terraform each x seconds")]
    public float terraformInterval = 0.2f;
    [Tooltip("Size of the crater")]
    public Vector3 craterSize;
    
    [HideInInspector]
    public EnemyMajor enemyMajor;
    
    private Vector3 _startPositionSpawn;
    private float _stepCharge;
    private float _totalCharge;
    private Rigidbody _shockwaveRb;
    private TargetPoint _playerTargetPoint;

    IEnumerator Charging()
    {
        float distance = Vector3.Distance(transform.localScale, shockwaveSize);
        Vector3 startScale = transform.localScale;
        
        while (Vector3.Distance(transform.localScale, shockwaveSize) > 0.01f)
        {
            _stepCharge = (distance / chargingTime) * Time.deltaTime;
            _totalCharge += _stepCharge;
            transform.localScale = Vector3.Lerp(startScale, shockwaveSize, _totalCharge/distance);
            yield return new WaitForEndOfFrame();
        }
        transform.localScale = shockwaveSize;
        StartCoroutine(Blasting());
    }

    IEnumerator Blasting()
    {
        _startPositionSpawn = transform.position;
        float terraform_time = 0f;
        
        while (Vector3.Distance(_startPositionSpawn, transform.position) < shockwaveDistance)
        {
            _shockwaveRb.velocity = transform.forward * shockwaveSpeed;
        
            terraform_time += Time.deltaTime;
            if (terraform_time < terraformInterval)
            {
                if( TerrainManager.HasInstance() )
                {
                    StartCoroutine( TerrainManager.Instance.Terraform( Random.Range(craterSize.x,craterSize.z), craterSize.y, transform.position, false ) );
                    terraform_time = 0f;
                }
            }
            yield return new WaitForEndOfFrame();
        }
        Vector3 end_position = transform.position;
        TerrainManager.Instance.EndTerraform(() => StartCoroutine( GameMaster.Instance.SpawnEnemiesInLine(enemyMajor.enemyFormation, _playerTargetPoint, 
            enemyMajor.spawnInterval, _startPositionSpawn, end_position )));
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if( !other.CompareTag( "Player" ) )
        {
            return;
        }
        
        PlayerHealth player_health = other.GetComponent<PlayerHealth>();
        if (player_health)
        {
            player_health.ReceiveDamage(shockwaveDamage);
            TerrainManager.Instance.EndTerraform(() => StartCoroutine(GameMaster.Instance.SpawnEnemiesInLine(enemyMajor.enemyFormation, _playerTargetPoint, 
                enemyMajor.spawnInterval, _startPositionSpawn, other.transform.position )));
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
        _shockwaveRb = GetComponent<Rigidbody>();
    }
    
    private void Start()
    {
        _playerTargetPoint = enemyMajor.currentPlayerTargeted.GetComponentInChildren<TargetPoint>();
        StartCoroutine(Charging());
    }
}
