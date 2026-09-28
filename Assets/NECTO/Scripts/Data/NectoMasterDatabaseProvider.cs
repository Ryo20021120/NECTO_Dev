using UnityEngine;

namespace Necto.Data
{
    [DefaultExecutionOrder(-10000)]
    public sealed class NectoMasterDatabaseProvider : MonoBehaviour
    {
        [SerializeField] private NectoMasterDatabase database;
        public NectoMasterDatabase Database => database;

        private void Awake()
        {
            NectoMasterDatabase.SetCurrent(database);
        }
    }
}
