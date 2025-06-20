namespace CryoFall.SaveSystem
{
    public class SaveGameData
    {
        public string PlayerCurrentRoomId { get; set; }
        public List<string> PlayerInventoryIds { get; set; } = new();
        public List<RoomSaveData> Rooms { get; set; } = new();
        public bool IsTutorialCompleted { get; set; }
        public List<string> VisitedRoomIds { get; set; } = new();
    }

    public class RoomSaveData
    {
        public string RoomId { get; set; }
        public bool IsLocked { get; set; }
        public List<string> ItemIdsInRoom { get; set; } = new();
    }    
}
