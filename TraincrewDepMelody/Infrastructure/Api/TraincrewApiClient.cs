using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Api;

/// <summary>
/// Traincrew API クライアント
/// (ITraincrewApi の実装をラップし、エラーハンドリングとリトライ処理を提供)
/// </summary>
public class TraincrewApiClient
{
    #region フィールド
    private readonly ITraincrewApi _api;
    private readonly ILogger<TraincrewApiClient> _logger;
    private readonly string _endpoint;

    // リトライ設定
    private const int MaxRetryCount = 3;
    private const int RetryDelayMs = 1000;
    private int _consecutiveFailures = 0;
    #endregion

    #region コンストラクタ
    public TraincrewApiClient(ITraincrewApi api, ILogger<TraincrewApiClient> logger, string endpoint)
    {
        _api = api;
        _logger = logger;
        _endpoint = endpoint;
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// API接続
    /// </summary>
    public bool Connect()
    {
        try
        {
            _logger.LogInformation("Connecting to Traincrew API");

            var success = _api.Connect();

            if (success)
            {
                _logger.LogInformation("Connected to Traincrew API");
                _consecutiveFailures = 0;
            }
            else
            {
                _logger.LogError("Failed to connect to Traincrew API");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during API connection: {Endpoint}", _endpoint);
            return false;
        }
    }

    /// <summary>
    /// API切断
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _api.Disconnect();
            _logger.LogInformation("Disconnected from Traincrew API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during API disconnection");
        }
    }

    /// <summary>
    /// 接続状態確認
    /// </summary>
    public bool IsConnected()
    {
        try
        {
            return _api.IsConnected();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during IsConnected check");
            return false;
        }
    }

    /// <summary>
    /// データ取得 (リトライ処理付き)
    /// </summary>
    public async Task FetchData()
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await _api.FetchData();
            return true;
        }, false);
    }

    /// <summary>
    /// ゲーム状態取得
    /// </summary>
    public GameStatus GetGameStatus()
    {
        try
        {
            return _api.GetGameStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game status");
            return GameStatus.Stopped;
        }
    }

    /// <summary>
    /// 在線軌道回路リスト取得
    /// </summary>
    public List<string> GetTrackCircuits()
    {
        try
        {
            return _api.GetTrackCircuits();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get occupied tracks");
            return new List<string>();
        }
    }

    /// <summary>
    /// 列番取得
    /// </summary>
    public string GetTrainNumber()
    {
        try
        {
            return _api.GetTrainNumber();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get train number");
            return string.Empty;
        }
    }
    #endregion

    #region プライベートメソッド
    /// <summary>
    /// リトライ処理を伴うAPI呼び出し
    /// </summary>
    private T ExecuteWithRetry<T>(Func<T> apiCall, T defaultValue)
    {
        for (int attempt = 0; attempt < MaxRetryCount; attempt++)
        {
            try
            {
                var result = apiCall();

                // 成功したら連続失敗カウントをリセット
                _consecutiveFailures = 0;

                return result;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;

                _logger.LogWarning("API call failed (attempt {Attempt}/{MaxRetry}): {Message}", attempt + 1, MaxRetryCount, ex.Message);

                if (attempt < MaxRetryCount - 1)
                {
                    // 最後の試行以外はリトライ
                    Thread.Sleep(RetryDelayMs);
                }
                else
                {
                    // 最大リトライ回数に達した
                    _logger.LogError("API call failed after {MaxRetry} attempts. Returning default value.", MaxRetryCount);

                    // 連続失敗が一定数を超えたら警告
                    if (_consecutiveFailures >= 5)
                    {
                        _logger.LogError("API connection unstable: {ConsecutiveFailures} consecutive failures", _consecutiveFailures);
                    }

                    return defaultValue;
                }
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// リトライ処理を伴う非同期API呼び出し
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> apiCall, T defaultValue)
    {
        for (var attempt = 0; attempt < MaxRetryCount; attempt++)
        {
            try
            {
                var result = await apiCall();

                // 成功したら連続失敗カウントをリセット
                _consecutiveFailures = 0;

                return result;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;

                _logger.LogWarning("API call failed (attempt {Attempt}/{MaxRetry}): {Message}", attempt + 1, MaxRetryCount, ex.Message);

                if (attempt < MaxRetryCount - 1)
                {
                    // 最後の試行以外はリトライ
                    await Task.Delay(RetryDelayMs);
                }
                else
                {
                    // 最大リトライ回数に達した
                    _logger.LogError("API call failed after {MaxRetry} attempts. Returning default value.", MaxRetryCount);

                    // 連続失敗が一定数を超えたら警告
                    if (_consecutiveFailures >= 5)
                    {
                        _logger.LogError("API connection unstable: {ConsecutiveFailures} consecutive failures", _consecutiveFailures);
                    }

                    return defaultValue;
                }
            }
        }

        return defaultValue;
    }
    #endregion
}