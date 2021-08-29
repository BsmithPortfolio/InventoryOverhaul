using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySwapper
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class InventorySwapper : BaseUnityPlugin
    {
        private const string ModName = "InventorySwapper";
        internal const string ModVersion = "0.0.6";
        internal const string ModGUID = "com.zarboz.inventoryoverhaul";
        internal static GameObject ContainerGO;
        internal static GameObject InventoryGO;
        internal static GameObject SplitGO;
        internal static GameObject DragItemGO;
        internal static GameObject HudGO;
        private static ConfigEntry<bool> AltInterface;
        private static ConfigEntry<Vector3> InvPos;
        private static ConfigEntry<Vector3> SplitPos;
        private static ConfigEntry<Vector3> ContainerPos;
        private static ConfigEntry<Int32> RowCount;
        public static ConfigEntry<string> hotKey1;
        public static ConfigEntry<string> hotKey2;
        public static ConfigEntry<string> hotKey3;
        public static ConfigEntry<string>[] hotkeys;
        private static ConfigEntry<float> quickAccessX;
        private static ConfigEntry<float> quickAccessY;

        public static GameObject container { get; set; }
        public static GameObject inventory { get; set; }
        public static GameObject splitpanel { get; set; }

        public void Awake()
        {
            AltInterface = Config.Bind("Inventory Interface", "Alt style", false, "Alternate Style");
            InvPos =  Config.Bind("Inventory Interface", "Position of Inventory", new Vector3(0f,0f,0f), new ConfigDescription("Location of Inventory"));
            SplitPos =  Config.Bind("Inventory Interface", "Position of SplitPanel", new Vector3(0f,0f,0f), new ConfigDescription("Location of SplitPanel"));
            ContainerPos =  Config.Bind("Inventory Interface", "Position of Container", new Vector3(0f,0f,0f), new ConfigDescription("Location of Container"));
            RowCount = Config.Bind("Inventory Interface", "Count of Rows", 8, new ConfigDescription("Use this to increase your row count for inventory size increase while using this mod", new AcceptableValueRange<Int32>(5, 50)));
            hotKey1 = Config.Bind<string>("Hotkeys", "HotKey1", "z", "Hotkey 1 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey2 = Config.Bind<string>("Hotkeys", "HotKey2", "x", "Hotkey 2 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            hotKey3 = Config.Bind<string>("Hotkeys", "HotKey3", "c", "Hotkey 3 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html");
            quickAccessX = Config.Bind<float>("ZCurrentPositions", "quickAccessX", 9999, "Current X of Quick Slots");
            quickAccessY = Config.Bind<float>("ZCurrentPositions", "quickAccessY", 9999, "Current Y of Quick Slots");

        
            hotkeys = new ConfigEntry<string>[]
            {
                hotKey1,
                hotKey2,
                hotKey3,
            };
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            LoadAssets();
            harmony.PatchAll(assembly);
            
        }

        public void Update()
        {
            switch (dragger.isMouseDown)
            {
                case true:
                    InvPos.Value = inventory.transform.localPosition;
                    ContainerPos.Value = container.transform.localPosition;
                    SplitPos.Value = splitpanel.transform.localPosition;
                    break;
            }
        }

        public void OnDestroy()
        {
            Config.Save();
        }

        public void LoadAssets()
        {
            AssetBundle assetBundle = GetAssetBundleFromResources("containers");
            switch (AltInterface.Value)
            {
                case true:
                    ContainerGO = assetBundle.LoadAsset<GameObject>("RPGContainer");
                    InventoryGO = assetBundle.LoadAsset<GameObject>("rpginv");
                    SplitGO = assetBundle.LoadAsset<GameObject>("RPGSplitInv");
                    DragItemGO = assetBundle.LoadAsset<GameObject>("drag_itemz");
                    HudGO = assetBundle.LoadAsset<GameObject>("RPGHudElement");
                    break;
                case false:
                    ContainerGO = assetBundle.LoadAsset<GameObject>("ZContainer");
                    InventoryGO = assetBundle.LoadAsset<GameObject>("InventoryZ");
                    SplitGO = assetBundle.LoadAsset<GameObject>("SplitInventory");
                    DragItemGO = assetBundle.LoadAsset<GameObject>("drag_itemz");
                    HudGO = assetBundle.LoadAsset<GameObject>("HudElementZ");
                    break;
            }
            


            Debug.Log($"Loaded {ContainerGO.name}");
            Debug.Log($"Loaded {InventoryGO.name}");
            Debug.Log($"Loaded {SplitGO.name}");
            Debug.Log($"Loaded {DragItemGO.name}");
            Debug.Log($"Loaded {HudGO.name}");
        }
        
        private static AssetBundle GetAssetBundleFromResources(string filename)
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }
        
        //Patches

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class LoaderPatch
        {
            public static void Postfix(ZNetScene __instance)
            {
                Debug.Log("running Znet hook");
                __instance.m_prefabs.Add(ContainerGO);
                __instance.m_namedPrefabs.Add(ContainerGO.name.GetStableHashCode(), ContainerGO);
                __instance.m_prefabs.Add(InventoryGO);
                __instance.m_namedPrefabs.Add(InventoryGO.name.GetStableHashCode(), InventoryGO);
                __instance.m_prefabs.Add(SplitGO);
                __instance.m_namedPrefabs.Add(SplitGO.name.GetStableHashCode(), SplitGO);
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InvGUIPatch
        {
            public static void Prefix(InventoryGui __instance)
            {
                //Container Instantiation
                container = Instantiate(ContainerGO, __instance.m_container.gameObject.transform, false);
                container.transform.localPosition = ContainerPos.Value;
                
                //Inventory Instantiation
                inventory = Instantiate(InventoryGO, __instance.m_player.transform, false);
                inventory.transform.localPosition = InvPos.Value;
                
                //Setup SplitWindow
                splitpanel = Instantiate(SplitGO, __instance.m_splitPanel.gameObject.transform, false);
                splitpanel.transform.localPosition  = SplitPos.Value;
                
                //These events need to happen prior to the awake function that we Postfix in the next method so chosen route is Prefix in order to allow the instantiation run prior to games actual Awake() call

            }



            public static void Postfix(InventoryGui __instance)
            {
                //Set parent container back active then disable all children components
                __instance.m_container.gameObject.SetActive(true);
                __instance.m_container.gameObject.transform.Find("Darken").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("selected_frame").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("Weight").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("Bkg").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("container_name").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("sunken").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("ContainerGrid").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("ContainerScroll").gameObject.SetActive(false);
                __instance.m_container.gameObject.transform.Find("TakeAll").gameObject.SetActive(false);
                
                //Reassign the container rect transform so it is activated when open chest is called
                __instance.m_container = ContainerMGR2.internalCTRrect;
                
                //Setup our new container within InventoryGUI
                __instance.m_containerName = ContainerMGR2.InternalCTtitle;
                __instance.m_containerWeight = ContainerMGR2.InternalCTWeightTXT;
                __instance.m_containerGrid = ContainerMGR2.InternalCTGrid;
                __instance.m_takeAllButton = ContainerMGR2.InternalTakeAll;
                ContainerMGR2.InternalTakeAll.onClick.AddListener(__instance.OnTakeAll);
                InventoryGrid tempgrid = ContainerMGR2.InternalCTGrid;
                ContainerMGR2.InternalCTGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(tempgrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(__instance.OnSelectedItem));
                InventoryGrid tempgrid2 = ContainerMGR2.InternalCTGrid;
                tempgrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(tempgrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(__instance.OnRightClickItem));
                ContainerMGR2.internalCTRrect.gameObject.transform.SetSiblingIndex(__instance.m_container.gameObject.transform.GetSiblingIndex());
                
                //Disable old inventory GO
                __instance.m_player.gameObject.SetActive(true);
                 __instance.m_player.gameObject.transform.Find("Darken").gameObject.SetActive(false);
                 __instance.m_player.gameObject.transform.Find("selected_frame").gameObject.SetActive(false);
                 __instance.m_player.gameObject.transform.Find("Armor").gameObject.SetActive(false);
                 __instance.m_player.gameObject.transform.Find("Weight").gameObject.SetActive(false);
                 __instance.m_player.gameObject.transform.Find("Bkg").gameObject.SetActive(false);
                 __instance.m_player.gameObject.transform.Find("sunken").gameObject.SetActive(false);
                 __instance.m_player.gameObject.transform.Find("PlayerGrid").gameObject.SetActive(false);
                
                //Setup Inventory window Variables
                __instance.m_playerGrid = InventoryMGR2.internalplayergrid;
                __instance.m_player = InventoryMGR2.internalPlayerRect;
                __instance.m_armor = InventoryMGR2.internalPlayerArmor;
                __instance.m_weight = InventoryMGR2.internalPlayerWeight;
                
                //lets make the dragItem font and size match our theme
                var font = __instance.m_dragItemPrefab.gameObject.transform.Find("amount").GetComponent<Text>();
                font.font = DragItemGO.GetComponentInChildren<Text>().font;
                font.fontSize = 120;
                font.horizontalOverflow = HorizontalWrapMode.Overflow;
                font.verticalOverflow = VerticalWrapMode.Overflow;
                font.resizeTextForBestFit = false;
                font.color = new Color(0.8196079f, 0.7882354f, 0.7607844f, 1f);
                __instance.m_dragItemPrefab.gameObject.transform.Find("amount").gameObject.GetComponent<RectTransform>().localScale =
                    new Vector3(0.125f, 0.125f, 0);
                __instance.m_dragItemPrefab.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 60f);
                
                //Setup the clicking interface for player inventory
                InventoryGrid playerGrid = InventoryMGR2.internalplayergrid;
                playerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(playerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(__instance.OnSelectedItem));
                InventoryGrid playerGrid2 = InventoryMGR2.internalplayergrid;
                playerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(playerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(__instance.OnRightClickItem));

                //grab the oldsplit windows GO and set it active to initiate our Awake() function on our panel
                var OldSplitter = __instance.m_splitPanel.gameObject;
                 OldSplitter.transform.Find("darken").gameObject.SetActive(false);
                 OldSplitter.transform.Find("win_bkg").gameObject.SetActive(false);
                 OldSplitter.SetActive(true);
                 
                 //Setup the Split window now that its variables are initialized via setting its parent layer active therefore calling Awake() on our SplitWindowManager.cs file
                 __instance.m_splitOkButton = SplitWindowManager.internalsplitOK;
                 __instance.m_splitCancelButton = SplitWindowManager.internalsplitcancel;
                 __instance.m_splitAmount = SplitWindowManager.internalsplitamt;
                 __instance.m_splitSlider = SplitWindowManager.internalsplitslider;
                 __instance.m_splitIcon = SplitWindowManager.internalspliticon;
                 
                 //Go ahead and turn this panel off we can let UI manager do it's thing with this GO now and toggle it on/off when we needit
                 __instance.m_splitPanel.gameObject.SetActive(false);
                 __instance.m_splitIconName = SplitWindowManager.internalspliticonname;
                 
                 //Setup Listeners for when you use the slider/click the buttons
                 SplitWindowManager.internalsplitOK.onClick.AddListener(__instance.OnSplitOk);
                 SplitWindowManager.internalsplitcancel.onClick.AddListener(__instance.OnSplitCancel);
                 SplitWindowManager.internalsplitslider.onValueChanged.AddListener(__instance.OnSplitSliderChanged);
                 //grab the sibling index from our other GO and and set our index == to it
                 SplitWindowManager.internalsliderGO.transform.SetSiblingIndex(OldSplitter.transform.GetSiblingIndex());
                 OldSplitter.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        public static class GridPatcher
        {
            public static void Prefix(InventoryGrid __instance)
            {
                var scrollrect = InventoryMGR2.internalScroller;
                SetPrivateField(scrollrect, "m_HasRebuiltLayout", false);
                var containerscroller = ContainerMGR2.internalcontainerscroller;
                SetPrivateField(containerscroller, "m_HasRebuiltLayout", false);
            }
        }
        
        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
        public static class HotkeyBarPatch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                __instance.m_elementPrefab = HudGO;
            }
            
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Awake))]
        public static class InventorySizerPatch
        {
            public static void Postfix(Humanoid __instance)
            {
                __instance.m_inventory.m_height = RowCount.Value;
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        public static class HudAwakePatch
        {
            public static void Postfix(Hud __instance)
            {
                Transform newBar = Instantiate(__instance.m_rootObject.transform.Find("HotKeyBar"));
                newBar.name = "QuickAccessBar";
                newBar.SetParent(__instance.m_rootObject.transform);
                newBar.GetComponent<RectTransform>().localPosition = Vector3.zero;
                GameObject go = HudGO;
                QuickAccessBar qab = newBar.gameObject.AddComponent<QuickAccessBar>();
                qab.m_elementPrefab = go;
                DragNDrop.ApplyDragWindowCntrl(newBar.gameObject);
                Destroy(newBar.GetComponent<HotkeyBar>());
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public static class UpdatePatch
        {
            public static void Postfix(Player __instance, Inventory ___m_inventory)
            {
                if(Player.m_localPlayer==null)
                    return;
                int which;
                if (KeyUtils.CheckKeyDown(hotKey1.Value))
                    which = 1;
                else if (KeyUtils.CheckKeyDown(hotKey2.Value))
                    which = 2;
                else if (KeyUtils.CheckKeyDown(hotKey3.Value))
                    which = 3;
                else return;

                ItemDrop.ItemData itemAt = Player.m_localPlayer.m_inventory.GetItemAt(which + 4, ___m_inventory.GetHeight() - 1);
                if (itemAt != null)
                {
                    __instance.UseItem(null, itemAt, false);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
        public static class HotKeyPatch
        {
            public static void Postfix(InventoryGrid ___m_playerGrid)
            {
                Inventory inv = Player.m_localPlayer.GetInventory();
                int offset = inv.GetWidth() * (inv.GetHeight() - 1);
                SetSlotText(hotKey1.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++), false);
                SetSlotText(hotKey2.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++), false);
                SetSlotText(hotKey3.Value, ___m_playerGrid.m_gridRoot.transform.GetChild(offset++), false);
            }
        }
        
        //Assist Funcs
        public static void SetSlotText(string value, Transform transform, bool center = true)
        {
            Transform t = transform.Find("binding");
            if (!t)
            {
                t = Instantiate(HudGO.transform.Find("binding"), transform);
            }
            t.GetComponent<Text>().enabled = true;
            t.GetComponent<Text>().text = value;
            if (center)
            {
                t.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 17);
                t.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, -10);
            }
        }
        private static BindingFlags BindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            obj.GetType().GetField(fieldName, BindFlags).SetValue(obj, value);
        }
        
    }
}