using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Api
{
    /// <summary>
    /// Traincrew API インターフェース
    /// (実装は別途提供されるDLL/WebSocketライブラリに委ねる)
    /// </summary>
    public interface ITraincrewApi
    {
        /// <summary>
        /// API接続
        /// </summary>
        bool Connect(string endpoint);

        /// <summary>
        /// API切断
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 接続状態確認
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// データ取得(API通信を実行し、内部にデータを保持)
        /// </summary>
        void FetchData();

        /// <summary>
        /// ゲーム状態取得(FetchDataで取得した値を返す)
        /// </summary>
        GameStatus GetGameStatus();

        /// <summary>
        /// 在線軌道回路リスト取得(FetchDataで取得した値を返す)
        /// </summary>
        List<string> GetTrackCircuits();

        /// <summary>
        /// 列番取得(FetchDataで取得した値を返す)
        /// </summary>
        string GetTrainNumber();
    }
}
