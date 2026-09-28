using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>背景込みのキャラクターアイコンPrefabをUI枠内へ表示する。</summary>
    public sealed class NectoPrefabIconPresenter : MonoBehaviour
    {
        private GameObject currentPrefab;
        private GameObject instance;

        private void LateUpdate()
        {
            // Button の ColorTint が、透明にした表示ホストを白へ戻すことがある。
            // 背景込みPrefabだけを見せるため、ホスト自身は常に透明に保つ。
            Image hostImage = GetComponent<Image>();
            if (hostImage != null)
            {
                hostImage.sprite = null;
                hostImage.color = Color.clear;
            }
        }

        public void Show(GameObject prefab)
        {
            Image hostImage = GetComponent<Image>();
            if (hostImage != null)
            {
                hostImage.sprite = null;
                hostImage.color = Color.clear;
            }
            if (currentPrefab == prefab && instance != null) return;
            if (instance != null) Destroy(instance);
            currentPrefab = prefab;
            if (prefab == null) return;

            instance = Instantiate(prefab, transform, false);
            instance.name = prefab.name + " [表示用]";

            // 一部のUI Prefabで、Instantiate後に子ImageのSprite参照だけが欠けることがある。
            // 元Prefabの表示設定を明示的に写し、背景込みのアイコンをそのまま再現する。
            Image[] sourceImages = prefab.GetComponentsInChildren<Image>(true);
            Image[] instanceImages = instance.GetComponentsInChildren<Image>(true);
            int imageCount = Mathf.Min(sourceImages.Length, instanceImages.Length);
            for (int i = 0; i < imageCount; i++)
            {
                instanceImages[i].sprite = sourceImages[i].sprite;
                instanceImages[i].material = sourceImages[i].material;
                instanceImages[i].color = sourceImages[i].color;
                instanceImages[i].type = sourceImages[i].type;
                instanceImages[i].preserveAspect = sourceImages[i].preserveAspect;
                instanceImages[i].SetAllDirty();
            }
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }
            foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }
    }
}
