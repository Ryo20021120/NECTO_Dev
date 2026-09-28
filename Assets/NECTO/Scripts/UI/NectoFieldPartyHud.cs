using System.Collections.Generic;
using Necto.Data;
using Necto.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Necto.UI
{
    /// <summary>
    /// フィールドHUDのパーティアイコンを管理する。
    /// 通常は先頭のみ表示し、ゲームパッドのLボタン中は最大3体を横に展開する。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("NECTO/UI/Field Party HUD")]
    public sealed class NectoFieldPartyHud : MonoBehaviour
    {
        [Header("表示")]
        [SerializeField] private NectoMonsterSkillDatabase monsterSkillDatabase;
        [Tooltip("表示に使用する既存の子スロット数。現在のパーティ上限に合わせて3を使用します。")]
        [Range(1, 3)]
        [SerializeField] private int maximumSlots = 3;

        [Header("操作")]
        [Tooltip("ゲームパッドのLボタンを押している間、2体目以降を横に展開します。")]
        [SerializeField] private bool expandWhileLeftShoulderHeld = true;

        private readonly List<GameObject> slots = new List<GameObject>();
        private readonly List<NectoPrefabIconPresenter> presenters = new List<NectoPrefabIconPresenter>();
        private string rosterSignature;
        private bool expanded;
        private bool wasLeftShoulderHeld;
        private int selectedIndex;
        private float nextNavigationTime;

        /// <summary>パーティ選択中は左スティックを移動へ渡さない。</summary>
        public static bool InputCaptured { get; private set; }

        private void Awake()
        {
            PrepareSlots();
            RefreshParty(true);
        }

        private void OnEnable()
        {
            PrepareSlots();
            RefreshParty(true);
        }

        private void Update()
        {
            Gamepad gamepad = Gamepad.current;
            bool shouldExpand = expandWhileLeftShoulderHeld &&
                gamepad != null && gamepad.leftShoulder.isPressed;

            if (shouldExpand && !wasLeftShoulderHeld)
                BeginSelection();

            if (shouldExpand)
                UpdateSelection(NectoGamepadInput.ReadLeftStick(gamepad));
            else if (wasLeftShoulderHeld)
                ConfirmSelection();

            InputCaptured = shouldExpand;
            wasLeftShoulderHeld = shouldExpand;

            string currentSignature = string.Join("|", NectoPartyRoster.PartyIds);
            if (shouldExpand != expanded || currentSignature != rosterSignature)
            {
                expanded = shouldExpand;
                RefreshParty(currentSignature != rosterSignature);
            }
        }

        private void OnDisable()
        {
            InputCaptured = false;
            wasLeftShoulderHeld = false;
        }

        private void BeginSelection()
        {
            expanded = true;
            IReadOnlyList<string> partyIds = NectoPartyRoster.PartyIds;
            selectedIndex = 0;
            for (int i = 0; i < partyIds.Count; i++)
                if (partyIds[i] == NectoPartyRoster.ActiveMonsterId) selectedIndex = i;
            nextNavigationTime = 0f;
            RefreshParty(false);
        }

        private void UpdateSelection(Vector2 input)
        {
            IReadOnlyList<string> partyIds = NectoPartyRoster.PartyIds;
            int count = Mathf.Min(partyIds.Count, slots.Count);
            if (count <= 1 || Time.unscaledTime < nextNavigationTime || Mathf.Abs(input.x) < .55f)
            {
                UpdateSelectionVisuals();
                return;
            }

            selectedIndex = (selectedIndex + (input.x > 0f ? 1 : -1) + count) % count;
            nextNavigationTime = Time.unscaledTime + .22f;
            UpdateSelectionVisuals();
        }

        private void ConfirmSelection()
        {
            IReadOnlyList<string> partyIds = NectoPartyRoster.PartyIds;
            if (selectedIndex >= 0 && selectedIndex < partyIds.Count)
                NectoPartyRoster.SetActiveMonster(partyIds[selectedIndex]);
            expanded = false;
            RefreshParty(false);
        }

        private void PrepareSlots()
        {
            if (slots.Count > 0) return;

            int slotCount = Mathf.Min(maximumSlots, transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (i >= slotCount)
                {
                    child.SetActive(false);
                    continue;
                }

                child.name = $"Party HUD Slot {i + 1}";
                NectoPrefabIconPresenter presenter =
                    child.GetComponent<NectoPrefabIconPresenter>() ??
                    child.AddComponent<NectoPrefabIconPresenter>();
                slots.Add(child);
                presenters.Add(presenter);
            }
        }

        private void RefreshParty(bool refreshIcons)
        {
            IReadOnlyList<string> partyIds = NectoPartyRoster.PartyIds;
            rosterSignature = string.Join("|", partyIds);
            NectoCharacterDatabase database = NectoCharacterDatabase.Load();

            for (int i = 0; i < slots.Count; i++)
            {
                bool hasMonster = i < partyIds.Count;
                bool isActiveMonster = hasMonster && partyIds[i] == NectoPartyRoster.ActiveMonsterId;
                bool visible = hasMonster && (expanded || isActiveMonster);
                slots[i].SetActive(visible);

                if (!refreshIcons || !hasMonster) continue;
                NectoMonsterSkillEntry debugEntry =
                    monsterSkillDatabase != null ? monsterSkillDatabase.Find(partyIds[i]) : null;
                if (debugEntry != null)
                {
                    presenters[i].Show(debugEntry.IconPrefab);
                    continue;
                }

                NectoCharacterEntry entry = database != null ? database.Find(partyIds[i]) : null;
                presenters[i].Show(entry != null ? entry.iconPrefab : null);
            }

            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                bool selected = expanded && i == selectedIndex;
                slots[i].transform.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maximumSlots = Mathf.Clamp(maximumSlots, 1, 3);
        }
#endif
    }
}
