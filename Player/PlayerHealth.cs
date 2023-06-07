using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float initialHealthPoints;
    private float _healthPoints;
    
    public float healthPoints
    {
        get => _healthPoints;
        set
        {
            _healthPoints = value;
            var hudManager = HUDManager.Instance;
            if (hudManager != null)
            {
                hudManager.resourcesInterface.UpdateHealth(healthPoints, initialHealthPoints);
            }
            if( _healthPoints <= 0.0f )
            {
                Kill();
            }
        }
    }

    public void Awake()
    {
        healthPoints = initialHealthPoints;
    }
    
    public void ReceiveDamage(float damage_amount)
    {
        if( CheatManager.Instance.cheat_GodModeActive )
        {
            return;
        }
        
        healthPoints -= damage_amount;
    }
    
    private void Kill()
    {
        //Destroy(gameObject);
    }
}
