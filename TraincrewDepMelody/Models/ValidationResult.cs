namespace TraincrewDepMelody.Models;

/// <summary>
/// バリデーション結果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> MissingEntries { get; set; } = new List<string>();
    public List<string> MissingFiles { get; set; } = new List<string>();
}