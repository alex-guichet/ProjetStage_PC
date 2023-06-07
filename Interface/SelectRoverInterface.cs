using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Linq;

public class SelectRoverInterface : Singleton<SelectRoverInterface>
{
    [Tooltip( "The list of every player prefab with their placeholder and labels ")]
    public UIRoverGroup[] roverGroupList;
    [Tooltip( "The rotation speed per second of the Rovers in the menu" )]
    [SerializeField]
    private Vector3 rotationPerSecond;
    [Tooltip( "The confirm button in the UI" )]
    [SerializeField]
    private Button confirmButton;
    [Tooltip( "List of every every multiplayer Event System in the scene" )]
    [SerializeField]
    private List<EventSystem> eventSystemList;

    public Dictionary<int, GameObject> playerPrefabDictionary = new();
    private Dictionary<int, Button> _lastSelectedButton = new();
    private Dictionary<int, EventSystem> _deviceEventSystems = new();

    private string _textButtonPattern;
    private int _numberConfirmedPlayer;
    private Coroutine _rotationPlaceholdersCoroutine;
    private List<Transform> _roverTransformList = new();
    private bool _hasChangedPosition;

    [HideInInspector]
    public UnityEvent onConfirmPlayerSelection = new();
    [HideInInspector]
    public UnityEvent onConfirmPlayerCreated = new();

    private void PerformCancel( InputAction.CallbackContext obj )
    {
        int device_id = obj.control.device.deviceId;
        EventSystem device_event_system = _deviceEventSystems[ device_id ];
        Button last_selected_button = _lastSelectedButton[ device_id ];

        device_event_system.gameObject.GetComponent<InputSystemUIInputModule>().cancel.action.performed -= PerformCancel;

        _numberConfirmedPlayer--;
        UpdateConfirmButtonText();
        SetButtonColor( last_selected_button, Color.white );
        device_event_system.SetSelectedGameObject( last_selected_button.gameObject );
    }
    
    public void SelectRover( int rover_index )
    {
        EventSystem current_event_system = EventSystem.current;
        var         player_input         = current_event_system.gameObject.GetComponent<PlayerInput>();
        if (!player_input)
        {
            return;
        }

        int device_id = player_input.devices[ 0 ].deviceId;
        current_event_system.gameObject.GetComponent<InputSystemUIInputModule>().cancel.action.performed += PerformCancel;

       
        playerPrefabDictionary[ device_id ] = roverGroupList[ rover_index ].playerPrefab;


        Button selected_button = current_event_system.currentSelectedGameObject.GetComponent<Button>();

        if( !_lastSelectedButton.TryAdd( device_id, selected_button ) )
        {
            if( _lastSelectedButton[ device_id ] != null )
            {
                SetButtonColor( _lastSelectedButton[ device_id ], Color.white );
            }

            _lastSelectedButton[ device_id ] = selected_button;
        }

        SetButtonColor( selected_button, selected_button.colors.pressedColor );
        EventSystem.current.SetSelectedGameObject( confirmButton.gameObject );

        _numberConfirmedPlayer++;
        UpdateConfirmButtonText();
    }

    private void SetButtonColor( Button button, Color color )
    {
        ColorBlock temp_color_block = button.colors;
        temp_color_block.normalColor = color;
        button.colors = temp_color_block;
    }

    private void UpdateConfirmButtonText()
    {
        int gamepad_count = Gamepad.all.Count( x => x.device.description.interfaceName == "XInput" );

        int player_count = gamepad_count > 0 ? gamepad_count : 1;
        var text_button  = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        text_button.text = _textButtonPattern.Replace( "$", _numberConfirmedPlayer + "/" + player_count );

        confirmButton.interactable = _numberConfirmedPlayer == player_count;
    }

    public void Confirm()
    {
        Time.timeScale = 1f;
        onConfirmPlayerSelection.Invoke();
        onConfirmPlayerCreated.Invoke();
        transform.parent.gameObject.SetActive( false );
    }

    IEnumerator RotationPlaceHolder()
    {
        while( true )
        {
            foreach( UIRoverGroup u in roverGroupList )
            {
                u.roverPlaceholder.Rotate( rotationPerSecond * 0.03f );
            }
            yield return new WaitForSecondsRealtime( 0.03f );
        }
    }

    private void InitializeRoverGroups()
    {
        foreach (var rover in roverGroupList)
        {
            GameObject player_rover = Instantiate(rover.playerPrefab, rover.roverPlaceholder);
            GameObject rover_graphics = null;
            for (int i = 0; i < player_rover.transform.childCount; i++)
            {
                var rover_child = player_rover.transform.GetChild(i).gameObject;
                if (rover_child.GetComponent<PlayerController>())
                {
                    rover_graphics = rover_child;
                    _roverTransformList.Add(rover_graphics.transform);
                }
                else
                {
                    Destroy(rover_child);
                }
            }
            
            var player_weapon_manager = rover_graphics.GetComponent<PlayerWeaponManager>();
            rover.weaponListLabel.text = "Weapon List :\n";
            foreach (var weapon in player_weapon_manager.weaponList)
            {
                rover.weaponListLabel.text += "- "+weapon.modularWeapon.name+"\n";
            }
            
            Component[] components = rover_graphics?.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++) 
            {
                if (components[i] is not Transform)
                {
                    Destroy(components[i]);
                }
            }
            GameObject sm_Rover = null;
            GameObject rover_graphics_child;
            for (int i = 0; i < rover_graphics.transform.childCount; i++)
            {
                rover_graphics_child = rover_graphics.transform.GetChild(i).gameObject;
                if (rover_graphics_child.name != "sk_Rover")
                {
                    Destroy(rover_graphics_child);
                }
                else
                {
                    sm_Rover = rover_graphics_child;
                }
            }

            var modular_weapon_list = sm_Rover.GetComponentsInChildren<ModularWeapon>();
            foreach (var modular_weapon in modular_weapon_list)
            {
                Destroy(modular_weapon);
            }
        }
    }

    public override void Awake()
    {
        base.Awake();
        InitializeRoverGroups();
    }
    
    private void Start()
    {
        int gamepad_count = Gamepad.all.Count( x => x.device.description.interfaceName == "XInput" );
        var text_button   = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        _textButtonPattern = text_button.text;
        UpdateConfirmButtonText();

        if( gamepad_count == 0 )
        {
            EventSystem event_system = eventSystemList[ 4 ];
            event_system.gameObject.SetActive( true );

            int device_id = event_system.gameObject.GetComponent<PlayerInput>().devices[ 0 ].deviceId;

            _deviceEventSystems.TryAdd( device_id, event_system );
        }
        
        for( var i = 0; i < gamepad_count; i++ )
        {
            EventSystem event_system = eventSystemList[ i ];
            event_system.gameObject.SetActive( true );

            int device_id = event_system.gameObject.GetComponent<PlayerInput>().devices[ 0 ].deviceId;

            _deviceEventSystems.TryAdd( device_id, event_system );
        }
        _rotationPlaceholdersCoroutine = StartCoroutine( RotationPlaceHolder() );
        
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (_hasChangedPosition) 
            return;
        
        foreach (var rover_transform in _roverTransformList)
        {
            if (rover_transform.localPosition != Vector3.zero)
            {
                rover_transform.localPosition = Vector3.zero;
            }
        }
        _hasChangedPosition = true;
    }

    private void OnDisable()
    {
        StopCoroutine( _rotationPlaceholdersCoroutine );
    }
}

[System.Serializable]
public struct UIRoverGroup
{ 
    [Tooltip( "Player Rover Prefab" )]
    public GameObject playerPrefab;  
    [Tooltip( "Rover place holder in the grid" )]
    public Transform roverPlaceholder;   
    [Tooltip( "Player Rover Weapons Label" )]
    public TextMeshProUGUI weaponListLabel;
    
}
