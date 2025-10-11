# 乗降促進アプリ API連携設計書

**バージョン**: 1.0
**作成日**: 2025年10月11日
**対象システム**: Traincrew 乗降促進アプリ

---

## 1. API概要

### 1.1 API提供方式

Traincrew APIは2つの方式で提供されます。

| 提供方式 | 説明 | 実装担当 |
|---------|-----|---------|
| DLL方式 | C#から直接呼び出し可能なDLL | 別途実装 |
| WebSocket方式 | WebSocket経由でリアルタイム通信 | 別途実装 |

**注意**: 本設計書では、APIのメソッドシグネチャのみを定義し、実装は別途提供されるDLL/WebSocketライブラリに委ねます。

### 1.2 API提供情報

Traincrew APIから取得する情報は以下の通りです。

| 情報 | データ型 | 説明 | 更新頻度 |
|-----|---------|-----|---------|
| ゲーム状態 | 列挙型 | 実行中/ポーズ中/終了 | 100ms |
| 在線軌道回路リスト | 文字列リスト | 列車が在線している軌道回路ID一覧 | 100ms |
| 列番 | 文字列 | 現在操作中の列車の列番 | 100ms |

---

## 2. APIインターフェース仕様

### 2.1 ITraincrewApi インターフェース

```csharp
/// <summary>
/// Traincrew API インターフェース
/// (実装は別途提供されるDLL/WebSocketライブラリに委ねる)
/// </summary>
public interface ITraincrewApi
{
    /// <summary>
    /// API接続
    /// </summary>
    /// <param name="endpoint">APIエンドポイント (例: "http://localhost:8080")</param>
    /// <returns>接続成功: true, 失敗: false</returns>
    bool Connect(string endpoint);

    /// <summary>
    /// API切断
    /// </summary>
    void Disconnect();

    /// <summary>
    /// 接続状態確認
    /// </summary>
    /// <returns>接続中: true, 未接続: false</returns>
    bool IsConnected();

    /// <summary>
    /// データ取得（API通信を実行し、内部にデータを保持）
    /// このメソッドが実際にAPI通信を行い、取得した値をクラス内部に保持する。
    /// GetGameStatus, GetTrackCircuits, GetTrainNumberは保持された値を返すだけ。
    /// </summary>
    /// <exception cref="ApiConnectionException">API接続エラー</exception>
    void FetchData();

    /// <summary>
    /// ゲーム状態取得（FetchDataで取得した値を返す）
    /// </summary>
    /// <returns>ゲーム状態</returns>
    GameStatus GetGameStatus();

    /// <summary>
    /// 在線軌道回路リスト取得（FetchDataで取得した値を返す）
    /// </summary>
    /// <returns>軌道回路ID一覧 (例: ["SB-01", "SB-02", "SB-03"])</returns>
    List<string> GetTrackCircuits();

    /// <summary>
    /// 列番取得（FetchDataで取得した値を返す）
    /// </summary>
    /// <returns>列番 (例: "1262", "回1301A")</returns>
    string GetTrainNumber();
}
```

### 2.2 GameStatus 列挙型

```csharp
/// <summary>
/// ゲーム状態
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// 実行中 (運転中)
    /// </summary>
    Running,

    /// <summary>
    /// ポーズ中
    /// </summary>
    Paused,

    /// <summary>
    /// 停止 (運転終了、またはゲーム終了)
    /// </summary>
    Stopped
}
```

### 2.3 ApiConnectionException 例外

```csharp
/// <summary>
/// API接続エラー例外
/// </summary>
public class ApiConnectionException : Exception
{
    public ApiConnectionException(string message) : base(message) { }
    public ApiConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
```

---

## 3. TraincrewApiClient クラス

### 3.1 クラス設計

```csharp
/// <summary>
/// Traincrew API クライアント
/// (ITraincrewApi の実装をラップし、エラーハンドリングとリトライ処理を提供)
/// </summary>
public class TraincrewApiClient
{
    #region フィールド
    private ITraincrewApi _api;
    private ILogger _logger;
    private string _endpoint;

    // リトライ設定
    private const int MaxRetryCount = 3;
    private const int RetryDelayMs = 1000;
    private int _consecutiveFailures = 0;
    #endregion

    #region コンストラクタ
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="api">Traincrew API実装</param>
    /// <param name="logger">ロガー</param>
    /// <param name="endpoint">APIエンドポイント</param>
    public TraincrewApiClient(ITraincrewApi api, ILogger logger, string endpoint)
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
            _logger.Info($"Connecting to Traincrew API: {_endpoint}");

            var success = _api.Connect(_endpoint);

            if (success)
            {
                _logger.Info("Connected to Traincrew API");
                _consecutiveFailures = 0;
            }
            else
            {
                _logger.Error("Failed to connect to Traincrew API");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during API connection: {_endpoint}", ex);
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
            _logger.Info("Disconnected from Traincrew API");
        }
        catch (Exception ex)
        {
            _logger.Error("Exception during API disconnection", ex);
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
            _logger.Error("Exception during IsConnected check", ex);
            return false;
        }
    }

    /// <summary>
    /// データ取得 (リトライ処理付き)
    /// API通信を実行し、内部にデータを保持
    /// </summary>
    public void FetchData()
    {
        ExecuteWithRetry(() =>
        {
            _api.FetchData();
            return true;
        }, false);
    }

    /// <summary>
    /// ゲーム状態取得
    /// FetchDataで取得した値を返すだけ
    /// </summary>
    public GameStatus GetGameStatus()
    {
        try
        {
            return _api.GetGameStatus();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to get game status", ex);
            return GameStatus.Stopped;
        }
    }

    /// <summary>
    /// 在線軌道回路リスト取得
    /// FetchDataで取得した値を返すだけ
    /// </summary>
    public List<string> GetTrackCircuits()
    {
        try
        {
            return _api.GetTrackCircuits();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to get occupied tracks", ex);
            return new List<string>();
        }
    }

    /// <summary>
    /// 列番取得
    /// FetchDataで取得した値を返すだけ
    /// </summary>
    public string GetTrainNumber()
    {
        try
        {
            return _api.GetTrainNumber();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to get train number", ex);
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
            catch (ApiConnectionException ex)
            {
                _consecutiveFailures++;

                _logger.Warn($"API call failed (attempt {attempt + 1}/{MaxRetryCount}): {ex.Message}");

                if (attempt < MaxRetryCount - 1)
                {
                    // 最後の試行以外はリトライ
                    Thread.Sleep(RetryDelayMs);
                }
                else
                {
                    // 最大リトライ回数に達した
                    _logger.Error($"API call failed after {MaxRetryCount} attempts. Returning default value.");

                    // 連続失敗が一定数を超えたら警告
                    if (_consecutiveFailures >= 5)
                    {
                        _logger.Error($"API connection unstable: {_consecutiveFailures} consecutive failures");
                    }

                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected exception during API call: {ex.Message}", ex);
                return defaultValue;
            }
        }

        return defaultValue;
    }
    #endregion
}
```

---

## 4. API呼び出しパターン

### 4.1 ポーリング方式

本アプリケーションでは、100ms間隔でAPIをポーリングして状態を取得します。

```csharp
public class ModeManager
{
    private DispatcherTimer _pollingTimer;
    private TraincrewApiClient _apiClient;

    public void StartPolling()
    {
        _pollingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };

        _pollingTimer.Tick += OnPollingTick;
        _pollingTimer.Start();

        _logger.Info("API polling started (100ms interval)");
    }

    private void OnPollingTick(object sender, EventArgs e)
    {
        // API情報取得（通信実行）
        _apiClient.FetchData();

        // データ読み出し
        var gameStatus = _apiClient.GetGameStatus();
        var occupiedTracks = _apiClient.GetTrackCircuits();
        var trainNumber = _apiClient.GetTrainNumber();

        // 状態更新
        UpdateState(gameStatus, occupiedTracks, trainNumber);
    }

    public void StopPolling()
    {
        _pollingTimer?.Stop();
        _logger.Info("API polling stopped");
    }
}
```

### 4.2 接続管理

```csharp
public class ApplicationBootstrap
{
    private TraincrewApiClient _apiClient;
    private ILogger _logger;

    public void Initialize()
    {
        // API接続
        if (!_apiClient.Connect())
        {
            _logger.Error("Failed to connect to Traincrew API");

            // エラーダイアログ表示
            MessageBox.Show(
                "Traincrewとの接続に失敗しました。\nTraincrewが起動しているか確認してください。",
                "接続エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            // アプリ終了 (または接続なしで起動)
            // Application.Current.Shutdown();
        }
    }

    public void Shutdown()
    {
        // API切断
        _apiClient.Disconnect();
    }
}
```

---

## 5. データ取得仕様

### 5.1 ゲーム状態取得

#### 5.1.1 取得タイミング

- 100ms間隔のポーリング

#### 5.1.2 使用箇所

- 音声再生制御 (ポーズ時に一時停止、再開時に再生)
- Topmost制御 (PlayingOnlyモード時に使用)
- モード切替 (Stopped時に車両モードに強制切替)

#### 5.1.3 エラー時の動作

- デフォルト値: `GameStatus.Stopped`
- 音声を停止し、安全側に倒す

---

### 5.2 在線軌道回路リスト取得

#### 5.2.1 取得タイミング

- 100ms間隔のポーリング

#### 5.2.2 データ形式

```csharp
List<string> occupiedTracks = new List<string> { "SB-01", "SB-02", "SB-03" };
```

#### 5.2.3 使用箇所

- 駅在線判定 (StationRepository.FindStation)
- デバッグパネル表示

#### 5.2.4 在線判定ロジック

```csharp
public StationInfo FindStation(List<string> occupiedTracks)
{
    if (occupiedTracks == null || occupiedTracks.Count == 0)
    {
        return null;
    }

    var occupiedSet = new HashSet<string>(occupiedTracks);

    foreach (var (stationPlatform, trackSet) in _stationTracks)
    {
        if (occupiedSet.SetEquals(trackSet))
        {
            return new StationInfo
            {
                StationName = stationPlatform.StationName,
                Platform = stationPlatform.Platform
            };
        }
    }

    return null;
}
```

#### 5.2.5 エラー時の動作

- デフォルト値: `new List<string>()`
- 駅在線なしとして扱う

---

### 5.3 列番取得

#### 5.3.1 取得タイミング

- 100ms間隔のポーリング

#### 5.3.2 データ形式

```csharp
string trainNumber = "1262";  // または "回1301A"
```

#### 5.3.3 使用箇所

- 上下線判定 (車両メロディー選択)
- デバッグパネル表示

#### 5.3.4 上下線判定ロジック

```csharp
public Direction DetermineDirection(string trainNumber)
{
    if (string.IsNullOrEmpty(trainNumber))
    {
        _logger.Warn("Train number is empty. Defaulting to Up.");
        return Direction.Up;
    }

    // 数字部分を抽出
    var match = Regex.Match(trainNumber, @"\d+");

    if (match.Success)
    {
        var lastDigit = int.Parse(match.Value[^1].ToString());
        var direction = lastDigit % 2 == 0 ? Direction.Up : Direction.Down;

        _logger.Info($"Train number: {trainNumber}, Last digit: {lastDigit}, Direction: {direction}");

        return direction;
    }

    _logger.Warn($"Failed to parse train number: {trainNumber}. Defaulting to Up.");
    return Direction.Up;
}
```

**判定例**:

| 列番 | 数字部分 | 最後の桁 | 偶奇 | 方向 |
|-----|---------|---------|-----|-----|
| 1262 | 1262 | 2 | 偶数 | 上り |
| 1261 | 1261 | 1 | 奇数 | 下り |
| 回1302A | 1302 | 2 | 偶数 | 上り |
| 回1301A | 1301 | 1 | 奇数 | 下り |
| 試運転 | (なし) | - | - | 上り (デフォルト) |

#### 5.3.5 エラー時の動作

- デフォルト値: `string.Empty`
- 上下線判定時にデフォルト (`Direction.Up`) を使用

---

## 6. API連携シーケンス

### 6.1 アプリ起動時

```
Application   Bootstrap   ApiClient   TraincrewApi   ModeManager
    │             │           │             │             │
    │─起動────────→│           │             │             │
    │             │─Connect()→│             │             │
    │             │           │─Connect()──→│             │
    │             │           │◄────────────│             │
    │             │◄──────────│             │             │
    │             │           │             │             │
    │             │─Start                                 │
    │             │  Polling()─────────────────────────→  │
    │             │           │             │             │
    │             │           │             │             │◄─Timer(100ms)
    │             │           │             │             │
    │             │           │◄────GetGameStatus()───────│
    │             │           │─────────────→│            │
    │             │           │◄─────────────│            │
    │             │           │                           │
    │             │           │◄────GetTrackCircuits()───│
    │             │           │─────────────→│            │
    │             │           │◄─────────────│            │
    │             │           │                           │
    │             │           │◄────GetTrainNumber()──────│
    │             │           │─────────────→│            │
    │             │           │◄─────────────│            │
    │             │           │                           │
```

### 6.2 ポーリング中のエラー発生

```
ModeManager   ApiClient   TraincrewApi
    │             │             │
    │─GetGame     │             │
    │  Status()──→│             │
    │             │─GetGame     │
    │             │  Status()──→│
    │             │             │ (タイムアウト)
    │             │             │
    │             │─Retry(1/3)─→│
    │             │             │ (タイムアウト)
    │             │             │
    │             │─Retry(2/3)─→│
    │             │             │ (タイムアウト)
    │             │             │
    │             │─Retry(3/3)─→│
    │             │             │ (タイムアウト)
    │             │             │
    │             │─Log Error   │
    │             │─Return      │
    │             │  Default    │
    │◄────────────│  (Stopped)  │
    │             │             │
```

---

## 7. エラーハンドリング

### 7.1 エラー種別と対処

| エラー種別 | 原因 | 対処方法 |
|-----------|-----|---------|
| 接続エラー | Traincrewが起動していない | エラーダイアログ表示、アプリ終了 (または接続なしで起動) |
| タイムアウト | API応答が遅い | リトライ (最大3回)、デフォルト値返却 |
| データ不正 | APIのレスポンスが不正 | エラーログ出力、デフォルト値返却 |
| 接続不安定 | 連続して5回以上失敗 | 警告ログ出力、デバッグパネルに警告表示 |

### 7.2 リトライ戦略

```csharp
// リトライ設定
private const int MaxRetryCount = 3;
private const int RetryDelayMs = 1000;  // 1秒

// 指数バックオフ (将来拡張)
private int GetRetryDelay(int attempt)
{
    return RetryDelayMs * (int)Math.Pow(2, attempt);  // 1秒, 2秒, 4秒, ...
}
```

### 7.3 フォールバック値

| API | デフォルト値 | 理由 |
|-----|------------|------|
| GetGameStatus() | `GameStatus.Stopped` | 安全側に倒す (音声停止) |
| GetTrackCircuits() | `new List<string>()` | 駅在線なしとして扱う |
| GetTrainNumber() | `string.Empty` | 上下線判定でデフォルト (上り) を使用 |

---

## 8. パフォーマンス考慮事項

### 8.1 ポーリング間隔

- **現在**: 100ms (10Hz)
- **理由**: リアルタイム性と負荷のバランス
- **将来拡張**: WebSocket方式でプッシュ通知に変更すれば、ポーリング不要

### 8.2 API呼び出し最適化

```csharp
// 一度に全ての情報を取得 (複数回API呼び出しを避ける)
private void OnPollingTick(object sender, EventArgs e)
{
    // バッチ取得 (実装依存)
    var state = _apiClient.GetAllState();  // 将来拡張

    // または個別取得
    var gameStatus = _apiClient.GetGameStatus();
    var occupiedTracks = _apiClient.GetTrackCircuits();
    var trainNumber = _apiClient.GetTrainNumber();
}
```

### 8.3 キャッシュ戦略

現在の取得情報は全てリアルタイムで変化する可能性があるため、キャッシュは使用しません。

---

## 9. セキュリティ

### 9.1 接続先制限

- **制限**: ローカルホスト (`localhost`, `127.0.0.1`) のみ接続を許可
- **理由**: 外部からの不正アクセスを防止

```csharp
public bool Connect(string endpoint)
{
    var uri = new Uri(endpoint);

    // ローカルホスト以外は拒否
    if (!IsLocalhost(uri.Host))
    {
        _logger.Error($"Connection to non-localhost is not allowed: {uri.Host}");
        throw new SecurityException($"Connection to non-localhost is not allowed: {uri.Host}");
    }

    return _api.Connect(endpoint);
}

private bool IsLocalhost(string host)
{
    return host == "localhost" ||
           host == "127.0.0.1" ||
           host == "::1";
}
```

### 9.2 タイムアウト設定

- **タイムアウト**: 5秒
- **理由**: ハングを防止

---

## 10. デバッグ機能

### 10.1 API接続状態の表示

デバッグパネルにAPI接続状態を表示します。

| 状態 | 表示 |
|-----|-----|
| 接続中 | `✓ 接続中` (緑) |
| 未接続 | `✗ 未接続` (赤) |
| 接続不安定 | `⚠ 接続不安定` (黄) |

### 10.2 API呼び出しログ

```csharp
_logger.Info($"API Call: GetGameStatus() -> {gameStatus}");
_logger.Info($"API Call: GetTrackCircuits() -> [{string.Join(", ", occupiedTracks)}]");
_logger.Info($"API Call: GetTrainNumber() -> {trainNumber}");
```

---

## 11. 将来拡張

### 11.1 WebSocket方式への移行

現在のポーリング方式から、WebSocketによるプッシュ通知方式に変更することで、以下のメリットがあります。

- リアルタイム性の向上
- ポーリング負荷の削減
- ネットワーク帯域の削減

```csharp
public interface ITraincrewApi
{
    // イベント駆動
    event EventHandler<GameStatusChangedEventArgs> GameStatusChanged;
    event EventHandler<OccupiedTracksChangedEventArgs> OccupiedTracksChanged;
    event EventHandler<TrainNumberChangedEventArgs> TrainNumberChanged;

    // データ取得は引き続きFetchData方式
    void FetchData();
}
```

### 11.2 バッチ取得API

複数の情報を一度に取得できるAPIがあれば、API呼び出し回数を削減できます。

```csharp
public class TraincrewState
{
    public GameStatus GameStatus { get; set; }
    public List<string> OccupiedTracks { get; set; }
    public string TrainNumber { get; set; }
}

public interface ITraincrewApi
{
    // FetchDataで一括取得する方式に変更可能
    void FetchData();  // 内部で全データを一括取得

    TraincrewState GetAllState();  // 保持した全データを一括で返す
}
```

---

## 12. テスト用モックAPI

### 12.1 MockTraincrewApi

```csharp
/// <summary>
/// テスト用モックAPI
/// </summary>
public class MockTraincrewApi : ITraincrewApi
{
    private bool _isConnected = false;
    private GameStatus _gameStatus = GameStatus.Running;
    private List<string> _occupiedTracks = new List<string>();
    private string _trainNumber = "1262";

    public bool Connect(string endpoint)
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
    /// データ取得（モックなので何もしない）
    /// </summary>
    public void FetchData()
    {
        // モック実装では何もしない
        // 実際のAPI実装では、ここでAPI通信を行い内部にデータを保持する
    }

    /// <summary>
    /// ゲーム状態取得（保持した値を返す）
    /// </summary>
    public GameStatus GetGameStatus()
    {
        return _gameStatus;
    }

    /// <summary>
    /// 在線軌道回路リスト取得（保持した値を返す）
    /// </summary>
    public List<string> GetTrackCircuits()
    {
        return new List<string>(_occupiedTracks);
    }

    /// <summary>
    /// 列番取得（保持した値を返す）
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
```

### 12.2 使用例

```csharp
// テスト時
var mockApi = new MockTraincrewApi();
var apiClient = new TraincrewApiClient(mockApi, logger, "mock://localhost");

// テストシナリオ設定
mockApi.SetGameStatus(GameStatus.Running);
mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02", "SB-03" });
mockApi.SetTrainNumber("1262");

// テスト実行
apiClient.FetchData();  // データ取得
var gameStatus = apiClient.GetGameStatus();  // 保持した値を取得
Assert.AreEqual(GameStatus.Running, gameStatus);
```

---

**改訂履歴**

| バージョン | 日付 | 改訂者 | 改訂内容 |
|-----------|------|--------|----------|
| 1.0 | 2025-10-11 |  | 初版作成 |
