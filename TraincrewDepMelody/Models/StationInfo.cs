namespace TraincrewDepMelody.Models
{
    /// <summary>
    /// 駅情報
    /// </summary>
    public class StationInfo
    {
        public string StationName { get; set; } = string.Empty;
        public int Platform { get; set; }
        public bool IsOddPlatform => Platform % 2 == 1;

        public override string ToString()
        {
            return $"{StationName} {Platform}番線";
        }
    }
}
