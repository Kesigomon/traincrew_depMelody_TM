# Traincrew 乗降促進アプリ (Departure Melody App)

鉄道シミュレーター「Traincrew」用の乗降促進メロディー再生アプリケーションです。

## 概要

このアプリケーションは、Traincrewゲーム内で駅や車両のメロディー・アナウンスを自動再生する補助ツールです。
駅モードと車両モードがあり、適切な音声を流します。

## 機能

### 主要機能
- **駅モード**: 駅スピーカーから流れる発車メロディーとドア締まりますアナウンスの再生
- **車両モード**: 車両スピーカーから流れるメロディーループ再生
- **マルチチャンネル音声再生**: 駅と車両の音声を独立して制御
- **プロファイル管理**: CSV形式で音声ファイルをカスタマイズ可能
- **設定画面**: 音量、API接続、ウィンドウ設定などを管理

### 技術仕様
- **フレームワーク**: .NET 8.0 + WPF
- **アーキテクチャ**: レイヤードアーキテクチャ (Infrastructure / Application / Presentation)
- **デザインパターン**: MVVM, Repository, State
- **ログ**: Microsoft.Extensions.Logging を使用したファイルログ

## プロジェクト構造

```
TraincrewDepMelody/
├── Models/                      # データモデルと列挙型
│   ├── Enums.cs
│   ├── StationInfo.cs
│   ├── ApplicationState.cs
│   └── AppSettings.cs
├── Infrastructure/              # インフラストラクチャ層
│   ├── Api/                     # API通信
│   │   ├── ITraincrewApi.cs
│   │   ├── MockTraincrewApi.cs
│   │   └── TraincrewApiClient.cs
│   ├── Repositories/            # データリポジトリ
│   │   ├── StationRepository.cs
│   │   ├── AudioRepository.cs
│   │   └── ProfileLoader.cs
│   ├── Settings/                # 設定管理
│   │   └── SettingsManager.cs
│   └── Logging/                 # ログ
│       └── FileLoggerProvider.cs
├── Application/                 # アプリケーション層
│   ├── Audio/                   # 音声再生
│   │   └── AudioPlayer.cs
│   └── Modes/                   # モード管理
│       ├── IMode.cs
│       ├── StationMode.cs
│       ├── VehicleMode.cs
│       └── ModeManager.cs
├── Presentation/                # プレゼンテーション層
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   └── Views/
│       └── SettingsWindow.xaml
├── MainWindow.xaml              # メインウィンドウ
├── appsettings.json             # アプリケーション設定
├── profiles/                    # 音声プロファイル
│   └── profile_default.csv
├── stations/                    # 駅定義
│   └── stations.csv
└── sounds/                      # 音声ファイル配置場所
    ├── vehicle/
    ├── station/
    └── door/
```

## セットアップ

### 必要な環境
- .NET 8.0 SDK
- Windows 10/11 (WPF対応)

### ビルド方法

```bash
cd TraincrewDepMelody
dotnet build
```

### 実行方法

```bash
dotnet run
```

または、ビルド後の実行ファイルを直接起動:
```
TraincrewDepMelody\bin\Debug\net8.0-windows\Traincrew_depMelody_M.exe
```

## 使用方法

### 初回セットアップ

1. **音声ファイルの配置**
   - `sounds/` ディレクトリに音声ファイル（MP3）を配置
   - 詳細は `sounds/README.txt` を参照

2. **駅定義の設定**
   - `stations/stations.csv` で駅と軌道回路のマッピングを定義

3. **音声プロファイルの設定**
   - `profiles/profile_default.csv` で音声ファイルのマッピングを定義

### 操作方法

- **ボタンクリック** or **スペースキー長押し**: メロディー再生
- **ボタンリリース** or **スペースキー離す**: ドア締まりますアナウンス再生
- **右クリックメニュー**: 設定画面を開く / アプリ終了

### モードの動作

#### 車両モード
- 通常時はこのモードで動作
- ボタン押下: 車両メロディーをループ再生
- ボタンリリース: メロディー停止 → ドア締まりますアナウンス再生

#### 駅モード
- 駅停車中にボタンを押すと自動的に切り替わる
- 初回押下: 駅メロディー再生 → 自動的にドア締まりますアナウンス再生
- 2回目以降: 車両モードと同じ動作
- 駅発車時: 自動的に車両モードに戻る

## 設定ファイル

### appsettings.json
アプリケーション全体の設定を管理します。

```json
{
  "ApiEndpoint": "http://localhost:8080",
  "CurrentProfile": "profiles/profile_default.csv",
  "ProfileFile": "profile_default.csv",
  "StationDefinition": "stations/stations.csv",
  "Topmost": "Always",
  "Volume": 0.8,
  "EnableKeyboard": true
}
```

### profiles/profile_default.csv
音声ファイルのマッピングを定義します。

```csv
種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,sounds/vehicle/melody_up.mp3
車両メロディー,,,下り,sounds/vehicle/melody_down.mp3
車両ドア締まります,,,,sounds/door/door_train.mp3
駅メロディー,サンプル駅,1,,sounds/station/sample_01.mp3
駅ドア締まります,,,奇数,sounds/door/door_odd.mp3
```

### stations/stations.csv
駅と軌道回路のマッピングを定義します。

```csv
駅名,番線,軌道回路1,軌道回路2,軌道回路3
サンプル駅,1,SAMPLE-01,SAMPLE-02,
サンプル駅,2,SAMPLE-03,SAMPLE-04,
```

## 開発情報

### 依存パッケージ
- **CsvHelper**: CSVファイルの読み込み
- **Newtonsoft.Json**: JSON設定ファイルの処理
- **Microsoft.Extensions.Logging**: ログ機能

### ログ出力
ログファイルは `log/` ディレクトリに出力されます。
ファイル名: `yyyyMMddHHmmss.txt`

### API連携
`TraincrewApi` クラスで実装済みです。
- **TrainCrewInput.dll**: ゲーム状態と列番の取得
- **WebSocket (ws://127.0.0.1:50300/)**: 軌道回路データの取得
- **モック実装**: `MockTraincrewApi` がテスト用に利用可能

## トラブルシューティング

### 音声ファイルが再生されない
- `sounds/` ディレクトリに音声ファイルが配置されているか確認
- `profiles/profile_default.csv` のパスが正しいか確認
- ログファイルでエラーメッセージを確認

### 設定が保存されない
- `appsettings.json` に書き込み権限があるか確認
- 設定画面で「OK」ボタンをクリックしているか確認

### ビルドエラー
- .NET 8.0 SDK がインストールされているか確認
- `dotnet --version` でバージョンを確認

## ライセンス
このプロジェクトは個人利用・研究目的で作成されました。

## 作成者
Claude Code による自動実装
