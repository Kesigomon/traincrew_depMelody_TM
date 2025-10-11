using TrainCrew;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Api;

public class TraincrewApi : ITraincrewApi
{
    private bool _isConnected;
    private string _trainNumber = string.Empty; 
    public bool Connect()
    {
        TrainCrewInput.Init();
        _isConnected = true;
        return true;
    }

    public void Disconnect()
    {
        TrainCrewInput.Dispose();
        _isConnected = false;
    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    public void FetchData()
    {
        var trainState = TrainCrewInput.GetTrainState();
        _trainNumber = trainState.diaName;
    }

    public GameStatus GetGameStatus()
    {
        return TrainCrewInput.gameState.gameScreen switch
        {
            GameScreen.MainGame => GameStatus.Running,
            GameScreen.MainGame_Pause => GameStatus.Paused,
            _ => GameStatus.Stopped
        };
    }

    public List<string> GetTrackCircuits()
    {
        throw new NotImplementedException();
    }

    public string GetTrainNumber()
    {
        return _trainNumber;
    }
}