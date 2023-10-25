using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ContolsType
{
    Touch,
    Keyboard,
    JoystickPS,
    JoystickXBOX,
    DefaultGamepad
}

public enum ActionType
{
    Interact = 0,
    UICancel = 1,
    Switch = 2,
    Attack = 3,
    SubAttack = 4,
    Dash = 5,
    LevelUp = 6,
    Pause = 7,
    AimH = 8,
    Vertical = 9,
    Map = 10,
    Options = 11,
    Recycle = 12,
    Movements = 13,
    Shield = 14
}

public class DisplayInput : MonoBehaviour
{
    public static DisplayInput Instance;

    public ContolsType CurrentController;

    public static Action<ContolsType> OnControllerChanged;

    [SerializeField] InputMapping m_Mapping = null;

    private List<_Sc_InputIndication> m_Indicators = new List<_Sc_InputIndication>();

    public bool IsJoystick => CurrentController == ContolsType.JoystickPS || CurrentController == ContolsType.JoystickXBOX;
    public bool UseJoystick => m_UseJoystick;

    private bool m_UseJoystick;

    private Player m_Player;
    private ContolsType m_JoystickType = ContolsType.JoystickPS | ContolsType.JoystickXBOX;
    private Vector3 m_PrevMousePosition;
    private double m_LastActiveTime;
    private GameObject m_LastSelectedObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ReInput.ControllerConnectedEvent += OnControllerConnected;
            ReInput.ControllerDisconnectedEvent += OnControllerDisconnected;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        m_Player = ReInput.players.GetPlayer(0);

        for (int i = 0; i < ReInput.controllers.joystickCount; i++)
        {
            if (ReInput.controllers.Joysticks[i].ImplementsTemplate<IGamepadTemplate>())
            {
                string controllerName = ReInput.controllers.Joysticks[i].name.ToLower();

                if (controllerName.Contains("uinput-fpc") || controllerName.Contains("uinput-fortsense") || controllerName.Contains("keyboard"))
                {
                    continue;
                }

                InitJoystick();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        ReInput.ControllerConnectedEvent -= OnControllerConnected;
        ReInput.ControllerDisconnectedEvent -= OnControllerDisconnected;
    }

    private void InitJoystick()
    {
        string gamepadName = Input.GetJoystickNames()[0].ToUpper();

        if (gamepadName.Contains("DUALSHOCK") ||
            gamepadName.Contains("DUALSENSE") ||
            gamepadName.Contains("TOUCHPAD") ||
            gamepadName.Contains("DS2") ||
            gamepadName.Contains("DS3") ||
            gamepadName.Contains("DS4") ||
            gamepadName.Contains("DS5")
            )
        {
            m_JoystickType = ContolsType.JoystickPS;
        }
        else
        {
            m_JoystickType = ContolsType.JoystickXBOX;
        }

        Debug.Log("**** Connected gamepad **** " + gamepadName + " Type is: " + m_JoystickType);

        m_UseJoystick = true;
    }

    private void Update()
    {

#if UNITY_EDITOR
        if (Input.mousePosition != m_PrevMousePosition)
        {
            m_LastActiveTime = ReInput.time.unscaledTime;
            m_PrevMousePosition = Input.mousePosition;
            EventSystem.current.SetSelectedGameObject(null);
            SetController(ContolsType.Keyboard);
        }

        if (Input.GetKeyDown(KeyCode.T)) SetController(ContolsType.Touch);
        if (Input.GetKeyDown(KeyCode.P)) SetController(ContolsType.JoystickPS);
        if (Input.GetKeyDown(KeyCode.X)) SetController(ContolsType.JoystickXBOX);
        if (Input.GetKeyDown(KeyCode.D)) SetController(ContolsType.DefaultGamepad);
#endif

        if (m_UseJoystick)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                m_LastActiveTime = ReInput.time.unscaledTime;
                EventSystem.current.SetSelectedGameObject(null);
                SetController(ContolsType.Touch);
            }

            if (m_Player.controllers.Joysticks[0].GetLastTimeActive() >= m_LastActiveTime)
            {
                m_LastActiveTime = ReInput.time.unscaledTime;
                SetController(m_JoystickType);
            }

            //DEBUG
            //Controller joystick = m_Player.controllers.Joysticks[0];
            //foreach (var item in joystick.Buttons)
            //{
            //    if (item.value)
            //        print(item.name);
            //}
        }
    }

    public void SetController(ContolsType controller)
    {
        if (CurrentController == controller) return;

        CurrentController = controller;

        if (controller != ContolsType.Touch)
        {
            foreach (var item in m_Indicators)
            {
                item.inputImage.sprite = m_Mapping.GetSprite(item.actionId, controller);
                item.Show(true);
            }
            UpdateUI(true);
        }
        else
        {
            foreach (var item in m_Indicators)
            {
                item.Show(false);
            }
            UpdateUI(false);
        }

        OnControllerChanged?.Invoke(controller);
    }

    private void UpdateUI(bool show)
    {
        if (FullMapMenu.instance == null || !FullMapMenu.instance.enabled)
        {            
            if (show)
            {
                EventSystem.current.SetSelectedGameObject(m_LastSelectedObject);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        if (_Sc_DisplayInputs.Instance != null)
        {
            _Sc_DisplayInputs.Instance.Show(show);
        }

        if(show == false && Pause.instance != null && Pause.instance.GameIsPaused)
        {
            return;
        }

        PlayerInput.Instance.HideGamepad(show);
    }

    public void Add(_Sc_InputIndication indicator)
    {
        m_Indicators.Add(indicator);

        if (CurrentController == ContolsType.Touch)
        {
            indicator.Show(false);
        }
        else
        {
            indicator.inputImage.sprite = m_Mapping.GetSprite(indicator.actionId, CurrentController);
            indicator.Show(true);
        }
    }

    public void Remove(_Sc_InputIndication indicator)
    {
        m_Indicators.Remove(indicator);
    }

    public Sprite GetSpriteForAction(ActionType actionId)
    {
        if (CurrentController == ContolsType.Touch)
        {
            return null;
        }
        else
        {
            return m_Mapping.GetSprite(actionId, CurrentController);
        }
    }

    public void SetSelectedGameObject(GameObject gameObject)
    {
        m_LastSelectedObject = gameObject;
        if (CurrentController != ContolsType.Touch)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    private void OnControllerConnected(ControllerStatusChangedEventArgs obj)
    {
        InitJoystick();
    }

    private void OnControllerDisconnected(ControllerStatusChangedEventArgs obj)
    {
        m_UseJoystick = false;
    }
}
