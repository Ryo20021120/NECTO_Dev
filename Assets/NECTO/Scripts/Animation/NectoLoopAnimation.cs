using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Necto.Animation
{
    /// <summary>NPCとモンスターへ、Animator Controllerなしで待機モーションを繰り返し再生します。</summary>
    public sealed class NectoLoopAnimation : MonoBehaviour
    {
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private double clipLength;

        public void Play(AnimationClip clip)
        {
            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null || clip == null) return;
            if (graph.IsValid()) graph.Destroy();
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.Update(0f);
            graph = PlayableGraph.Create("NECTO Loop - " + gameObject.name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Loop Animation", animator);
            output.SetSourcePlayable(playable);
            clipLength = clip.length;
            graph.Play();
        }

        private void Update()
        {
            if (!graph.IsValid() || clipLength <= 0d) return;
            double time = playable.GetTime();
            if (time >= clipLength) playable.SetTime(time % clipLength);
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}

namespace Necto.Prototype
{
    public static class NectoCharacterAnimation
    {
        public static void AttachIdle(GameObject character, string sourceName = null)
        {
            if (character == null || character.GetComponentInChildren<Animator>() == null) return;
            NectoAssetCatalog catalog = Resources.Load<NectoAssetCatalog>("NectoAssetCatalog");
            if (catalog == null) return;
            string key = string.IsNullOrEmpty(sourceName) ? character.name : sourceName;
            AnimationClip clip;
            if (key.Contains("Turtle", System.StringComparison.OrdinalIgnoreCase)) clip = catalog.turtleIdleClip;
            else if (key.Contains("Skeleton", System.StringComparison.OrdinalIgnoreCase)) clip = catalog.skeletonIdleClip;
            else if (key.Contains("Slime", System.StringComparison.OrdinalIgnoreCase)) clip = catalog.slimeIdleClip;
            else clip = catalog.npcIdleClip;
            if (clip == null) return;
            Necto.Animation.NectoLoopAnimation loop = character.GetComponent<Necto.Animation.NectoLoopAnimation>();
            if (loop == null) loop = character.AddComponent<Necto.Animation.NectoLoopAnimation>();
            loop.Play(clip);
        }
    }
}
