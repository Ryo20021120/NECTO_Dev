using UnityEngine;

namespace Necto.Player
{
    /// <summary>
    /// AnimatorからRoot Motionを抽出するがTransformへは適用しない。
    /// CharacterControllerで移動するキャラクターをIn-Placeで再生するために使う。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class NectoInPlaceRootMotion : MonoBehaviour
    {
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            GetComponent<Animator>().applyRootMotion = true;
        }

        private void OnAnimatorMove()
        {
            // animator.deltaPosition / deltaRotationを意図的に適用しない。
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
        }
    }
}
