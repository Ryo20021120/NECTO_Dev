using System.Collections.Generic;
using Necto.Data;
using Necto.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>
    /// 選択中モンスターの装備技を、Vertical Layout Group配下に自動生成する。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("NECTO/UI/Skill Command List HUD")]
    public sealed class NectoSkillCommandListHud : MonoBehaviour
    {
        [Header("データ")]
        [SerializeField] private NectoMonsterSkillDatabase monsterSkillDatabase;

        [Header("表示項目のテンプレート")]
        [Tooltip("Skill Command List直下にあるUI項目の1つを登録します。")]
        [SerializeField] private GameObject itemTemplate;

        [Header("コマンドボタン画像")]
        [SerializeField] private Sprite buttonA;
        [SerializeField] private Sprite buttonB;
        [SerializeField] private Sprite buttonX;
        [SerializeField] private Sprite buttonY;

        private readonly List<GameObject> generatedItems = new List<GameObject>();
        private bool prepared;

        private void Awake()
        {
            PrepareTemplate();
        }

        private void OnEnable()
        {
            NectoPartyRoster.ActiveMonsterChanged += HandleActiveMonsterChanged;
            PrepareTemplate();
            Rebuild();
        }

        private void OnDisable()
        {
            NectoPartyRoster.ActiveMonsterChanged -= HandleActiveMonsterChanged;
        }

        private void HandleActiveMonsterChanged(string _)
        {
            Rebuild();
        }

        private void PrepareTemplate()
        {
            if (prepared) return;

            if (itemTemplate == null && transform.childCount > 0)
                itemTemplate = transform.GetChild(0).gameObject;

            // 編集時に置いてある見本は、実行中は表示しない。
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            prepared = true;
        }

        public void Rebuild()
        {
            PrepareTemplate();
            ClearGeneratedItems();

            if (monsterSkillDatabase == null || itemTemplate == null) return;

            NectoMonsterSkillEntry monster =
                monsterSkillDatabase.Find(NectoPartyRoster.ActiveMonsterId);
            if (monster == null && monsterSkillDatabase.Monsters.Count > 0)
                monster = monsterSkillDatabase.Monsters[0];
            if (monster == null) return;

            foreach (NectoDebugSkillData skill in monster.Skills)
            {
                if (skill == null) continue;

                GameObject item = Instantiate(itemTemplate, transform);
                item.name = $"Skill Command - {skill.SkillId}";
                ConfigureItem(item, skill);
                item.SetActive(true);
                generatedItems.Add(item);
            }

            if (transform is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void ConfigureItem(GameObject item, NectoDebugSkillData skill)
        {
            ConfigureCommandButtons(item, skill.Command);

            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0) return;

            if (texts.Length == 1)
            {
                texts[0].text = $"{skill.Command}    {skill.DisplayName}";
                return;
            }

            TMP_Text commandText = null;
            TMP_Text nameText = null;
            foreach (TMP_Text text in texts)
            {
                string objectName = text.gameObject.name.ToLowerInvariant();
                if (commandText == null && (objectName.Contains("command") || objectName.Contains("button")))
                    commandText = text;
                else if (nameText == null && (objectName.Contains("name") || objectName.Contains("skill")))
                    nameText = text;
            }

            commandText ??= texts[0];
            nameText ??= texts.Length > 1 ? texts[1] : texts[0];
            commandText.text = skill.Command;
            nameText.text = skill.DisplayName;
        }

        private void ConfigureCommandButtons(GameObject item, string command)
        {
            // テンプレートのボタン欄は、A画像を6枚並べた状態で作られている。
            // LayoutGroupの階層に依存せず、登録済みのボタンSpriteを持つImageだけを拾う。
            List<Image> icons = new List<Image>();
            foreach (Image image in item.GetComponentsInChildren<Image>(true))
            {
                if (IsCommandButtonSprite(image.sprite))
                    icons.Add(image);
            }

            if (icons.Count == 0) return;

            string normalizedCommand = (command ?? string.Empty).Trim().ToUpperInvariant();
            for (int i = 0; i < icons.Count; i++)
            {
                bool used = i < normalizedCommand.Length;
                icons[i].gameObject.SetActive(used);
                if (!used) continue;

                Sprite sprite = GetButtonSprite(normalizedCommand[i]);
                if (sprite != null) icons[i].sprite = sprite;
            }
        }

        private bool IsCommandButtonSprite(Sprite sprite)
        {
            return sprite != null &&
                   (sprite == buttonA || sprite == buttonB || sprite == buttonX || sprite == buttonY);
        }

        private Sprite GetButtonSprite(char command)
        {
            switch (command)
            {
                case 'A': return buttonA;
                case 'B': return buttonB;
                case 'X': return buttonX;
                case 'Y': return buttonY;
                default: return null;
            }
        }

        private void ClearGeneratedItems()
        {
            foreach (GameObject item in generatedItems)
            {
                if (item == null) continue;
                item.SetActive(false);
                Destroy(item);
            }
            generatedItems.Clear();
        }
    }
}
