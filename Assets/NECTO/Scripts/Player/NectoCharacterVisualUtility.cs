using UnityEngine;

namespace Necto.Prototype
{
    /// <summary>キャラクター表示のサイズ、足元、実行時マテリアルだけを調整する。</summary>
    public static class NectoCharacterVisualUtility
    {
        public static void FitCharacter(GameObject character, float targetHeight)
        {
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y > 0.001f)
                character.transform.localScale *= targetHeight / bounds.size.y;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float groundY = character.transform.parent.position.y - 1f;
            character.transform.position += Vector3.up * (groundY - bounds.min.y);
        }

        public static float TargetHeight(GameObject prefab)
        {
            if (prefab == null) return 2f;
            string name = prefab.name;
            return name.Contains("Breathing Idle", System.StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Player_04", System.StringComparison.OrdinalIgnoreCase)
                ? 2.25f
                : 2f;
        }

        public static void RepairMaterials(GameObject target)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null || target == null) return;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || (source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/"))) continue;
                    Material replacement = new Material(urp) { name = (source != null ? source.name : "NECTO") + " Runtime URP" };
                    if (source != null)
                    {
                        Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
                            source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                        replacement.SetColor("_BaseColor", color);
                        if (texture != null) replacement.SetTexture("_BaseMap", texture);
                    }
                    materials[i] = replacement;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }
    }
}
