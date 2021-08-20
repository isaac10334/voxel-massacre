using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;
using DG.Tweening;

public class UIThings : MonoBehaviour
{
    public enum MenuState
    {
        MainMenu,
        EscapeMenu
    }
    
    public static UIThings Instance;
    public SettingsMenu settingsMenu;
    public ReloadUI reloadUI;
    public CombatNotifications combatNotifications;
    public Leaderboard leaderboard;

    public Transform toolbarContainer;
    public GameObject toolbarSlotPrefab;
    public GameOverScreen gameOverScreen;
    public Slider healthSlider;
    [SerializeField] private TMP_Text menuText;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject menuCamera;
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private CanvasGroup youMenu;
    [SerializeField] private CanvasGroup loadoutMenu;

    // round information
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text redDeaths;
    [SerializeField] private TMP_Text blueDeaths;
    
    // avatar
    [SerializeField] private Avatar selectedAvatar;

    // loading screen
    [SerializeField] private CanvasGroup loadingScreen;
    [SerializeField] private Transform loadingScreenText;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_Text deathsNotificationText;
    [SerializeField] private string primaryWeaponChoice;
    [SerializeField] private string secondaryWeaponChoice;
    [SerializeField] private string meleeWeaponChoice;
    [SerializeField] private Button openYouMenuButton;
    [SerializeField] private Button closeYouMenuButton;
    [SerializeField] private Button openLoadoutMenuButton;
    [SerializeField] private GameObject quitButton;

    //how many toolbar slots you want
    public int toolbarSlots;
    private Dictionary<int, InventoryItemController> slotLookup = new Dictionary<int, InventoryItemController>();

    #region Inventory
    [SerializeField] private ItemsDatabase itemsDatabase;
    #endregion
    
    private MenuState _currentMenuState;
    private readonly string[] deathVerbs = new string[]
    {
        "destroyed",
        "obliterated",
        "murdered",
        "humiliated",
        "exterminated",
        "demolished"
    };

    private void Awake()
    {
        if(Application.platform == RuntimePlatform.WebGLPlayer)
        {
            quitButton.SetActive(false);
        }
        
        CreateSingleton();
        
        GenerateInventory();

        SetupAnimations();

        OpenMenuAsMainMenu();
        ShowLoadingScreen();

        gameOverScreen.CloseGameOverScreen();

        youMenu.gameObject.SetActive(false);

        openYouMenuButton.onClick.AddListener(() => OpenYouMenu());
        closeYouMenuButton.onClick.AddListener(() => CloseYouMenu());
        openLoadoutMenuButton.onClick.AddListener(() => OpenLoadoutMenu());
    }

    private void CreateSingleton()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void SetupAnimations()
    {
        TweenParams tParms = new TweenParams().SetLoops(-1).SetEase(Ease.OutElastic);
        interactText.transform.DOScale(1.20f, 2).SetAs(tParms);
        interactText.transform.DOScale(0.80f, 2).SetAs(tParms);
        loadingScreenText.transform.DOScale(1.1f, 2).SetAs(tParms);
    }

    public void OnGameStart()
    {
        gameOverScreen.CloseGameOverScreen();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(menu.activeInHierarchy)
            {
                CloseEscapeMenu();
            }
            else
            {
                OpenMenuAsEscapeMenu();
            }
        }

        if(Input.GetKeyDown(KeyCode.Tab))
        {
            leaderboard.OpenLeaderboard();
        }
        if(Input.GetKeyUp(KeyCode.Tab))
        {
            leaderboard.CloseLeaderboard();
        }
    }
    
    public void UpdateTimer(float currentTime)
    {
        timerText.text = ((int)currentTime).ToString();
    }

    public void UpdateScore(int redDeaths, int blueDeaths)
    {
        this.redDeaths.text = redDeaths.ToString();
        this.blueDeaths.text = blueDeaths.ToString();
    }

    public void OnPlayerDeath()
    {
        Debug.Log("Player died.");
        OpenMenuAsMainMenu();
    }
    
    private void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowToolbar(bool value)
    {
        toolbarContainer.gameObject.SetActive(value);
    }

    #region Inventory
    private void GenerateInventory()
    {
        //it might be a good idea to clear everything before doing this. destroy all children and clear the dictionary
        for(int i = 0; i < toolbarSlots; i++)
        {
            GameObject newObj = Instantiate(toolbarSlotPrefab);
            newObj.transform.SetParent(toolbarContainer, false);
            InventoryItemController controller = newObj.GetComponent<InventoryItemController>();

            controller.slotNumber = i;
            controller.isToolbarSlot = true;
            slotLookup.Add(i, controller);

        }
    }
    public void AddInventoryItemToUI(InventoryItem item)
    {
        InventoryItemController controller = slotLookup[item.slot];

        if(controller.slotNumber == item.slot)
        {
            if(!controller.isToolbarSlot)
                controller.itemName.text = item.name;

            controller.itemImage.enabled = true;
            controller.itemImage.sprite = itemsDatabase.GetItemByName(item.name).sprite;
        }
        else
        {
            Debug.Log("For some reason the slot number in the dictionary doesn't match the one on the item.");
        }
    }
    public void RemoveItemFromUI(int slot)
    {
        InventoryItemController controller = slotLookup[slot];

        if(controller.slotNumber == slot)
        {
            if(!controller.isToolbarSlot)
                controller.itemName.text = "";

            controller.itemImage.sprite = null;
        }
        else
        {
            Debug.Log("Slot lookup logic error");
        }
    }

    public void SelectSlot(int slot)
    {
        InventoryItemController controller = slotLookup[slot];
        controller.SelectThisSlot();
    }
    #endregion

    public void SetHealthText(int value)
    {
        healthSlider.value = value;
    }
    public void ShowInteractionText(string content)
    {
        interactionText.text = content;
    }
    public async void ShowNotificationText(string content, float time)
    {
        notificationText.text = content;

        await new WaitForSeconds(time);
        notificationText.text = String.Empty;
    }
    public void GameOver(Team winningTeam)
    {
        Pause();

        Player localPlayer = NetworkClient.localPlayer.GetComponent<Player>();
        Team playersTeam = localPlayer.team;

        if(playersTeam == winningTeam)
        {
            gameOverScreen.PlayerWon(playersTeam);
        }
        else
        {
            gameOverScreen.PlayerLost(playersTeam);
        }
    }
    public void Tie()
    {
        Pause();
        gameOverScreen.Tie();
    }

    public void OpenMenuAsEscapeMenu()
    {
        if(NetworkClient.connection == null) return;

        menuText.text = "Click to Resume";
        menuCamera.SetActive(false);
        menu.SetActive(true);
        gameUI.SetActive(false);
    
        Pause();

        _currentMenuState = MenuState.EscapeMenu;
    }

    public void CloseEscapeMenu()
    {
        if(NetworkClient.connection == null) return;
        if(NetworkClient.localPlayer == null) return;

        CloseMenu();

        menu.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        PlayerInput.MouseInputEnabled = true;
        PlayerInput.InputEnabled = true;
        Player.Instance.UnlockPlayer();
    }

    public void OpenMenuAsMainMenu()
    {
        UnlockCursor();

        menuText.text = "Click to Play";
        menuCamera.SetActive(true);
        menu.SetActive(true);
        gameUI.SetActive(false);

        _currentMenuState = MenuState.MainMenu;
    }

    private void Pause()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        PlayerInput.MouseInputEnabled = false;
        PlayerInput.InputEnabled = false;

        if(NetworkClient.active && NetworkClient.localPlayer != null)
        {
            NetworkClient.localPlayer.GetComponent<Player>().LockPlayer();
        }
    }

    public void CloseMenu()
    {
        menu.SetActive(false);
        gameUI.SetActive(true);
    }

    public void ClickedMenuArea()
    {
        if(_currentMenuState == MenuState.MainMenu)
        {
            ((MyNetworkManager)MyNetworkManager.singleton).Spawn(GetCurrentAvatarChoice(), GetCurrentLoadoutChoice(), usernameInput.text);
        }
        else if(_currentMenuState == MenuState.EscapeMenu)
        {
            CloseEscapeMenu();
        }
    }

    private Loadout GetCurrentLoadoutChoice()
    {
        return new Loadout(primaryWeaponChoice,
                            secondaryWeaponChoice,
                            meleeWeaponChoice);
    }
    private Avatar GetCurrentAvatarChoice()
    {
        return selectedAvatar;
    }

    private void SelectAvatar(Avatar avatar)
    {
        selectedAvatar = avatar;
    }

    public void SelectPrimaryLoadout(string item)
    {
        primaryWeaponChoice = item;
    }
    public void SelectSecondaryLoadout(string item)
    {
        secondaryWeaponChoice = item;
    }
    public void SelectMeleeLoadout(string item)
    {
        meleeWeaponChoice = item;
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene("Main");
    }

    #region LoadingScreen
    public void ShowLoadingScreen()
    {
        loadingScreen.gameObject.SetActive(true);
        loadingScreen.alpha = 1f;
    }
    public async void HideLoadingScreen()
    {
        await loadingScreen.DOFade(0f, 0.25f).AsyncWaitForCompletion();
        loadingScreen.gameObject.SetActive(false);
    }
    #endregion

    #region DeathsNotification
    public void UpdateDeaths(Player killer, Player victim)
    {
        deathsNotificationText.text += killer.username + " " + GetRandomDeathVerb() + " " + victim.username;
    }

    private string GetRandomDeathVerb()
    {
        int range = UnityEngine.Random.Range(0, 100);

        return deathVerbs[UnityEngine.Random.Range(0, deathVerbs.Length)];
    }

    #endregion

    #region PlayerCustomizationMenus
    private void OpenYouMenu()
    {
        youMenu.gameObject.SetActive(true);
    }
    private void CloseYouMenu()
    {
        youMenu.gameObject.SetActive(false);
    }
    private void OpenLoadoutMenu()
    {
        loadoutMenu.gameObject.SetActive(true);
    }

    public void OnClientJoined(NetworkIdentity identity)
    {
        leaderboard.AddPlayer(identity);
    }

    #endregion
    public void Quit()
    {
        Application.Quit();
    }
}
