using UnityEngine;

namespace Necto.Effects
{
    public enum NectoEffectPointType
    {
        RightHand,
        LeftHand,
        Weapon,
        Front,
        Ground,
        Head,
        Body,
        Custom
    }

    /// <summary>
    /// キャラクターや操作親に置く、エフェクトの発生地点。
    /// キャラ変更後もPointTypeから現在有効な発生地点を検索できる。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("NECTO/Effects/Necto Effect Point")]
    public sealed class NectoEffectPoint : MonoBehaviour
    {
        [Header("Effect Point / エフェクト発生地点")]
        [SerializeField] private NectoEffectPointType pointType = NectoEffectPointType.RightHand;

        [Tooltip("同じ種類が複数ある場合は、数値が大きいPointを優先します")]
        [SerializeField] private int priority;

        public NectoEffectPointType PointType => pointType;
        public int Priority => priority;
        public Transform PointTransform => transform;

        /// <summary>指定したキャラの中から、有効な同種Pointを取得する。</summary>
        public static NectoEffectPoint Find(Transform characterRoot, NectoEffectPointType type, bool includeInactive = false)
        {
            if (characterRoot == null) return null;

            NectoEffectPoint best = null;
            NectoEffectPoint[] points = characterRoot.GetComponentsInChildren<NectoEffectPoint>(includeInactive);
            for (int i = 0; i < points.Length; i++)
            {
                NectoEffectPoint point = points[i];
                if (point.pointType != type) continue;
                if (!includeInactive && !point.gameObject.activeInHierarchy) continue;
                if (best == null || point.priority > best.priority) best = point;
            }
            return best;
        }

        /// <summary>
        /// 選択中のキャラを先に探し、なければPlayer_Main側の共通Pointを探す。
        /// </summary>
        public static NectoEffectPoint FindForActiveCharacter(
            Transform playerRoot,
            Transform activeCharacter,
            NectoEffectPointType type)
        {
            NectoEffectPoint point = Find(activeCharacter, type);
            return point != null ? point : Find(playerRoot, type);
        }

        /// <summary>
        /// NectoEffectPoint が見つからない場合、Humanoidボーンを自動でフォールバックとして返す。
        /// NectoEffectPoint を配置しなくてもエフェクトが出る。
        /// </summary>
        public static Transform ResolveTransform(
            Transform playerRoot,
            Transform activeCharacter,
            NectoEffectPointType type,
            Animator animator)
        {
            NectoEffectPoint point = FindForActiveCharacter(playerRoot, activeCharacter, type);
            if (point != null) return point.transform;
            if (animator != null && animator.isHuman)
            {
                Transform bone = animator.GetBoneTransform(TypeToHumanBone(type));
                if (bone != null) return bone;
            }
            return activeCharacter != null ? activeCharacter : playerRoot;
        }

        private static HumanBodyBones TypeToHumanBone(NectoEffectPointType type)
        {
            switch (type)
            {
                case NectoEffectPointType.RightHand: return HumanBodyBones.RightHand;
                case NectoEffectPointType.LeftHand:  return HumanBodyBones.LeftHand;
                case NectoEffectPointType.Weapon:    return HumanBodyBones.RightIndexDistal;
                case NectoEffectPointType.Head:      return HumanBodyBones.Head;
                case NectoEffectPointType.Ground:    return HumanBodyBones.RightFoot;
                case NectoEffectPointType.Body:      return HumanBodyBones.Chest;
                default:                             return HumanBodyBones.Hips;
            }
        }

        [ContextMenu("Transformをローカル原点へ戻す")]
        private void ResetLocalTransform()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = PointColor(pointType);
            Gizmos.DrawWireSphere(transform.position, .055f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * .22f);
        }

        private static Color PointColor(NectoEffectPointType type)
        {
            switch (type)
            {
                case NectoEffectPointType.RightHand: return new Color(.15f, .7f, 1f);
                case NectoEffectPointType.LeftHand: return new Color(.2f, 1f, .65f);
                case NectoEffectPointType.Weapon: return new Color(1f, .3f, .2f);
                case NectoEffectPointType.Front: return new Color(1f, .8f, .1f);
                case NectoEffectPointType.Ground: return new Color(.6f, .35f, 1f);
                default: return Color.white;
            }
        }
    }
}
