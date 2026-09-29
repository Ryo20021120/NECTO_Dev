#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Necto.Editor
{
    /// <summary>プレイヤー表示Prefabの足元と表示身長を統一する一回限りの補正。</summary>
    internal static class NectoNormalizePlayerVisualPrefabs
    {
        private const float TargetHeight = 2f;
        private const string PlayableCharactersPath = "Assets/02 Playable Characters.prefab";

        private static readonly string[] PrefabPaths =
        {
            "Assets/NECTO/Data/Runtime/Player/Player_蒼真.prefab",
            "Assets/NECTO/Data/Runtime/Player/Player_紅羽.prefab",
            "Assets/NECTO/Data/Runtime/Player/Player_玄牙.prefab",
            "Assets/NECTO/Data/Runtime/Player/Player_白蓮.prefab",
            "Assets/NECTO/Data/Runtime/Player/Player_紫苑.prefab",
            "Assets/NECTO/Data/Runtime/Player/Player_雷華.prefab",
            "Assets/NECTO/Data/Runtime/Player/Player_黒曜.prefab"
        };

        [MenuItem("NECTO/Player/Normalize All Player Visual Prefabs")]
        public static void NormalizeAll()
        {
            foreach (string path in PrefabPaths)
                Normalize(path);

            NormalizePlayableCharacters();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NECTO] 7体の表示Prefabを高さ2m・足元Y=0へ補正しました。");
        }

        private static void NormalizePlayableCharacters()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayableCharactersPath);
            try
            {
                Animator[] players = prefabRoot.GetComponentsInChildren<Animator>(true)
                    .Where(animator => animator.gameObject.name.StartsWith("Player_", StringComparison.Ordinal))
                    .ToArray();

                foreach (Animator animator in players)
                {
                    Transform visual = animator.transform;
                    Transform commonParent = visual.parent;
                    visual.localRotation = Quaternion.identity;
                    EvaluateDefaultPose(animator);

                    if (!TryGetHumanoidMeasurements(animator, out float measuredHeight,
                            out Transform leftFoot, out Transform rightFoot,
                            out Transform leftToes, out Transform rightToes))
                    {
                        Debug.LogWarning($"[NECTO] Humanoidの足を取得できません: {visual.name}");
                        continue;
                    }

                    float scale = TargetHeight / measuredHeight;
                    visual.localScale *= scale;
                    EvaluateDefaultPose(animator);

                    leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
                    rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
                    Transform leftCenter = leftToes != null ? leftToes : leftFoot;
                    Transform rightCenter = rightToes != null ? rightToes : rightFoot;
                    Vector3 feetCenter = (leftCenter.position + rightCenter.position) * 0.5f;
                    float soleY = GetSoleY(animator, leftFoot, rightFoot, leftToes, rightToes);
                    Vector3 target = commonParent.position;
                    visual.position += new Vector3(target.x - feetCenter.x, target.y - soleY, target.z - feetCenter.z);

                    Debug.Log($"[NECTO MAIN PREFAB] {visual.name}: height={TargetHeight:F2}, localPosition={visual.localPosition}, localScale={visual.localScale}");
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayableCharactersPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem("NECTO/Player/Audit Player Foot Positions")]
        private static void AuditAll()
        {
            foreach (string path in PrefabPaths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Animator animator = root.GetComponent<Animator>();
                    EvaluateDefaultPose(animator);
                    Transform leftFoot = animator != null && animator.isHuman
                        ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
                    Transform rightFoot = animator != null && animator.isHuman
                        ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
                    Transform leftToes = animator != null && animator.isHuman
                        ? animator.GetBoneTransform(HumanBodyBones.LeftToes) : null;
                    Transform rightToes = animator != null && animator.isHuman
                        ? animator.GetBoneTransform(HumanBodyBones.RightToes) : null;
                    Transform head = animator != null && animator.isHuman
                        ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                        .Where(renderer => !(renderer is ParticleSystemRenderer))
                        .ToArray();
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    string Child(Transform transform) => transform == null ? "なし" :
                        $"{transform.name}:({transform.position.x:F4},{transform.position.y:F4},{transform.position.z:F4})";
                    Debug.Log($"[NECTO FOOT AUDIT] {root.name} humanScale={(animator != null ? animator.humanScale : 0f):F4} Bounds minY={bounds.min.y:F4} center=({bounds.center.x:F4},{bounds.center.y:F4},{bounds.center.z:F4}) size=({bounds.size.x:F4},{bounds.size.y:F4},{bounds.size.z:F4}) / {Child(head)} / {Child(leftFoot)} / {Child(rightFoot)} / {Child(leftToes)} / {Child(rightToes)}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            GameObject mainRoot = PrefabUtility.LoadPrefabContents(PlayableCharactersPath);
            try
            {
                foreach (Animator animator in mainRoot.GetComponentsInChildren<Animator>(true)
                             .Where(value => value.gameObject.name.StartsWith("Player_", StringComparison.Ordinal)))
                {
                    EvaluateDefaultPose(animator);
                    if (!TryGetHumanoidMeasurements(animator, out float height,
                            out Transform leftFoot, out Transform rightFoot,
                            out Transform leftToes, out Transform rightToes)) continue;
                    float soleY = GetSoleY(animator, leftFoot, rightFoot, leftToes, rightToes);
                    float parentY = animator.transform.parent.position.y;
                    Debug.Log($"[NECTO MAIN AUDIT] {animator.name}: height={height:F4}, soleLocalY={soleY - parentY:F4}, rootLocal={animator.transform.localPosition}, scale={animator.transform.localScale}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(mainRoot);
            }
        }

        private static void Normalize(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Animator animator = root.GetComponent<Animator>();
                EvaluateDefaultPose(animator);
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => !(renderer is ParticleSystemRenderer))
                    .ToArray();

                if (renderers.Length == 0)
                {
                    Debug.LogWarning($"[NECTO] Rendererがないため補正をスキップ: {path}");
                    return;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                Transform leftFoot = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
                Transform rightFoot = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
                Transform leftToes = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.LeftToes) : null;
                Transform rightToes = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.RightToes) : null;
                Transform head = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.Head) : null;

                float measuredHeight = bounds.size.y;
                if (head != null && leftFoot != null && rightFoot != null)
                {
                    float soleY = GetSoleY(animator, leftFoot, rightFoot, leftToes, rightToes);
                    measuredHeight = head.position.y - soleY + animator.humanScale * 0.28f;
                }

                if (measuredHeight <= 0.0001f)
                {
                    Debug.LogWarning($"[NECTO] 高さを測定できないため補正をスキップ: {path}");
                    return;
                }

                float scale = TargetHeight / measuredHeight;
                Transform[] directChildren = root.transform.Cast<Transform>().ToArray();
                foreach (Transform child in directChildren)
                {
                    child.localPosition *= scale;
                    child.localScale *= scale;
                }

                EvaluateDefaultPose(animator);
                leftFoot = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
                rightFoot = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
                leftToes = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.LeftToes) : null;
                rightToes = animator != null && animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.RightToes) : null;

                Vector3 worldLift;
                if (leftFoot != null && rightFoot != null)
                {
                    Transform leftCenter = leftToes != null ? leftToes : leftFoot;
                    Transform rightCenter = rightToes != null ? rightToes : rightFoot;
                    Vector3 feetCenter = (leftCenter.position + rightCenter.position) * 0.5f;
                    float soleY = GetSoleY(animator, leftFoot, rightFoot, leftToes, rightToes);
                    worldLift = new Vector3(
                        root.transform.position.x - feetCenter.x,
                        root.transform.position.y - soleY,
                        root.transform.position.z - feetCenter.z);
                }
                else
                {
                    bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    worldLift = new Vector3(
                        root.transform.position.x - bounds.center.x,
                        root.transform.position.y - bounds.min.y,
                        root.transform.position.z - bounds.center.z);
                }
                Vector3 localLift = root.transform.InverseTransformVector(worldLift);
                root.transform.localPosition += localLift;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[NECTO] Humanoid補正完了: {path} (計測身長 {measuredHeight:F3}m / 倍率 {scale:F4})");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NECTO] 補正失敗: {path}\n{exception}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EvaluateDefaultPose(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
        }

        private static float GetSoleY(
            Animator animator,
            Transform leftFoot,
            Transform rightFoot,
            Transform leftToes,
            Transform rightToes)
        {
            float humanScale = animator != null ? animator.humanScale : 1f;
            if (leftToes != null && rightToes != null)
                return Mathf.Min(leftToes.position.y, rightToes.position.y) - humanScale * 0.05f;

            return Mathf.Min(leftFoot.position.y, rightFoot.position.y) - humanScale * 0.15f;
        }

        private static bool TryGetHumanoidMeasurements(
            Animator animator,
            out float measuredHeight,
            out Transform leftFoot,
            out Transform rightFoot,
            out Transform leftToes,
            out Transform rightToes)
        {
            measuredHeight = 0f;
            leftFoot = rightFoot = leftToes = rightToes = null;
            if (animator == null || !animator.isHuman) return false;

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
            if (head == null || leftFoot == null || rightFoot == null) return false;

            float soleY = GetSoleY(animator, leftFoot, rightFoot, leftToes, rightToes);
            measuredHeight = head.position.y - soleY + animator.humanScale * 0.28f;
            return measuredHeight > 0.0001f;
        }
    }
}
#endif
