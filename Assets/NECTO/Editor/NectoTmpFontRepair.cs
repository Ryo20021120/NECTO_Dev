#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Necto.Editor
{
    /// <summary>
    /// 参照元OTFが外れたTMPフォントを、日本語を実行時追加できるDynamic設定へ戻す。
    /// </summary>
    internal static class NectoTmpFontRepair
    {
        private static readonly string[,] FontPairs =
        {
            { "Assets/UI_Items/LINESeedJP_A_OTF_Bd SDF.asset", "Assets/UI_Items/LINESeedJP_A_OTF_Bd.otf" },
            { "Assets/UI_Items/LINESeedJP_A_OTF_Eb SDF.asset", "Assets/UI_Items/LINESeedJP_A_OTF_Eb.otf" },
            { "Assets/UI_Items/LINESeedJP_A_OTF_Rg SDF.asset", "Assets/UI_Items/LINESeedJP_A_OTF_Rg.otf" },
            { "Assets/UI_Items/LINESeedJP_A_OTF_Th SDF.asset", "Assets/UI_Items/LINESeedJP_A_OTF_Th.otf" }
        };

        [InitializeOnLoadMethod]
        private static void ScheduleRepair()
        {
            EditorApplication.delayCall += Repair;
        }

        private static void Repair()
        {
            bool changed = false;
            for (int i = 0; i < FontPairs.GetLength(0); i++)
            {
                TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPairs[i, 0]);
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPairs[i, 1]);
                if (fontAsset == null || sourceFont == null) continue;

                bool assetChanged = false;
                SerializedObject serializedFont = new SerializedObject(fontAsset);
                SerializedProperty sourceFontProperty = serializedFont.FindProperty("m_SourceFontFile");
                if (sourceFontProperty != null && sourceFontProperty.objectReferenceValue != sourceFont)
                {
                    sourceFontProperty.objectReferenceValue = sourceFont;
                    serializedFont.ApplyModifiedPropertiesWithoutUndo();
                    assetChanged = true;
                }

                if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                {
                    fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    assetChanged = true;
                }

                if (!fontAsset.isMultiAtlasTexturesEnabled)
                {
                    fontAsset.isMultiAtlasTexturesEnabled = true;
                    assetChanged = true;
                }

                if (assetChanged)
                {
                    EditorUtility.SetDirty(fontAsset);
                    changed = true;
                }
            }

            if (changed) AssetDatabase.SaveAssets();
        }
    }
}
#endif
