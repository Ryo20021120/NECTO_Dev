using System;
using System.Collections.Generic;
using UnityEngine;

namespace Necto.Development
{
    /// <summary>
    /// 子オブジェクトを切り替え可能なキャラクター外見として登録する目印。
    /// GameObject名やキャラクター名には依存しない。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NectoCharacterVisualContainer : MonoBehaviour
    {
        [Serializable]
        private sealed class CharacterVisualBinding
        {
            public string characterId;
            public GameObject visual;
        }

        [Header("キャラクターIDと表示オブジェクトの対応")]
        [SerializeField] private List<CharacterVisualBinding> bindings = new List<CharacterVisualBinding>();

        [Header("表示対象一覧")]
        [SerializeField] private List<GameObject> characterVisuals = new List<GameObject>();

        public Transform PlayerRoot => transform.parent;

        public GameObject FindByCharacterId(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            CharacterVisualBinding binding = bindings.Find(item =>
                item != null && item.characterId == characterId && item.visual != null);
            return binding != null ? binding.visual : null;
        }

        public void CollectCharacters(List<GameObject> destination)
        {
            destination.Clear();

            if (characterVisuals.Count > 0)
            {
                foreach (GameObject visual in characterVisuals)
                    if (visual != null && !destination.Contains(visual)) destination.Add(visual);
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject candidate = transform.GetChild(i).gameObject;
                if (candidate.GetComponentInChildren<Animator>(true) != null ||
                    candidate.GetComponentInChildren<Renderer>(true) != null)
                    destination.Add(candidate);
            }
        }

        public void ShowOnly(GameObject selected)
        {
            var visuals = new List<GameObject>();
            CollectCharacters(visuals);
            foreach (GameObject visual in visuals)
                visual.SetActive(visual == selected);
        }
    }
}
