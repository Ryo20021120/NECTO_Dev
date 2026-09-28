using Necto.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Necto.UI
{
    /// <summary>Town UIの画面開閉だけを管理する。</summary>
    public sealed class NectoUiExSettingsController : MonoBehaviour
    {
        private static NectoUiExSettingsController instance;

        [Header("開閉する画面（Inspectorで接続）")]
        [SerializeField] private GameObject settingsScreenRoot;
        [SerializeField] private GameObject inventoryScreenRoot;
        [FormerlySerializedAs("townHudRoot")]
        [SerializeField] private GameObject fieldHudRoot;
        [Tooltip("TownSceneでは常にField HUDを非表示・停止します。")]
        [SerializeField] private bool hideFieldHudInTownScene = true;

        private NectoSettingsScreenController settingsScreen;
        private bool settingsOpen;
        private bool inventoryOpen;
        private float nextNavigation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FindInstance() { instance = FindFirstObjectByType<NectoUiExSettingsController>(); }

        public static bool TryToggle()
        {
            if (instance == null) instance = FindFirstObjectByType<NectoUiExSettingsController>();
            if (instance == null) return false;
            instance.SetSettingsOpen(!instance.settingsOpen);
            return true;
        }

        public static bool TryToggleMenu()
        {
            if (instance == null) instance = FindFirstObjectByType<NectoUiExSettingsController>();
            if (instance == null || instance.inventoryScreenRoot == null) return false;
            instance.SetInventoryOpen(!instance.inventoryOpen);
            return true;
        }

        private void Awake()
        {
            instance = this;
            EnsureEventSystem();
            if (settingsScreenRoot != null)
                settingsScreen = settingsScreenRoot.GetComponent<NectoSettingsScreenController>();
            // Map / START / SELECT はTownとFieldで共通。階層を分離しても
            // このコントローラー配下から名前で接続できるようにする。
            Transform hud = transform;
            WireButton(FindRecursive(hud, "Btn_Settings") ?? FindRecursive(hud, "Icon_Setting"), () => SetSettingsOpen(true));
            WireButton(FindRecursive(hud, "Btn_Bag") ?? FindRecursive(hud, "Icon_Bag"), () => SetInventoryOpen(true));
            settingsScreen?.Initialize();
            SetSettingsOpen(false);
        }

        private void Update()
        {
            Gamepad pad = Gamepad.current;
            Keyboard keyboard = Keyboard.current;
            bool toggleInventory = pad != null && pad.startButton.wasPressedThisFrame;
            toggleInventory |= keyboard != null && keyboard.tabKey.wasPressedThisFrame;
            if (toggleInventory) { SetInventoryOpen(!inventoryOpen); return; }
            bool toggleSettings = pad != null && pad.selectButton.wasPressedThisFrame;
            toggleSettings |= keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            if (toggleSettings) { SetSettingsOpen(!settingsOpen); return; }
            if (!settingsOpen && !inventoryOpen) return;
            // B（下側ボタン）は交換選択の解除／戻る。画面自体はSTART/SELECTで閉じる。
            if ((pad != null && pad.buttonSouth.wasPressedThisFrame) || (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame))
            {
                if (inventoryOpen)
                {
                    NectoMonsterSelectionScreen monsterScreen = inventoryScreenRoot != null
                        ? inventoryScreenRoot.GetComponent<NectoMonsterSelectionScreen>()
                        : null;
                    // Bは交換元の選択解除だけ。画面を閉じるのはSTARTのみ。
                    monsterScreen?.CancelSwap();
                }
                return;
            }
            if (!settingsOpen || settingsScreen == null) return;

            Vector2 navigation = Necto.Input.NectoGamepadInput.ReadNavigation(pad);
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed) navigation.x = -1f;
                else if (keyboard.rightArrowKey.isPressed) navigation.x = 1f;
                if (keyboard.downArrowKey.isPressed) navigation.y = -1f;
                else if (keyboard.upArrowKey.isPressed) navigation.y = 1f;
            }
            if (Time.unscaledTime >= nextNavigation && settingsScreen.Navigate(navigation))
                nextNavigation = Time.unscaledTime + .22f;
            // A（右側ボタン）で設定項目を決定する。
            if (pad != null && pad.buttonEast.wasPressedThisFrame) settingsScreen.Confirm();
        }

        private void SetSettingsOpen(bool value)
        {
            settingsOpen = value;
            if (value) inventoryOpen = false;
            SetFieldHudVisible(!value && !inventoryOpen);
            if (settingsScreenRoot != null) settingsScreenRoot.SetActive(value);
            if (inventoryScreenRoot != null) inventoryScreenRoot.SetActive(false);
            if (value) settingsScreen?.ShowCurrentPage();
            NectoSummonBattle.SetMenuLock(this, value);
        }

        private void SetInventoryOpen(bool value)
        {
            inventoryOpen = value;
            if (value) settingsOpen = false;
            SetFieldHudVisible(!value && !settingsOpen);
            if (settingsScreenRoot != null) settingsScreenRoot.SetActive(false);
            if (inventoryScreenRoot != null) inventoryScreenRoot.SetActive(value);
            NectoSummonBattle.SetMenuLock(this, value);
        }

        private void SetFieldHudVisible(bool requestedVisible)
        {
            if (fieldHudRoot == null) return;
            bool isTownScene = NectoSceneNames.IsTown(SceneManager.GetActiveScene());
            fieldHudRoot.SetActive(requestedVisible && !(hideFieldHudInTownScene && isTownScene));
        }

        private static void WireButton(Transform target, UnityEngine.Events.UnityAction action)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) return;
            Button button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static Transform FindRecursive(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name == objectName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindRecursive(root.GetChild(i), objectName);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void OnDestroy()
        {
            NectoSummonBattle.SetMenuLock(this, false);
            if (instance == this) instance = null;
        }
    }
}
