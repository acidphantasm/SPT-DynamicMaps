namespace DynamicMaps.Common
{
    public class ConditionData
    {
        public ConditionData(string QuestId, string ConditionId, string ItemId)
        {
            this.QuestId = QuestId;
            this.ConditionId = ConditionId;
            this.ItemId = ItemId;
        }

        public float[] SpawnPoint { get; set; }
        public string MapName { get; set; } = string.Empty;
        public bool IsSet { get; set; }
        public string QuestId { get; }
        public string ConditionId { get; }
        public string ItemId { get; }

        public void SetSpawnPoint(string mapName, float[] spawnPoint)
        {
            if (IsSet) return;
            IsSet = true;
            SpawnPoint = spawnPoint;
            MapName = mapName;
        }
    }
}