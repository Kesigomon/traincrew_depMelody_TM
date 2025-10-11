using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TrainCrew;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Api;

internal class CommandToTrainCrew
{
    public string command { get; init; }
    public string[] args { get; init; }
}

[Serializable]
internal class TraincrewBaseData
{
    public string type { get; init; }
    public object data { get; init; }
}

[Serializable]
internal class TrainCrewState
{
    public string type { get; init; }
    public TrainCrewStateData data { get; init; }
}

[Serializable]
internal class TrainCrewStateData
{
    public List<TrackCircuitData> trackCircuitList { get; init; } = [];
}

[Serializable]
internal class TrackCircuitData
{
    public bool On { get; set; } = false;
    public bool Lock { get; set; } = false;
    public string Last { get; set; } = null;
    public string Name { get; set; } = "";

    public override string ToString()
    {
        return $"{Name}";
    }
}

public class TraincrewApi : ITraincrewApi, IDisposable
{
    private const string DataRequestCommand = "DataRequest";
    private const string ConnectUri = "ws://127.0.0.1:50300/";
    private static readonly string[] DataRequestArgs = ["tconlyontrain"];
    private static readonly Encoding Encoding = Encoding.UTF8;

    private bool _isConnected;
    private string _trainNumber = string.Empty;
    private ClientWebSocket _webSocket = new();
    private List<string> _trackCircuits = [];
    private readonly SemaphoreSlim _fetchDataSemaphore = new(1, 1); 
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

    public async Task FetchData()
    {
        // 既に実行中の場合は待たずに即座にreturn
        if (!await _fetchDataSemaphore.WaitAsync(0))
        {
            return;
        }

        try
        {
            TrainCrewInput.RequestData(DataRequest.Signal);
            var trainState = TrainCrewInput.GetTrainState();
            _trainNumber = trainState.diaName;
            if (TrainCrewInput.gameState.gameScreen
                is not (GameScreen.MainGame or GameScreen.MainGame_Pause))
            {
               return;
            }

            while (_webSocket.State != WebSocketState.Open)
            {
                try
                {
                    await _webSocket.ConnectAsync(new(ConnectUri), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    _webSocket.Dispose();
                    _webSocket = new();
                    return;
                }
                catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
                {
                    _webSocket.Dispose();
                    _webSocket = new();
                }
            }

            if (GetGameStatus() == GameStatus.Running && _webSocket.State == WebSocketState.Open)
            {
                await SendMessages();
                await ReceiveMessages(_trainNumber);
            }
        }
        finally
        {
            _fetchDataSemaphore.Release();
        }
    }

    private async Task SendMessages()
    {
        CommandToTrainCrew requestCommand = new()
        {
            command = DataRequestCommand,
            args = DataRequestArgs
        };

        var json = JsonSerializer.Serialize(requestCommand);
        var bytes = Encoding.GetBytes(json);

        try
        {
            await _webSocket.SendAsync(new(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (WebSocketException)
        {
            _webSocket.Dispose();
        }
    }

    private async Task ReceiveMessages(string trainNumber)
    {
        var buffer = new byte[2048];

        if (_webSocket.State != WebSocketState.Open)
        {
            return;
        }

        List<byte> messageBytes = [];
        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(new(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            messageBytes.AddRange(buffer.Take(result.Count));
        } while (!result.EndOfMessage);

        var jsonResponse = Encoding.GetString(messageBytes.ToArray());
        messageBytes.Clear();

        var traincrewBaseData = JsonSerializer.Deserialize<TraincrewBaseData>(jsonResponse);

        if (traincrewBaseData == null)
        {
            return;
        }

        if (traincrewBaseData.type != "TrainCrewStateData")
        {
            return;
        }

        var dataJsonElement = (JsonElement)traincrewBaseData.data;
        var trainCrewStateData = JsonSerializer.Deserialize<TrainCrewStateData>(dataJsonElement.GetRawText());

        if (trainCrewStateData == null)
        {
            return;
        }

        _trackCircuits = trainCrewStateData
            .trackCircuitList
            .Where(trackCircuit => trackCircuit.Last == trainNumber)
            .Select(trackCircuit => trackCircuit.Name)
            .ToList();
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
        return _trackCircuits.ToList();
    }

    public string GetTrainNumber()
    {
        return _trainNumber;
    }

    public void Dispose()
    {
        TrainCrewInput.Dispose();
        _webSocket.Dispose();
        _fetchDataSemaphore.Dispose();
    }
}