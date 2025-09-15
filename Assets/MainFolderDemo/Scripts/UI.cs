using UnityEngine;
using UnityEngine.UI; // for Image
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;  // for dash VFX

//using UnityEngine.EventSystems;
//using Unity.VisualScripting;

[System.Serializable]
public class MagicStationUI
{
    public MagicStation station; // the scene object
    public Text text;            // the UI Text to show
    public MagicType type;       // which magic this station equips
    public string label;         // nice name, for ex) crystal
}


public class UI : MonoBehaviour
{
    public Transform player;            //we fetch player like this and not script in start like the magic manager.
    private ArmMovementMegaScript arm;       // we’ll assign grenade prefab on this
    private MagicManager magicManager;      // Direct reference instead of Instance
    private GrenadeManager grenadeManager;


    [Header("Weapon Info UI")]
    public Text WeaponAmmoText;
    public Text WeaponNameText;
    public Text setBonusText;


    [Header("Mystery Box Popup UI")]
    public Text MysteryBoxText;
    public MysteryBox mysteryBox;    //getting script

    [Header("Round UI")]
    public Text roundText;
    public Text roundCountdownText;
    public ZombieSpawner zombieSpawner;  //getting script

    [Header("Kinetic Slam UI")]
    public Slider slamCooldownSlider;
    public PlayerMovement playerMovement;
    public Text kineticJumpHintText;
    public Text slamHintText;
    public Slider kineticJumpCooldownSlider;
    private float kineticHintReadyTime = -999f; // when the hint is allowed to appear

    [Header("Player Health UI")]
    public Slider playerHealthSlider;  

    [Header("Points UI")]
    public Text pointsText;

    [Header("Ammo Box UI")]
    public Text ammoBoxText;
    public AmmoBox ammoBox; // reference to your ammo box object/script

    //[Header("Magic Station UI")]
    //public Text fireMagicText;                   
    //public Text crystalMagicText;
    //public Text VoidMagicText;
    //public Text IceMagicText;
    //public Text VenomMagicText;
    //public Text LightningMagicText;
    //public Text WindMagicText;
    //public Text MeteorMagicText;
    //public Text CrimsonMagicText;
    //public MagicStation fireStation;
    //public MagicStation crystalStation;  
    //public MagicStation VoidMagicStation;
    //public MagicStation IceMagicStation;
    //public MagicStation VenomMagicStation;
    //public MagicStation LightningMagicStation;
    //public MagicStation WindMagicStation;
    //public MagicStation MeteorMagicStation;
    //public MagicStation CrimsonMagicStation;
    
    
    [Header("Magic Station UI")]
    public MagicStationUI[] magicStations;
    public Text NoMoneyStatusText;

    [Header("Magic Cooldown UI")]
    public Slider magicCooldownSlider;
    public Text magicStatusText;
    public Image magicCooldownFill;

    [Header("Magic Cooldown Colors")]
    public Color normalColor = new Color(1f, 0.35f, 0.2f);      
    public Color crystalColor = new Color(0.35f, 0.85f, 1f);
    public Color voidColor = new Color(0.6f, 0.3f, 1f);
    public Color iceColor = new Color(0.6f, 0.9f, 1f);
    public Color venomColor = new Color(0.5f, 1f, 0.4f);
    public Color lightningColor = new Color(1f, 1f, 0.4f);
    public Color windColor = new Color(0.8f, 1f, 0.9f);
    public Color meteorColor = new Color(1f, 0.5f, 0.1f);
    public Color crimsonColor = new Color(1f, 0.2f, 0.35f);

    [Header("Magic Slot UI")]
    public Image magicSlotIcon;  // assign in inspector
    public Sprite noneIcon;
    public Sprite normalIcon;
    public Sprite crystalIcon;
    public Sprite voidIcon;
    public Sprite iceIcon;
    public Sprite venomIcon;
    public Sprite lightningIcon;
    public Sprite windIcon;
    public Sprite meteorIcon;
    public Sprite crimsonIcon;

    //-------------------- grenade

    [Header("Grenade Chest UI")]
    public GrenadeChest grenadeChest;   
    public Text grenadePrompt;     // "Press [E] to open"
    public GameObject grenadePanel;
    public Text grenadeAmountText;
    public Text grenadeStatusText;
    private Coroutine grenadeMsgCo;
    private bool grenadePanelOpen = false;

    [Header("Grenade Slot UI")]
    public Image grenadeSlotIcon;
    public Sprite fragIcon;
    public Sprite impactIcon;
    public Sprite semtexIcon;
    public Sprite bioIcon;
    public Sprite sulfuricNapalmIcon;
    public Sprite crystalClusterIcon;
    public Sprite bastionIcon;
    public Sprite ragnarokIcon;
    public Sprite spiderIcon;
    public Sprite noneGrenadeIcon;

    //-------------------------

    [Header("Perk Bar")]
    public RectTransform perkBar;       // Empty UI object with HorizontalLayoutGroup

    public Image perkIconPrefab;        // Simple Image prefab (no script) a tiny prefab whose root is an Image (no scripts needed). This is what gets cloned for each icon.
    public Sprite speedIcon;
    public Sprite healthIcon;
    public Sprite reviveIcon;
    public Sprite fireRateIcon;
    public Sprite fastHandsIcon;
    public Sprite magicCooldownIcon;
    public Sprite moreDropsIcon;
    public Sprite adrenalineIcon;
    public Sprite lessAmmoConsumeIcon;
    public Sprite moreDashSlamIcon;
    public Sprite resurrectIcon;

    private readonly List<PerkType> _perkOrder = new List<PerkType>();  //a list that remembers which perks were added and in what order (first → last). Useful if you ever need to read them back in order.
    private readonly Dictionary<PerkType, Image> _activePerkIcons = new Dictionary<PerkType, Image>();  //map/dict so we can perkkType --> img assign to that perk. 
    
    public Text fireRatePerkText;
    public Text speedPerkText;
    public Text healthPerkText;
    public Text revivePerkText;
    public Text fastHandsPerkText;
    public Text magicCooldownPerkText;
    public Text moreDropsPerkText;
    public Text adrenalinePerkText;
    public Text lessAmmoConsumePerkText;
    public Text moreDashSlamPerkText;
    public Text resurrectPerkText;
    //-------------------------- PACK
    [Header("Pack A Punch")]
    public Text packAPunchText;           // Text to show Pack-A-Punch prompts
    public PackAPunch packAPunchMachine;

    // timed power-up UI
    private Coroutine powerupTimerCo;
    public RectTransform statusStackRoot;
    public Text statusRowPrefab;

    private readonly System.Collections.Generic.Dictionary<string, UnityEngine.UI.Text> _timedRows =
    new System.Collections.Generic.Dictionary<string, UnityEngine.UI.Text>();
    private readonly System.Collections.Generic.Dictionary<string, Coroutine> _timedCoroutines =
        new System.Collections.Generic.Dictionary<string, Coroutine>();

    //------------------ INFUSION 
    [Header("Infusion UI")]
    public GameObject infusePanel;
    public Text infusePromptText;
    public InfuseStation currentInfuseStation;
    private bool infusePanelOpen = false;
    public Text infuseStatusText;
    [HideInInspector] Weapon currentWeapon;  //we need for cur weapon.

    [Header("Infusion Slot UI")]
    public Image infusionSlotIcon;
    public Sprite infusionNoneIcon;
    public Sprite infusionFireIcon;
    public Sprite infusionCrystalIcon;
    public Sprite infusionVoidIcon;
    public Sprite infusionIceIcon;
    public Sprite infusionVenomIcon;
    public Sprite infusionLightningIcon;
    public Sprite infusionWindIcon;
    public Sprite infusionMeteorIcon;
    public Sprite infusionCrimsonIcon;

    [Header("Magic VFX Slot UI")]
    public GameObject fireMagicVFX;
    public GameObject crystalMagicVFX;
    public GameObject voidMagicVFX;
    public GameObject iceMagicVFX;
    public GameObject venomMagicVFX;
    public GameObject lightningMagicVFX;
    public GameObject windMagicVFX;
    public GameObject meteorMagicVFX;
    public GameObject crimsonMagicVFX;

    [Header("Dash UI")]
    public Slider dashCooldownSlider;    
    public Image dashFill;               
    public Color dashReadyColor = Color.white;
    public Image dashVFXPicture;
    //public Color dashRechargingColor = new Color(1f, 1f, 1f, 0.6f);
    //public Text DashNotReadyText;   //when u try to dash but it cooling down  !!!!!!!!!!!!! We can come back to this 

    [Header("Post Processing")]
    public PostProcessVolume postProcessVolume;
    private ChromaticAberration chromaticAberration;
    private DepthOfField depthOfFeild;

    [Header("Elimination UI")]
    public RectTransform eliminationStackRoot;
    public Text eliminationRowPrefab;
    private readonly List<Text> _activeEliminationRows = new List<Text>();
    public int maxEliminationRows = 10; // cap

    [Header("Debris")]
    public Text DebrisText;


    void Start()
    {
        magicManager = FindFirstObjectByType<MagicManager>();  // Find it once at start
        grenadeManager = FindFirstObjectByType<GrenadeManager>();  
        arm = FindFirstObjectByType<ArmMovementMegaScript>();

        if (grenadePanel) grenadePanel.SetActive(false); //set panel to false off rip
        if (grenadePrompt) grenadePrompt.gameObject.SetActive(false);  //set text to false off rip

        if (infusePanel) infusePanel.SetActive(false);
        if (infusePromptText) infusePromptText.gameObject.SetActive(false);

        if (infuseStatusText) infuseStatusText.text = "";   //start empty

        if (grenadeManager != null)
        {
            GetGrenadeSpriteForSlots(grenadeManager.currentType);
        }

        //coloring the fill Bar
        if (magicCooldownFill == null && magicCooldownSlider != null && magicCooldownSlider.fillRect != null)
        {
            magicCooldownFill = magicCooldownSlider.fillRect.GetComponent<Image>();
        }

        //dash Recharge Bar
        if (dashFill == null && dashCooldownSlider != null && dashCooldownSlider.fillRect != null)
        {
            dashFill = dashCooldownSlider.fillRect.GetComponent<Image>();
        }

        if (kineticJumpHintText)  //disable on start
        {
            kineticJumpHintText.gameObject.SetActive(false);
        }

        if (slamHintText)  //turn off slam text on start
        {
            slamHintText.gameObject.SetActive(false);
        }

        if (dashVFXPicture)
        {
            dashVFXPicture.gameObject.SetActive(false); 
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out chromaticAberration);
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.Override(0f); // start off
                chromaticAberration.active = true;         // keep it enabled, just zeroed
            }

            postProcessVolume.profile.TryGetSettings(out depthOfFeild);
            if (depthOfFeild != null)
            {
                depthOfFeild.focusDistance.Override(10f); // default “no blur”
                depthOfFeild.active = true;              // keep enabled, just tweak distance
            }
        }

        // KJ bar: init and hide
        if (kineticJumpCooldownSlider != null)
        {
            kineticJumpCooldownSlider.minValue = 0f;
            kineticJumpCooldownSlider.maxValue = 1f;
            kineticJumpCooldownSlider.value = 0f;
            kineticJumpCooldownSlider.gameObject.SetActive(false);
        }


    }

    //bool chestPanelOpen = false;

    private string GetWeaponDisplayName(Weapon weapon)
    {
        if (weapon.upgradeLevel <= 0)
        {
            return weapon.weaponName; // Normal weapon name
        }
        else
        {
            string tierName = GetTierSuffix(weapon.upgradeLevel);
            return $"{weapon.weaponName} {tierName}";
        }
    }

    private string GetTierSuffix(int level)
    {
        switch (level)
        {
            case 1: return "Tier I";
            case 2: return "Tier II";
            case 3: return "Tier III";
            default: return "Max Tier";
        }
    }

    void Update()
    {
        Weapon currentWeapon = WeaponManager.ActiveWeapon;

        //--------------------------------------------------------------- Weapon UI

        if (currentWeapon != null)
        {
            WeaponAmmoText.text = currentWeapon.GetCurrentAmmo() + " / " + currentWeapon.GetAmmoReserve();
            WeaponNameText.text = GetWeaponDisplayName(currentWeapon);
        }
        else
        {
            WeaponAmmoText.text = "-- / --";
            WeaponNameText.text = "No Weapon";
        }

        //--------------------------------------------------------------- Set Bonus UI
        if (setBonusText != null)
        {
            if (currentWeapon != null && WeaponManager.HasSetBonus())
            {
                Color c = GetColorForInfusion(currentWeapon.infusion);
                string hex = ColorUtility.ToHtmlStringRGB(c);

                switch (currentWeapon.infusion)
                {
                    case InfusionType.Fire:
                        setBonusText.text = $"<color=#{hex}>Fire Infused Magic Set Bonus</color>\n25% Increased on Burn Damage";
                        break;
                    case InfusionType.Ice:
                        setBonusText.text = $"<color=#{hex}>Ice Infused Magic Set Bonus:</color>\n10% Increased Freeze Time\n10% Additonal Decrease Enemy Speed";
                        break;
                    case InfusionType.Wind:
                        setBonusText.text = $"<color=#{hex}>Wind Infused Magic Set Bonus:</color>\n15% Increased Knockback";
                        break;
                    case InfusionType.Lightning:
                        setBonusText.text = $"<color=#{hex}>Lightning Infused Magic Set Bonus:</color>\n30% Increased Lightning Distance";
                        break;
                    case InfusionType.Crystal:
                        setBonusText.text = $"<color=#{hex}>Crystal Infused Magic Set Bonus:</color>\n25% Increased Crystal Shatter Distance";
                        break;
                    case InfusionType.Venom:
                        setBonusText.text = $"<color=#{hex}>Venom Infused Magic Set Bonus:</color>\n45% Increased on Poison Venom Damage";
                        break;
                    case InfusionType.Meteor:
                        setBonusText.text = $"<color=#{hex}>Meteor Infused Magic Set Bonus:</color>\n12.5% Increased Damage for each Meteor";
                        break;
                    case InfusionType.Crimson:
                        setBonusText.text = $"<color=#{hex}>Crimson Infused Magic Set Bonus:</color>\n10% Increase Healing Per Bullet";
                        break;
                    case InfusionType.Void:
                        setBonusText.text = $"<color=#{hex}>Void Infused Magic Set Bonus</color>\n35% Increased on Portal Damage";
                        break;
                    default:
                        setBonusText.text = "";
                        break;
                }
            }
            else
            {
                setBonusText.text = ""; // hide when no bonus
            }
        }

        //--------------------------------------------------------------- Mystery Box UI

        const int BOX_COST = 950;  // always 950 points

        float distanceToBox = Vector3.Distance(player.position, mysteryBox.transform.position);   //we check distance from player and box

        bool PlayerIsCloseCanOpenBox = !mysteryBox.IsBoxOpen() && distanceToBox <= mysteryBox.minimumDistanceToOpen;      //box closed + close
        bool PlayerIsCloseCanGrabWeapon = mysteryBox.IsBoxOpen() && distanceToBox <= mysteryBox.minimumDistanceToOpen;   //box open  + close

        if (PlayerIsCloseCanOpenBox)
        {
            int pts = (PointManager.Instance != null) ? PointManager.Instance.GetPoints() : 0;

            if (pts < BOX_COST)
            {
                // not enough → show text + optional toast
                MysteryBoxText.text = "Not enough points";
                MysteryBoxText.gameObject.SetActive(true);

                if (KeybindManager.Instance.GetKeyDown("Interact"))
                    ShowTemporaryPerkMessage("Not enough points");
            }
            else
            {
                // enough → just show buy prompt; DO NOT subtract here
                string interactKey = KeybindManager.Instance.GetKeyName("Interact");
                MysteryBoxText.text = $"Press [{interactKey}] to Open Weapon Box 950 Points";
                MysteryBoxText.gameObject.SetActive(true);
            }
        }
        else if (PlayerIsCloseCanGrabWeapon && mysteryBox.GetCurrentPreview() != null)
        {
            Weapon weapon = mysteryBox.GetCurrentPreview().GetComponent<Weapon>();   //so we get the weapon adn than grab the Weapon.cs script 
            
            string weaponName = (weapon != null) ? weapon.weaponName : "Unknown";    //if we find weapon script use that name if we cant use unkown
            
            string interactKey = KeybindManager.Instance.GetKeyName("Interact"); // or whatever you called it
            MysteryBoxText.text = $"Press [{interactKey}] to pick up: {weaponName}";

            MysteryBoxText.gameObject.SetActive(true);
        }
        else
        {
            MysteryBoxText.gameObject.SetActive(false);   //if either variabel is not true we keep it false at all times.
        }


        // ------------------------------------------------------------------ Ammo Box UI

        if (ammoBox != null && ammoBoxText != null)
        {
            float distanceToAmmo = Vector3.Distance(player.position, ammoBox.transform.position);

            if (distanceToAmmo <= ammoBox.interactDistance)
            {
                currentWeapon = WeaponManager.ActiveWeapon;

                if (currentWeapon != null)
                {
                    bool clipFull = currentWeapon.GetCurrentAmmo() == currentWeapon.clipSize;
                    bool reserveFull = currentWeapon.GetAmmoReserve() == currentWeapon.maxReserve;

                    if (clipFull && reserveFull)
                    {
                        ammoBoxText.text = "Ammo is Full";
                    }
                    else
                    {
                        string interactKey = KeybindManager.Instance.GetKeyName("Interact");
                        ammoBoxText.text = $"Press [{interactKey}] to Refill Ammo";

                    }

                    ammoBoxText.gameObject.SetActive(true);
                }
                else
                {
                    ammoBoxText.gameObject.SetActive(false);
                }
            }
            else
            {
                ammoBoxText.gameObject.SetActive(false);
            }

        }

        // ------------------------------------------------------------------ Magic Station UI

        //// Handle Normal Fire Station
        //if (fireStation != null && fireMagicText != null)
        //{
        //    float distanceToFireStation = Vector3.Distance(player.position, fireStation.transform.position);   //calc how close the player adn station

        //    if (distanceToFireStation <= fireStation.interactionRange)    //is it less than or equal the distanec ( we in range ?) 
        //    {
        //        if (magicManager != null)  // Using direct reference instead of Instance
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Normal)
        //            {
        //                fireMagicText.text = "Normal Fireball Equipped";
        //            }
        //            else
        //            {
        //                fireMagicText.text = "Press [E] to Equip Normal Fireball";
        //            }

        //            fireMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            fireMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        fireMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle Sulfuric Fire Station
        //if (crystalStation != null && crystalMagicText != null)
        //{
        //    float distanceToCrystalStation = Vector3.Distance(player.position, crystalStation.transform.position);

        //    if (distanceToCrystalStation <= crystalStation.interactionRange)
        //    {
        //        if (magicManager != null)  // Using direct reference instead of Instance
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Crystal)
        //            {
        //                crystalMagicText.text = "Crystal Magic Equipped";
        //            }
        //            else
        //            {
        //                crystalMagicText.text = "Press [E] to Equip Crystal Magic";
        //            }

        //            crystalMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            crystalMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        crystalMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle void magic
        //if (VoidMagicStation != null && VoidMagicText != null)
        //{
        //    float distanceToVoidMagicStation = Vector3.Distance(player.position, VoidMagicStation.transform.position);

        //    if (distanceToVoidMagicStation <= VoidMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Void)
        //            {
        //                VoidMagicText.text = "Void Magic Equipped";
        //            }
        //            else
        //            {
        //                VoidMagicText.text = "Press [E] to Equip Void Magic";
        //            }

        //            VoidMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            VoidMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        VoidMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle ice magic
        //if (IceMagicStation != null && IceMagicText != null)
        //{
        //    float distanceToIceMagicStation = Vector3.Distance(player.position, IceMagicStation.transform.position);

        //    if (distanceToIceMagicStation <= IceMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Ice)
        //            {
        //                IceMagicText.text = "Ice Magic Equipped";
        //            }
        //            else
        //            {
        //                IceMagicText.text = "Press [E] to Equip Ice Magic";
        //            }

        //            IceMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            IceMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        IceMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle Venom magic
        //if (VenomMagicStation != null && VenomMagicText != null)
        //{
        //    float distanceToVenomMagicStation = Vector3.Distance(player.position, VenomMagicStation.transform.position);

        //    if (distanceToVenomMagicStation <= VenomMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Venom)
        //            {
        //                VenomMagicText.text = "Venom Magic Equipped";
        //            }
        //            else
        //            {
        //                VenomMagicText.text = "Press [E] to Equip Venom Magic";
        //            }

        //            VenomMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            VenomMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        VenomMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle Lightining magic
        //if (LightningMagicStation != null && LightningMagicText != null)
        //{
        //    float distanceToLightningMagicStation = Vector3.Distance(player.position, LightningMagicStation.transform.position);

        //    if (distanceToLightningMagicStation <= LightningMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Lightning)
        //            {
        //                LightningMagicText.text = "Lightning Magic Equipped";
        //            }
        //            else
        //            {
        //                LightningMagicText.text = "Press [E] to Equip Lightning Magic";
        //            }

        //            LightningMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            LightningMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        LightningMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle Wind magic
        //if (WindMagicStation != null && WindMagicText != null)
        //{
        //    float distanceToWindMagicStation = Vector3.Distance(player.position, WindMagicStation.transform.position);

        //    if (distanceToWindMagicStation <= WindMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Wind)
        //            {
        //                WindMagicText.text = "Wind Magic Equipped";
        //            }
        //            else
        //            {
        //                WindMagicText.text = "Press [E] to Equip Wind Magic";
        //            }

        //            WindMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            WindMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        WindMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle Meteor magic
        //if (MeteorMagicStation != null && MeteorMagicText != null)
        //{
        //    float distanceToMeteorMagicStation = Vector3.Distance(player.position, MeteorMagicStation.transform.position);

        //    if (distanceToMeteorMagicStation <= MeteorMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Meteor)
        //            {
        //                MeteorMagicText.text = "Meteor Magic Equipped";
        //            }
        //            else
        //            {
        //                MeteorMagicText.text = "Press [E] to Equip Meteor Magic";
        //            }

        //            MeteorMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            MeteorMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        MeteorMagicText.gameObject.SetActive(false);
        //    }
        //}

        //// Handle Crimson magic
        //if (CrimsonMagicStation != null && CrimsonMagicText != null)
        //{
        //    float distanceToCrimsonMagicStation = Vector3.Distance(player.position, CrimsonMagicStation.transform.position);

        //    if (distanceToCrimsonMagicStation <= CrimsonMagicStation.interactionRange)
        //    {
        //        if (magicManager != null)
        //        {
        //            MagicType currentMagic = magicManager.GetCurrentMagicType();

        //            if (currentMagic == MagicType.Crimson)
        //            {
        //                CrimsonMagicText.text = "Crimson Magic Equipped";
        //            }
        //            else
        //            {
        //                CrimsonMagicText.text = "Press [E] to Equip Crimson Magic";
        //            }

        //            CrimsonMagicText.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            CrimsonMagicText.gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        CrimsonMagicText.gameObject.SetActive(false);
        //    }
        //}

        UpdateMagicPrompts();

        //--------------------------------------------------Magic Cooldown

        // --- Magic Cooldown UI ---
        if (magicManager != null && magicCooldownSlider != null)
        {
            magicCooldownSlider.value = magicManager.GetCooldownProgress01(); // 0..1
        }


        //--------------------------------------------------------------- Round system UI

        if (zombieSpawner != null)
        {
            roundText.text = "" + zombieSpawner.GetCurrentRound();
        }

        //--------------------------------------------------------------- KineticSlamCooldown UI

        if (playerMovement != null)
        {
            float slamTimePassed = Time.time - playerMovement.LastSlamTime;   //Time.time is the current time in seconds since the game started. we sub lastslameTime top calc how much time has passed
            float slamProgress = Mathf.Clamp01(slamTimePassed / playerMovement.slamCooldown);  //calmp01 makes it between 0 and 1 so when we get more than 1 cooldwon is done. 
                                                                                               //      slamTimePassed = 5, slamCooldown = 10
                                                                                               //       → 5 / 10 = 0.5
                                                                                               //       → So you're halfway through the cooldown.
            slamCooldownSlider.value = slamProgress;  //we have range from 0 to 1.
        }

        //--------------------------------------------------------------- health uI
        if (PointManager.Instance != null && pointsText != null)
        {
            pointsText.text = "" + PointManager.Instance.points;
        }

        //------grenade logic

        HandleGrenadeChestUI();

        if (grenadeManager != null && grenadeAmountText != null)
        {
            var t = grenadeManager.currentType;
            int have = grenadeManager.GetCount(t);
            int cap = grenadeManager.GetCap(t);
            grenadeAmountText.text = have.ToString();      //have + " / " + cap;   // e.g. "4 / 6"  we took this out 
        }
        // Example for 4 perks:

        HandlePerkStationUI(
            FindFirstObjectByType<MoreFireRatePerk>(),
            fireRatePerkText,
            "Faster Fire Rate",
            FindFirstObjectByType<MoreFireRatePerk>() != null && FindFirstObjectByType<MoreFireRatePerk>().hasMoreFireRatePerk,
            FindFirstObjectByType<MoreFireRatePerk>() != null ? FindFirstObjectByType<MoreFireRatePerk>().cost : 0
        );

        HandlePerkStationUI(
            FindFirstObjectByType<MoreSpeedPerk>(),
            speedPerkText,
            "More Running Speed and Slide Speed",
            FindFirstObjectByType<MoreSpeedPerk>() != null && FindFirstObjectByType<MoreSpeedPerk>().hasMoreSpeedPerk,
            FindFirstObjectByType<MoreSpeedPerk>() != null ? FindFirstObjectByType<MoreSpeedPerk>().cost : 0
        );

        HandlePerkStationUI(
            FindFirstObjectByType<FastHandsPerk>(),
            fastHandsPerkText,
            "Faster Reload",
            FindFirstObjectByType<FastHandsPerk>() != null && FindFirstObjectByType<FastHandsPerk>().hasFastHandsPerk,
            FindFirstObjectByType<FastHandsPerk>() != null ? FindFirstObjectByType<FastHandsPerk>().cost : 0
        );

        HandlePerkStationUI(
            FindFirstObjectByType<MoreHealthPerk>(),
            healthPerkText,
            "More Health",
            FindFirstObjectByType<MoreHealthPerk>() != null && FindFirstObjectByType<MoreHealthPerk>().hasMoreHealthPerk,
            FindFirstObjectByType<MoreHealthPerk>() != null ? FindFirstObjectByType<MoreHealthPerk>().cost : 0
        );

        HandlePerkStationUI(
            FindFirstObjectByType<MoreRevivePerk>(),
            revivePerkText,
            "Fast Revive",
            FindFirstObjectByType<MoreRevivePerk>() != null && FindFirstObjectByType<MoreRevivePerk>().hasMoreRevivePerk,
            FindFirstObjectByType<MoreRevivePerk>() != null ? FindFirstObjectByType<MoreRevivePerk>().cost : 0
        );
        HandlePerkStationUI(
            FindFirstObjectByType<MagicCooldownPerk>(),
            magicCooldownPerkText,
            "Faster Magic Cooldown",
            FindFirstObjectByType<MagicCooldownPerk>()?.hasMagicCooldownPerk ?? false,
            FindFirstObjectByType<MagicCooldownPerk>()?.cost ?? 0
        );
        HandlePerkStationUI(
            FindFirstObjectByType<MoreDropsPerk>(),
            moreDropsPerkText,
            "Increase Drop Chance",
            FindFirstObjectByType<MoreDropsPerk>()?.hasMoreDropsPerk ?? false,
            FindFirstObjectByType<MoreDropsPerk>()?.cost ?? 0
        );
        HandlePerkStationUI(
            FindFirstObjectByType<AdrenalinePerk>(),
            adrenalinePerkText,
            "Adrenaline (speed boost before death)",
            FindFirstObjectByType<AdrenalinePerk>()?.hasAdrenalinePerk ?? false,
            FindFirstObjectByType<AdrenalinePerk>()?.cost ?? 0
        );

        HandlePerkStationUI(
            FindFirstObjectByType<LessAmmoConsumePerk>(),
            lessAmmoConsumePerkText,
            "15% Chance to not consume ammunition",
            FindFirstObjectByType<LessAmmoConsumePerk>()?.hasLessAmmoPerk ?? false,
            FindFirstObjectByType<LessAmmoConsumePerk>()?.cost ?? 0
        );

        HandlePerkStationUI(
            FindFirstObjectByType<MoreDashSlamPerk>(),
            moreDashSlamPerkText,
            "Dash further, faster, more often and Faster Kinetic Jump window",
            FindFirstObjectByType<MoreDashSlamPerk>()?.hasMoreDashSlamPerk ?? false,
            FindFirstObjectByType<MoreDashSlamPerk>()?.cost ?? 0
        );
        
        HandlePerkStationUI(
            FindFirstObjectByType<ResurrectPerk>(),
            resurrectPerkText,
            "Come back from the Dead",
            FindFirstObjectByType<ResurrectPerk>()?.hasResurrectPerk ?? false,
            FindFirstObjectByType<ResurrectPerk>()?.cost ?? 0
        );


        if (packAPunchMachine != null && packAPunchText != null)
        {
            string promptText = packAPunchMachine.GetPromptText();

            if (string.IsNullOrEmpty(promptText))
            {
                packAPunchText.gameObject.SetActive(false);
            }
            else
            {
                packAPunchText.text = promptText;
                packAPunchText.gameObject.SetActive(true);
            }
        }

        HandleInfuseStationUI();

        // --- Dash Cooldown UI ---
        if (playerMovement != null && dashCooldownSlider != null)
        {
            float dashTimePassed = Time.time - playerMovement.LastDashTime;
            float dashProgress = Mathf.Clamp01(dashTimePassed / playerMovement.dashCooldown);
            dashCooldownSlider.value = dashProgress;

            //// optional: color swap when ready vs recharging
            //if (dashFill != null)
            //{
            //    dashFill.color = (dashProgress >= 1f) ? dashReadyColor : dashRechargingColor;

            //}
        }
        // --- Kinetic Jump HUD hint ---
        if (playerMovement != null && kineticJumpHintText != null)
        {
            bool canJump = playerMovement.CanKineticJumpNow;

            if (canJump)
            {
                // If we just became allowed, set the delay timer
                if (kineticHintReadyTime < 0f)
                {
                    kineticHintReadyTime = Time.time + 0.05f; // wait 0.05s before showing (THIS IS ALL JUST BECAUSE OF THE FLICKER BUG THE TEXT KEEPS APPEARING WHEN U JUMP PREMAUTRALY)
                }

                // Show text only if delay has passed
                if (Time.time >= kineticHintReadyTime)
                {
                    if (!kineticJumpHintText.gameObject.activeSelf)
                    {
                        kineticJumpHintText.gameObject.SetActive(true);
                    }
                    string jumpKey = KeybindManager.Instance.GetKeyName("Jump&Slam");
                    kineticJumpHintText.text = $"Press [{jumpKey}] to Kinetic Jump";
                }
            }
            else
            {
                // Reset when not allowed anymore
                kineticHintReadyTime = -999f;

                if (kineticJumpHintText.gameObject.activeSelf)
                    kineticJumpHintText.gameObject.SetActive(false);
            }
        }

        // --- Kinetic Jump buildup while sliding (the slider) ---

        if (playerMovement != null && kineticJumpCooldownSlider != null)
        {
            bool showSliderBar = playerMovement.IsSliding() && playerMovement.GetSlideTimer() < playerMovement.minSlideTimeForKineticJump;

            // show/hide only when it changes (avoids SetActive spam)
            if (kineticJumpCooldownSlider.gameObject.activeSelf != showSliderBar)
            {
                kineticJumpCooldownSlider.gameObject.SetActive(showSliderBar);
            }
            if (showSliderBar)
            {
                kineticJumpCooldownSlider.value = playerMovement.GetKJProgress01WhileSliding();
            }
            else
            {
                // reset when hidden so it starts empty next slide
                kineticJumpCooldownSlider.value = 0f;
            }
        }


        // --- Airborne Slam HUD hint ---
        if (playerMovement != null && slamHintText != null)
        {
            if (playerMovement.CanSlamNow)
            {
                if (!slamHintText.gameObject.activeSelf)
                {
                    slamHintText.gameObject.SetActive(true);
                }

                string slamKey = KeybindManager.Instance.GetKeyName("Jump&Slam");
                slamHintText.text = $"Press [{slamKey}] to SLAM";
                // ensure fully opaque if you’ve got any fades elsewhere
                // var c = slamHintText.color; slamHintText.color = new Color(c.r, c.g, c.b, 1f);
            }
            else if (slamHintText.gameObject.activeSelf)
            {
                slamHintText.gameObject.SetActive(false);
            }
        }

        //dash VFX to enabel chromatic abbriation adn DepthOffeidl for blur 
        if (playerMovement != null)
        {
            if (playerMovement.IsDashing())
            {
                if (chromaticAberration != null)
                    chromaticAberration.active = true;

                if (depthOfFeild != null)
                    depthOfFeild.active = true;
            }
            else
            {
                if (chromaticAberration != null)
                    chromaticAberration.active = false;

                if (depthOfFeild != null)
                    depthOfFeild.active = false;
            }
        }

        // --- Dash Cooldown Status ---  WE CAN COME BACK TO THIS 
        //if (playerMovement != null && dashCooldownSlider != null && DashNotReadyText != null)
        //{
        //    float dashTimePassed = Time.time - playerMovement.LastDashTime;
        //    float dashProgress = Mathf.Clamp01(dashTimePassed / playerMovement.dashCooldown);
        //    dashCooldownSlider.value = dashProgress;

        //    bool onCooldown = dashProgress < 1f;
        //    DashNotReadyText.gameObject.SetActive(onCooldown);
        //    DashNotReadyText.text = onCooldown ? "Dash on Cooldown" : "";
        //}


    }

    //------------------------------------ round UI logic
    public void ShowRoundCountdown(float countdownSeconds)
    {
        StartCoroutine(RoundCountdownRoutine(countdownSeconds));
    }

    private IEnumerator RoundCountdownRoutine(float countdownSeconds)
    {
        int currentRound = zombieSpawner.GetCurrentRound() - 1; // previous round just ended

        // Show "Wave X completed"
        roundCountdownText.text = $"Wave {currentRound} completed!";
        roundCountdownText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);

        // Countdown
        float t = countdownSeconds;
        while (t > 0)
        {
            roundCountdownText.text = $"Next wave begins in {Mathf.CeilToInt(t)}...";
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }

        // Hide after countdown
        roundCountdownText.gameObject.SetActive(false);
    }


    //------------------------------------ grenade logic
    void HandleGrenadeChestUI()
    {
        //if (grenadePanel == null || grenadePrompt == null || player == null || grenadeChest == null)
        //    return;  

        // if panel is open, force-hide the prompt and listen for close keys
        if (grenadePanelOpen)
        {
            //if (grenadePrompt.gameObject.activeSelf)  
            //    grenadePrompt.gameObject.SetActive(false);

            if (KeybindManager.Instance.GetKeyDown("BackOutInteract") || KeybindManager.Instance.GetKeyDown("Interact"))
                CloseGrenadePanel();
            return;
        }

        bool inRange = Vector3.Distance(player.position, grenadeChest.transform.position) <= grenadeChest.interactDistance;        //return true if we are in distance

        grenadePrompt.gameObject.SetActive(inRange);  //set active based on teh range so it wil be true if were in range bc its a bool
        string interactKey = KeybindManager.Instance.GetKeyName("Interact");
        if (inRange)
        {
            // Update this line to match your scene text:
            grenadePrompt.text = $"Press [{interactKey}] to Open Grenade Chest and Swap Lethals";
        }
        if (inRange && KeybindManager.Instance.GetKeyDown("Interact"))
        {
            OpenGrenadePanel();
        }
    }

    void OpenGrenadePanel()
    {
        grenadePanelOpen = true;        
        grenadePanel.SetActive(true);
        grenadePrompt.gameObject.SetActive(false);  // hide prompt

        Cursor.lockState = CursorLockMode.None;         //turn the mouse on so we cam actauly select the grenade panel 
        Cursor.visible = true;

        var cam = FindFirstObjectByType<CameraScript>();  //fethc cam script
        if (cam) cam.cameraLocked = true;    //were disablign movment by setting this varibale as true so we can move around in teh menu -- look at cam script we put this eveyrwhere

        if (statusStackRoot) statusStackRoot.gameObject.SetActive(false);   //so you dont see drops when your in the menu
    }

    public bool IsGrenadePanelOpen => grenadePanelOpen;   //this is for in weapon so we can not shoot when panel is open  -- getter

    void CloseGrenadePanel()
    {
        grenadePanelOpen = false;           //we turn eveyrhting off
        grenadePanel.SetActive(false);
        grenadePrompt.gameObject.SetActive(false);  // still hide after close

        Cursor.lockState = CursorLockMode.Locked;    //return back to normal ingame mouse
        Cursor.visible = false;

        var cam = FindFirstObjectByType<CameraScript>();        //fethc cam script
        if (cam) cam.cameraLocked = false;   //were enable cam movment by setting this varibale as false so we can move around in game with camera -- look at cam script we put this eveyrwhere

        if (statusStackRoot) statusStackRoot.gameObject.SetActive(true);  //so you can see drops if active

    }

    //--- Grenade type Dictionary setting
    public void OnPickFrag()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();  //fetch grenadeManager Script
        if (gm) gm.SetType(GrenadeType.Frag);                //set the key 
        CloseGrenadePanel();                                //close panel
    }

    public void OnPickImpact()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.Impact);
        CloseGrenadePanel();
    }

    public void OnPickSemtex()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.Semtex);
        CloseGrenadePanel();
    }

    public void OnPickBio()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.Bio);
        CloseGrenadePanel();
    }

    public void OnPickSulfuricNapalm()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.SulfuricNapalm);
        CloseGrenadePanel();
    }

    public void OnPickCrystalCluster()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.CrystalCluster);
        CloseGrenadePanel();
    }

    public void OnPickBastion()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.Bastion);
        CloseGrenadePanel();
    }

    public void OnPickRagnarok()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.Ragnarok);
        CloseGrenadePanel();
    }
    public void OnPickSpider()
    {
        var gm = FindFirstObjectByType<GrenadeManager>();
        if (gm) gm.SetType(GrenadeType.Spider);
        CloseGrenadePanel();
    }

    public void ShowTemporaryGrenadeMessage(string message)
    {
        if (grenadeStatusText == null) return;              //if you alerady have the text GTFO this code
        if (grenadeMsgCo != null) StopCoroutine(grenadeMsgCo);      
        grenadeMsgCo = StartCoroutine(GrenadeMsgRoutine(message));  //start the message for a quick sec
    }

    private IEnumerator GrenadeMsgRoutine(string message)
    {
        grenadeStatusText.text = message;           //we pu rmessage into string of what we want to say 
        grenadeStatusText.gameObject.SetActive(true);       //set it true
        yield return new WaitForSeconds(1.2f);              //show for only this many seconds
        grenadeStatusText.gameObject.SetActive(false);          //turn it off
        grenadeMsgCo = null;
    }

    public void GetGrenadeSpriteForSlots(GrenadeType type)   // this is for the logo
    {
        if (grenadeSlotIcon == null) return;

        switch (type)
        {
            case GrenadeType.Frag: grenadeSlotIcon.sprite = fragIcon; break;
            case GrenadeType.Impact: grenadeSlotIcon.sprite = impactIcon; break;
            case GrenadeType.Semtex: grenadeSlotIcon.sprite = semtexIcon; break;
            case GrenadeType.Bio: grenadeSlotIcon.sprite = bioIcon; break;
            case GrenadeType.SulfuricNapalm: grenadeSlotIcon.sprite = sulfuricNapalmIcon; break;
            case GrenadeType.CrystalCluster: grenadeSlotIcon.sprite = crystalClusterIcon; break;
            case GrenadeType.Bastion: grenadeSlotIcon.sprite = bastionIcon; break;
            case GrenadeType.Ragnarok: grenadeSlotIcon.sprite = ragnarokIcon; break;
            case GrenadeType.Spider: grenadeSlotIcon.sprite = spiderIcon; break;

            default: grenadeSlotIcon.sprite = fragIcon; break;
        }
    }


    //-------------------------MAGIC functions

    //---MAGIC prompt text
    void UpdateMagicPrompts()
    {
        if (magicStations == null || player == null || magicManager == null) return;  //get out if we dont have one of these

        int points = (PointManager.Instance != null) ? PointManager.Instance.GetPoints() : 0;    //grab the player’s current points once - if dont have points assume its 0

        foreach (var e in magicStations) //loop through all stations 
        {
            if (e == null || e.station == null || e.text == null) continue;   //skip any incomplete entry so we don’t crash

            float dist = Vector3.Distance(player.position, e.station.transform.position);  //find position from station to player
            bool inRange = dist <= e.station.interactionRange;  //in range bc we see dist and check if were within the interaction range

            if (!inRange)
            {
                e.text.gameObject.SetActive(false);         //turn off text when we aint in range
                continue;
            }

            e.text.gameObject.SetActive(true);      //show the prompt, then decide what it should say

            bool equipped = (magicManager.GetCurrentMagicType() == e.type); //if already equipped
            if (equipped)
            {
                e.text.text = $"{e.label} Magic Equipped";
            }
            else
            {
                int cost = e.station.cost;          //if statement to see if you can afford
                if (points < cost)
                {
                    e.text.text = $"Need {cost} Points for {e.label} Magic";
                }
                else
                {
                    string interactKey = KeybindManager.Instance.GetKeyName("Interact");
                    e.text.text = $"Press [{interactKey}] to buy {e.label} Magic ({cost} Points)";
                }

            }
        }
    }

    public void ShowTemporaryMagicMessage(string message)
    {
        StopAllCoroutines(); // cancel any old message timers
        StartCoroutine(ShowMagicMessageRoutine(message));
    }

    private IEnumerator ShowMagicMessageRoutine(string message)
    {
        magicStatusText.text = message;
        magicStatusText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f); // how long to show

        magicStatusText.gameObject.SetActive(false);
    }
    //--- Magic slot bottom right image 
    public void UpdateMagicSlot(MagicType type)
    {
        if (magicSlotIcon == null) return; // for this the code leaves 

        // reset sprite
        magicSlotIcon.sprite = GetMagicSpriteForSlots(type);
        //magicSlotIcon.enabled = (magicSlotIcon.sprite != null);
         
        // disable all VFX 
        if (fireMagicVFX) fireMagicVFX.SetActive(false);
        if (crystalMagicVFX) crystalMagicVFX.SetActive(false);
        if (voidMagicVFX) voidMagicVFX.SetActive(false);
        if (iceMagicVFX) iceMagicVFX.SetActive(false);
        if (venomMagicVFX) venomMagicVFX.SetActive(false);
        if (lightningMagicVFX) lightningMagicVFX.SetActive(false);
        if (windMagicVFX) windMagicVFX.SetActive(false);
        if (meteorMagicVFX) meteorMagicVFX.SetActive(false);
        if (crimsonMagicVFX) crimsonMagicVFX.SetActive(false);

        // enable the one that matches the sprite
        switch (type)
        {
            case MagicType.Normal: if (fireMagicVFX) fireMagicVFX.SetActive(true); break;
            case MagicType.Crystal: if (crystalMagicVFX) crystalMagicVFX.SetActive(true); break;
            case MagicType.Void: if (voidMagicVFX) voidMagicVFX.SetActive(true); break;
            case MagicType.Ice: if (iceMagicVFX) iceMagicVFX.SetActive(true); break;
            case MagicType.Venom: if (venomMagicVFX) venomMagicVFX.SetActive(true); break;
            case MagicType.Lightning: if (lightningMagicVFX) lightningMagicVFX.SetActive(true); break;
            case MagicType.Wind: if (windMagicVFX) windMagicVFX.SetActive(true); break;
            case MagicType.Meteor: if (meteorMagicVFX) meteorMagicVFX.SetActive(true); break;
            case MagicType.Crimson: if (crimsonMagicVFX) crimsonMagicVFX.SetActive(true); break;
        }

        // keep your rotation logic
        if (magicSlotIcon.enabled)
        {
            magicSlotIcon.rectTransform.localRotation = Quaternion.identity;
            magicSlotIcon.rectTransform.Rotate(0f, 0f, 45f);
        }

        SetMagicCooldownColor(type);  //this is to change color of the fill bar
    }

    private void SetMagicCooldownColor(MagicType type)
    {
        if (magicCooldownFill == null) return;

        // keep whatever alpha the fill currently has
        float a = magicCooldownFill.color.a;
        Color c = normalColor;

        switch (type)
        {
            case MagicType.Normal: c = normalColor; break;
            case MagicType.Crystal: c = crystalColor; break;
            case MagicType.Void: c = voidColor; break;
            case MagicType.Ice: c = iceColor; break;
            case MagicType.Venom: c = venomColor; break;
            case MagicType.Lightning: c = lightningColor; break;
            case MagicType.Wind: c = windColor; break;
            case MagicType.Meteor: c = meteorColor; break;
            case MagicType.Crimson: c = crimsonColor; break;
        }

        magicCooldownFill.color = new Color(c.r, c.g, c.b, a);
    }


    private Sprite GetMagicSpriteForSlots(MagicType type)
    {
        switch (type)
        {
            case MagicType.Normal: return normalIcon;
            case MagicType.Crystal: return crystalIcon;
            case MagicType.Void: return voidIcon;
            case MagicType.Ice: return iceIcon;
            case MagicType.Venom: return venomIcon;
            case MagicType.Lightning: return lightningIcon;
            case MagicType.Wind: return windIcon;
            case MagicType.Meteor: return meteorIcon;
            case MagicType.Crimson: return crimsonIcon;
            default: return noneIcon;
        }
    }

    //this is for magic infusion to change color 
    private Color GetColorForInfusion(InfusionType type)
    {
        switch (type)
        {
            case InfusionType.Fire: return normalColor;
            case InfusionType.Crystal: return crystalColor;
            case InfusionType.Void: return voidColor;
            case InfusionType.Ice: return iceColor;
            case InfusionType.Venom: return venomColor;
            case InfusionType.Lightning: return lightningColor;
            case InfusionType.Wind: return windColor;
            case InfusionType.Meteor: return meteorColor;
            case InfusionType.Crimson: return crimsonColor;
            default: return Color.white;
        }
    }


    //-------------------------health functions
    public void UpdateHealthBar(float value)
    {
        playerHealthSlider.value = value;
    }

    //-------------------------- perk ICON logic
    public void ShowPerkIcon(PerkType type)
    {
        if (perkBar == null || perkIconPrefab == null) return;          
        if (_activePerkIcons.ContainsKey(type)) return;                 

        Sprite s = GetPerkSprite(type);
        if (s == null) return;

        Image img = Instantiate(perkIconPrefab, perkBar);
        img.sprite = s;
        img.SetNativeSize();                                            // optional
                                                                        // optional: clamp size
        var rt = img.rectTransform;
        rt.sizeDelta = new Vector2(30, 30);

        // pop-in anim
        StartCoroutine(PopIn(img));

        _activePerkIcons[type] = img;
        _perkOrder.Add(type);                                           // chronological order
    }

    public void RemovePerkIcon(PerkType type)
    {
        if (_activePerkIcons.TryGetValue(type, out var img) && img != null)
        {
            Destroy(img.gameObject);
        }
        _activePerkIcons.Remove(type);
        _perkOrder.Remove(type);
    }

    private Sprite GetPerkSprite(PerkType type)
    {
        switch (type)
        {
            case PerkType.Speed: return speedIcon;
            case PerkType.Health: return healthIcon;
            case PerkType.Revive: return reviveIcon;
            case PerkType.FireRate: return fireRateIcon;
            case PerkType.FastHands: return fastHandsIcon;
            case PerkType.MagicCooldown: return magicCooldownIcon;
            case PerkType.MoreDrop: return moreDropsIcon;
            case PerkType.Adrenaline: return adrenalineIcon;
            case PerkType.LessAmmoConsume: return lessAmmoConsumeIcon;
            case PerkType.MoreDashSlam: return moreDashSlamIcon;
            case PerkType.Resurrect: return resurrectIcon;


            default: return null;
        }
    }

    private IEnumerator PopIn(Image img)
    {
        if (img == null) yield break;
        CanvasGroup cg = img.GetComponent<CanvasGroup>();
        if (cg == null) cg = img.gameObject.AddComponent<CanvasGroup>();

        float t = 0f, dur = 0.2f;
        cg.alpha = 0f;
        img.rectTransform.localScale = Vector3.one * 0.6f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            float s = Mathf.Lerp(0.6f, 1f, t);
            img.rectTransform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // this is all to increase transparency so its not see thru
        cg.alpha = 1f;
        img.canvasRenderer.SetAlpha(1f);
        var c = img.color; img.color = new Color(c.r, c.g, c.b, 1f);
    }

    //----- Perk UI
    void HandlePerkStationUI(MonoBehaviour perkObj, UnityEngine.UI.Text uiText, string perkName, bool hasPerk, int cost)
    {
        if (uiText == null) return;             //if we dont have the text get out

        if (perkObj == null || player == null)      //capturte script and the player
        {
            uiText.gameObject.SetActive(false);     //turn off text ui if no script or player 
            return;
        }

        const float InteractionRange = 3f;     //determine the distance (interationRange)
            
        float dist = Vector3.Distance(player.position, perkObj.transform.position);  //find distance between player and the player
        if (dist > InteractionRange)        //if your not in range than make text ui false
        {
            uiText.gameObject.SetActive(false);
            return;
        }

        uiText.gameObject.SetActive(true);             //if you are than showcase that text 

        if (hasPerk)
        {
            uiText.text = $"{perkName} Perk Owned";     //if you already have that perk than show
            return;
        }

        PointManager pm = FindFirstObjectByType<PointManager>();
        int points = (pm != null) ? pm.GetPoints() : 0;   //get the points if you dont have a script assume its 0 

        if (points < cost)
        {
            uiText.text = $"Need {cost} pts for {perkName} Perk";
        }
        else
        {
            string interactKey = KeybindManager.Instance.GetKeyName("Interact");
            uiText.text = $"Press [{interactKey}] to buy {perkName} Perk ({cost} pts)";
        }
    }


    public void ShowTemporaryPerkMessage(string message)
    {
        StartCoroutine(ShowPerkMessageRoutine(message));
    }

    private IEnumerator ShowPerkMessageRoutine(string message)
    {
        Text t = (NoMoneyStatusText != null) ? NoMoneyStatusText : magicStatusText;
        if (t == null) yield break;

        t.text = message;
        t.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        t.gameObject.SetActive(false);
    }
    //-------------------- Enemy Kill Feed text 

    public void ShowEliminationMessage(int awardedPoints, string enemyName = "Zombie")
    {
        if (eliminationStackRoot == null || eliminationRowPrefab == null) return;  //gtfo this code if we dont have

        int UpdatedAwardedPoints = Mathf.RoundToInt(awardedPoints * PointManager.GlobalPointsMult);

        if (_activeEliminationRows.Count >= maxEliminationRows) 
        {
            Text oldest = _activeEliminationRows[0];  //oldest one 
            
            if (oldest != null)
            {
                Destroy(oldest.gameObject);
            }
            
            _activeEliminationRows.RemoveAt(0);
        }

        var row = Instantiate(eliminationRowPrefab, eliminationStackRoot);  //spawn here and what to spawn 
        row.text = $"+{UpdatedAwardedPoints} {enemyName} Eliminated";                 // what the text says

        _activeEliminationRows.Add(row);
        StartCoroutine(PopInAndFadeOut(row));
    }

    private IEnumerator PopInAndFadeOut(Text row)
    {
        if (row == null) yield break;

        // Ensure CanvasGroup is present
        CanvasGroup cg = row.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = row.gameObject.AddComponent<CanvasGroup>();  //get canvasGROUP
        }

        // Initial state
        cg.alpha = 0f;  //set to 0 so you cant see 
        row.rectTransform.localScale = Vector3.one * 0.6f; 

        // --- POP IN ---
        float timer = 0f; //start the timer at 0
        float popDuration = 0.25f;

        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime / popDuration;
            cg.alpha = Mathf.Lerp(0f, 1f, timer);          // transparency 
            float s = Mathf.Lerp(0.6f, 1f, timer);
            row.rectTransform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // Hold for a moment
        yield return new WaitForSecondsRealtime(1.75f);

        // --- FADE OUT ---
        timer = 0f;
        float fadeDuration = 0.15f;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime / fadeDuration;
            cg.alpha = Mathf.Lerp(1f, 0f, timer);
            yield return null;
        }

        //yield return new WaitForSecondsRealtime(0.1f);

        Destroy(row.gameObject);
    }




    // ---------------- Drops text
    public void ShowToast(string message, float seconds = 1.5f)
    {
        // fallback to your old single text if stack not wired yet
        if (statusStackRoot == null || statusRowPrefab == null)
        {
            ShowTemporaryMagicMessage(message);
            return;
        }

        var row = Instantiate(statusRowPrefab, statusStackRoot);
        row.text = message;
        StartCoroutine(DestroyRowAfter(row, seconds));
    }

    private System.Collections.IEnumerator DestroyRowAfter(UnityEngine.UI.Text row, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (row != null) Destroy(row.gameObject);
    }

    // Timed badge that updates every frame (e.g., "INSTA-KILL (25s)"), one row per key.
    // If you call again with same key, it refreshes the same row instead of adding a duplicate.
    public void ShowTimedPowerup(string key, string label, float durationSeconds)
    {
        if (statusStackRoot == null || statusRowPrefab == null)
        {
            ShowTemporaryMagicMessage(label);
            return;
        }

        if (_timedCoroutines.TryGetValue(key, out var co) && co != null)
            StopCoroutine(co);

        if (!_timedRows.TryGetValue(key, out var row) || row == null)
        {
            row = Instantiate(statusRowPrefab, statusStackRoot);
            _timedRows[key] = row;
        }

        _timedCoroutines[key] = StartCoroutine(TimedPowerupRoutine(key, row, label, durationSeconds));
    }

    private System.Collections.IEnumerator TimedPowerupRoutine(string key, UnityEngine.UI.Text row, string label, float durationSeconds)
    {
        float end = Time.time + Mathf.Max(0f, durationSeconds);
        row.gameObject.SetActive(true);

        while (Time.time < end)
        {
            int secs = Mathf.CeilToInt(end - Time.time);
            row.text = $"{label} ({secs}s)";
            yield return null;
        }

        // cleanup
        if (_timedCoroutines.ContainsKey(key)) _timedCoroutines.Remove(key);
        if (_timedRows.ContainsKey(key)) _timedRows.Remove(key);
        if (row != null) Destroy(row.gameObject);
    }


    //---------------------------------INFUSING --------------------------- 
    void HandleInfuseStationUI()
    {
        if (infusePanelOpen)
        {
            if (KeybindManager.Instance.GetKeyDown("BackOutInteract") || KeybindManager.Instance.GetKeyDown("Interact"))
                CloseInfusePanel();
            return;
        }

        InfuseStation[] allStations = FindObjectsOfType<InfuseStation>();
        foreach (var station in allStations)
        {
            float dist = Vector3.Distance(player.position, station.transform.position);
            if (dist <= station.interactDistance)
            {
                currentInfuseStation = station;
                string interactKey = KeybindManager.Instance.GetKeyName("Interact");
                infusePromptText.text = $"Press [{interactKey}] to OPEN Infuse Station";
                infusePromptText.gameObject.SetActive(true);

                if (KeybindManager.Instance.GetKeyDown("Interact"))
                    OpenInfusePanel();
                return;
            }
        }

        // Not near any
        infusePromptText.gameObject.SetActive(false);
        currentInfuseStation = null;
    }
    void OpenInfusePanel()
    {
        infusePanelOpen = true;
        infusePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var cam = FindFirstObjectByType<CameraScript>();
        if (cam) cam.cameraLocked = true;

        if (statusStackRoot) statusStackRoot.gameObject.SetActive(false);
    }

    public bool IsInfusePanelOpen => infusePanelOpen;


    void CloseInfusePanel()
    {
        infusePanelOpen = false;
        infusePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var cam = FindFirstObjectByType<CameraScript>();
        if (cam) cam.cameraLocked = false;

        if (statusStackRoot) statusStackRoot.gameObject.SetActive(true);
    }

    public void SetCurrentWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
        if (infuseStatusText != null)
        {
            // Check the actual InfusionType enum instead of the string
            if (weapon.infusion != InfusionType.None)
            {
                infuseStatusText.text = $"{weapon.infusion} Magic Infused";
            }
            else if (!string.IsNullOrEmpty(weapon.infusedElement))
            {
                // Fallback to string version if enum is None but string exists
                infuseStatusText.text = $"{weapon.infusedElement} XXX ERROR Magic Infused!";
            }
            else
            {
                infuseStatusText.text = "";
            }
        }
    }

    //------------------------ INFUSE UI
    public void OnClickInfuseFire()
    {
        InfuseWith(InfusionType.Fire);
    }

    public void OnClickInfuseIce()
    {
        InfuseWith(InfusionType.Ice);
    }

    public void OnClickInfuseVenom()
    {
        InfuseWith(InfusionType.Venom);
    }

    public void OnClickInfuseLightning()
    {
        InfuseWith(InfusionType.Lightning);
    }

    public void OnClickInfuseWind()
    {
        InfuseWith(InfusionType.Wind);
    }

    public void OnClickInfuseMeteor()
    {
        InfuseWith(InfusionType.Meteor);
    }

    public void OnClickInfuseCrimson()
    {
        InfuseWith(InfusionType.Crimson);
    }

    public void OnClickInfuseVoid()
    {
        InfuseWith(InfusionType.Void);
    }

    public void OnClickInfuseCrystal()
    {
        InfuseWith(InfusionType.Crystal);
    }

    public void InfuseWith(InfusionType type)
    {
        if (currentWeapon != null)
        {
            // Set both the enum and string for consistency
            currentWeapon.SetInfusion(type);
            currentWeapon.SetInfusedElement(type.ToString());

            if (infuseStatusText != null)
                infuseStatusText.text = $"{type} Magic Infused";
        }
        else
        {
            Debug.LogWarning("No current weapon found to infuse.");
        }
        CloseInfusePanel();
    }

    public void UpdateInfusionFromWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            GetInfusionSpriteForSlots(InfusionType.None);
            return;
        }

        GetInfusionSpriteForSlots(weapon.infusion);  // whichever infusion this weapon has
    }

    public void GetInfusionSpriteForSlots(InfusionType type)
    {
        if (infusionSlotIcon == null) return;

        if (type == InfusionType.None)
        {
            // hide the icon, show just your empty background frame
            infusionSlotIcon.enabled = false;
            return;
        }

        infusionSlotIcon.enabled = true;

        switch (type)
        {
            case InfusionType.Fire: infusionSlotIcon.sprite = infusionFireIcon; break;
            case InfusionType.Crystal: infusionSlotIcon.sprite = infusionCrystalIcon; break;
            case InfusionType.Void: infusionSlotIcon.sprite = infusionVoidIcon; break;
            case InfusionType.Ice: infusionSlotIcon.sprite = infusionIceIcon; break;
            case InfusionType.Venom: infusionSlotIcon.sprite = infusionVenomIcon; break;
            case InfusionType.Lightning: infusionSlotIcon.sprite = infusionLightningIcon; break;
            case InfusionType.Wind: infusionSlotIcon.sprite = infusionWindIcon; break;
            case InfusionType.Meteor: infusionSlotIcon.sprite = infusionMeteorIcon; break;
            case InfusionType.Crimson: infusionSlotIcon.sprite = infusionCrimsonIcon; break;
        }
    }

}



