using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Api;

/// <summary>
/// テスト用モックAPI
/// </summary>
public class MockTraincrewApi : ITraincrewApi
{
    private bool _isConnected = false;
    private GameStatus _gameStatus = GameStatus.Running;
    private List<string> _occupiedTracks = new List<string>();
    private string _trainNumber = "1262";

    public bool Connect()
    {
        _isConnected = true;
        return true;
    }

    public void Disconnect()
    {
        _isConnected = false;
    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    /// <summary>
    /// データ取得(モックなので何もしない)
    /// </summary>
    public Task FetchData()
    {
        // モック実装では何もしない
        // 実際のAPI実装では、ここでAPI通信を行い内部にデータを保持する
        return Task.CompletedTask;
    }

    /// <summary>
    /// ゲーム状態取得(保持した値を返す)
    /// </summary>
    public GameStatus GetGameStatus()
    {
        return _gameStatus;
    }

    /// <summary>
    /// 在線軌道回路リスト取得(保持した値を返す)
    /// </summary>
    public List<string> GetTrackCircuits()
    {
        return new List<string>(_occupiedTracks);
    }

    /// <summary>
    /// 列番取得(保持した値を返す)
    /// </summary>
    public string GetTrainNumber()
    {
        return _trainNumber;
    }

    // テスト用セッター
    public void SetGameStatus(GameStatus status) => _gameStatus = status;
    public void SetOccupiedTracks(List<string> tracks) => _occupiedTracks = tracks;
    public void SetTrainNumber(string number) => _trainNumber = number;
}