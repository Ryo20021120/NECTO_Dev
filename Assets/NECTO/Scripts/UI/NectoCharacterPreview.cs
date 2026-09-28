using Necto.Animation;
using Necto.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace Necto.UI
{
    /// <summary>見た目選択画面に、選択中の主人公をIdle付きで表示する3Dプレビュー。</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class NectoCharacterPreview : MonoBehaviour
    {
        private const int PreviewLayer = 31;

        [Header("シーン上のプレビュー設備（Inspectorで接続）")]
        [SerializeField] private Transform previewCharacterPosition;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Light previewLight;
        [SerializeField] private RawImage outputImage;
        [SerializeField] private bool autoFrameCharacter = true;
        [SerializeField] private Vector3 modelRotation;
        [SerializeField, Range(0.1f, 1f)] private float framingScale = 1f;
        [Tooltip("オンにするとモデルの反対側から撮影します。モンスタープレビュー用です。")]
        [SerializeField] private bool reverseCameraSide;

        private RenderTexture texture;
        private GameObject stage;
        private GameObject model;
        private int shownSourceId;
        private bool ownsStage;
        private static int nextStageIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPreviewStages() { nextStageIndex = 0; }

        private void Awake()
        {
            // Inspectorで接続したカメラは構図の見本専用。これを有効なままにすると
            // Main Cameraと同じ街全体を画面へ二重描画してしまう。
            if (previewCamera != null)
            {
                previewCamera.enabled = false;
                previewCamera.cullingMask = 1 << PreviewLayer;
                previewCamera.targetTexture = null;
            }
        }

        private void OnEnable()
        {
            if (ownsStage && previewCamera != null) previewCamera.enabled = true;
        }

        private void OnDisable()
        {
            if (previewCamera != null) previewCamera.enabled = false;
        }

        public void Show(GameObject source)
        {
            if (source == null) return;
            EnsureStage();
            int sourceId = source.GetInstanceID();
            if (shownSourceId == sourceId && model != null) return;
            shownSourceId = sourceId;
            if (model != null) Destroy(model);

            model = Instantiate(source, stage.transform);
            model.name = "Preview Model / " + source.name;
            model.transform.localPosition = Vector3.zero;
            // 各プレハブで調整済みの正面方向を保持する。
            Vector3 previewRotation = modelRotation;
            if (reverseCameraSide) previewRotation.y += 180f;
            model.transform.localRotation = Quaternion.Euler(previewRotation) * source.transform.localRotation;
            foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
            model.SetActive(true);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
            SetLayer(model.transform, PreviewLayer);
            NectoCharacterVisualUtility.RepairMaterials(model);
            if (autoFrameCharacter) FitAndFrame(model);
        }

        private void EnsureStage()
        {
            if (stage != null) return;
            texture = new RenderTexture(512, 768, 16, RenderTextureFormat.ARGB32) { name = "NECTO Character Preview" };
            texture.Create();
            if (outputImage == null) outputImage = GetComponent<RawImage>();
            outputImage.color = Color.white;
            outputImage.texture = texture;

            int stageIndex = nextStageIndex++;
            stage = new GameObject($"NECTO Preview Stage {stageIndex}");
            stage.transform.position = new Vector3(5000f + stageIndex * 100f, 5000f, 5000f);
            ownsStage = true;

            Camera cameraTemplate = previewCamera;
            GameObject cameraObject = new GameObject("Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            previewCamera = cameraObject.GetComponent<Camera>();
            if (cameraTemplate != null) previewCamera.CopyFrom(cameraTemplate);
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.orthographic = true;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.targetTexture = texture;
            previewCamera.enabled = isActiveAndEnabled;
            previewCamera.nearClipPlane = .01f;
            previewCamera.farClipPlane = 200f;

            GameObject lightObject = new GameObject("Preview Light", typeof(Light));
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(35f, -35f, 0f);
            previewLight = lightObject.GetComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 1.25f;
            previewLight.cullingMask = 1 << PreviewLayer;
        }

        private void FitAndFrame(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"NECTO: プレビュー対象 {target.name} にRendererがありません。", target);
                return;
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                if (renderers[i].bounds.size.sqrMagnitude > 0f) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.sqrMagnitude <= .0001f)
            {
                Debug.LogWarning($"NECTO: プレビュー対象 {target.name} の表示Boundsを取得できません。", target);
                return;
            }
            target.transform.position += stage.transform.position - bounds.center;
            bounds.center += stage.transform.position - bounds.center;
            float height = Mathf.Max(.5f, bounds.size.y);
            previewCamera.orthographicSize = height * .62f / Mathf.Max(.1f, framingScale);
            previewCamera.transform.position = bounds.center + Vector3.forward * 6f;
            previewCamera.transform.LookAt(bounds.center);
        }

        private static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++) SetLayer(root.GetChild(i), layer);
        }

        private void OnDestroy()
        {
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (model != null) Destroy(model);
            if (ownsStage && stage != null) Destroy(stage);
        }
    }
}
