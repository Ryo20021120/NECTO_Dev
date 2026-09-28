using UnityEngine;

namespace Necto.Prototype
{
    public sealed class NectoAssetCatalog : ScriptableObject
    {
        public Material skybox;
        public GameObject playerPrefab;
        public GameObject[] selectablePlayerPrefabs = new GameObject[0];
        public Sprite[] selectablePlayerIcons = new Sprite[0];
        public AnimationClip[] selectablePlayerWalkClips = new AnimationClip[0];
        public AnimationClip[] selectablePlayerIdleClips = new AnimationClip[0];
        public GameObject femaleNpcPrefab;
        public GameObject[] naturePrefabs = new GameObject[0];
        public GameObject[] monsterPrefabs = new GameObject[0];
        public GameObject[] allyMonsterPrefabs = new GameObject[0];
        public GameObject[] enemyMonsterPrefabs = new GameObject[0];
        public AnimationClip playerWalkClip;
        public AnimationClip npcIdleClip;
        public AnimationClip slimeIdleClip;
        public AnimationClip turtleIdleClip;
        public AnimationClip skeletonIdleClip;
    }
}
