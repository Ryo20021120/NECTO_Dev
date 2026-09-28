using UnityEngine.SceneManagement;

namespace Necto
{
    public static class NectoSceneNames
    {
        public const string Town = "TownScene";
        public const string Field = "FieldScene";
        public const string DebugTown = "NectoDebugTown";
        public const string DebugField = "NectoDebugField";

        public static bool IsTown(Scene scene) => scene.name == Town || scene.name == DebugTown;
        public static bool IsField(Scene scene) => scene.name == Field || scene.name == DebugField;
        public static bool IsDebug(Scene scene) => scene.name == DebugTown || scene.name == DebugField;
        public static string TownForCurrentScene() => IsDebug(SceneManager.GetActiveScene()) ? DebugTown : Town;
        public static string FieldForCurrentScene() => IsDebug(SceneManager.GetActiveScene()) ? DebugField : Field;
    }
}
