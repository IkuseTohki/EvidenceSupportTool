# クラス図

このドキュメントは、エビデンスサポートツールのクラス構造を定義します。

```mermaid
classDiagram
    direction LR

    class MainWindow {
        <<View>>
        -void Window_Drop(object sender, DragEventArgs e)
        -void Window_DragOver(object sender, DragEventArgs e)
    }

    class MainViewModel {
        <<ViewModel>>
        +string StatusText
        +ICommand StartMonitoringCommand
        +ICommand StopMonitoringCommand
        +ICommand DropSettingFileCommand
        -IMonitoringService monitoringService
        -IConfigService configService
        -IUserInteractionService userInteractionService
    }

    class IMonitoringService {
        <<Interface>>
        +void Start()
        +void Stop()
        +event Action~string~ StatusChanged
    }

    class MonitoringService {
        <<Service>>
        -IConfigService configService
        -IUserInteractionService userInteractionService
        -IEvidenceExtractionService evidenceExtractionService
        -IScreenshotService screenshotService
        +void Start()
        +void Stop()
        +event Action~string~ StatusChanged
    }

    class IScreenshotService {
        <<Interface>>
        +void Start(string savePath)
        +void Stop()
    }

    class ScreenshotService {
        <<Service>>
        +void Start(string savePath)
        +void Stop()
    }

    class IEvidenceExtractionService {
        <<Interface>>
        +void CreateSnapshot(string snapshotPath, IEnumerable~MonitoringTarget~ targets)
        +void ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath)
    }

    class EvidenceExtractionService {
        <<Service>>
        +void CreateSnapshot(string snapshotPath, IEnumerable~MonitoringTarget~ targets)
        +void ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath)
    }

    class IConfigService {
        <<Interface>>
        +AppSettings GetAppSettings()
        +IEnumerable~MonitoringTarget~ GetMonitoringTargets()
        +IEnumerable~CollectTarget~ GetCollectTargets()
        +void LoadSettings(string filePath)
    }

    class ConfigService {
        <<Service>>
        -IniParser parser
        -string settingsFilePath
        +AppSettings GetAppSettings()
        +IEnumerable~MonitoringTarget~ GetMonitoringTargets()
        +IEnumerable~CollectTarget~ GetCollectTargets()
        +void LoadSettings(string filePath)
    }

    class IUserInteractionService {
        <<Interface>>
        +void ShowMessage(string message)
        +void ShowError(string message)
    }

    class AppSettings { <<Model>> }
    class MonitoringTarget { <<Model>> }
    class IniParser { <<Helper>> }
    class IniData { <<DTO>> }
    class ICommand { <<Interface>> }


    MainWindow --|> MainViewModel : DataContext
    MainViewModel ..> IMonitoringService : Uses
    MainViewModel ..> IConfigService : Uses
    MainViewModel ..> IUserInteractionService : Uses
    MainViewModel ..> ICommand : Uses

    MonitoringService ..|> IMonitoringService : Implements
    MonitoringService ..> IConfigService : Uses
    MonitoringService ..> IUserInteractionService : Uses
    MonitoringService ..> IEvidenceExtractionService : Uses
    MonitoringService ..> IScreenshotService : Uses

    ScreenshotService ..|> IScreenshotService : Implements

    EvidenceExtractionService ..|> IEvidenceExtractionService : Implements

    ConfigService ..|> IConfigService : Implements
    ConfigService ..> IniParser : Uses
    IniParser ..> IniData : Creates

```

## クラス詳細

### Views

- **MainWindow**:
  - アプリケーションのメインウィンドウ（UI）です。
  - `DataContext`として`MainViewModel`を持ち、UI 要素を ViewModel のプロパティやコマンドにバインドします。
  - ウィンドウへのドラッグ＆ドロップ（`Drop` / `DragOver`）イベントをハンドリングし、ドロップされたファイルパスを `MainViewModel.DropSettingFileCommand` に渡します。

### ViewModels

- **MainViewModel**:
  - UI の状態とロジックを管理します。
  - `IUserInteractionService` を通じて、ユーザーへの通知（メッセージやエラーダイアログの表示）を要求します。
  - `StatusText`: UI に表示される現在のステータス（"待機中", "監視中"など）です。
  - `StartMonitoringCommand`: 監視開始ボタンにバインドされるコマンドです。実行されると`IMonitoringService`の`Start`メソッドを呼び出します。
  - `StopMonitoringCommand`: 監視停止ボタンにバインドされるコマンドです。実行されると`IMonitoringService`の`Stop`メソッドを呼び出します。
  - `DropSettingFileCommand`: 設定ファイルのドラッグ＆ドロップ時に実行されるコマンドです。渡されたファイルパスの妥当性を検証し、`IConfigService.LoadSettings` を呼び出して設定を動的に反映します。

### Models

- **AppSettings**:
  - `setting.ini`の`[Settings]`セクションに対応するデータモデルです。
  - `EvidenceSavePath`: エビデンスの保存先パスを保持します。
  - `KeepSnapshot`: スナップショットを保持するかどうかの設定を保持します。
  - `AutoSaveScreenshots`: クリップボードの画像を自動保存するかどうかの設定を保持します。
- **MonitoringTarget**:
  - `setting.ini`の`[Targets]`セクションの個々のエントリに対応するデータモデルです。
  - `Name`: ターゲット名（出力フォルダ名）を保持します。
  - `PathPattern`: 監視対象のパス（ワイルドカードや日付フォーマットを含む）を保持します。
- **IniData**:
  - `IniParser`によってパースされた INI ファイルの生データを保持するデータ転送オブジェクト(DTO)です。
  - `Sections`: セクション名をキーとし、そのセクション内のキーと値の辞書を値として持つ、ネストされた辞書構造です。

### Services

- **IUserInteractionService** (Interface):
  - ユーザーへの通知（ダイアログ表示など）を行うための契約を定義します。これにより、ビジネスロジック層が UI 層に直接依存することを防ぎます。
  - `ShowMessage(string message)`: 情報メッセージを表示します。
  - `ShowError(string message)`: エラーメッセージを表示します。
- **IConfigService** (Interface):
  - 設定情報を取得するための契約を定義します。
  - `GetAppSettings()`: `AppSettings`オブジェクトを取得します。
  - `GetMonitoringTargets()`: `MonitoringTarget`のコレクションを取得します。
  - `GetCollectTargets()`: `CollectTarget`のコレクションを取得します。
  - `LoadSettings(string filePath)`: 指定されたパスの設定ファイルを読み込み、内部設定を更新します。
- **ConfigService**:
  - `IConfigService`の実装クラスです。
  - `IniParser`を利用して INI ファイルの内容を解析し、その結果を`AppSettings`や`MonitoringTarget`といった、アプリケーションで扱いやすいモデルオブジェクトに変換する責務を持ちます。
  - `LoadSettings(string filePath)`: 設定ファイルパスを更新し、次回以降の設定取得に反映します。また指定ファイルが存在し内容が妥当かを検証します。
- **IMonitoringService** (Interface):
  - ログ監視のライフサイクル管理に関する契約を定義します。
  - `Start()`: 監視プロセスを開始します。
  - `Stop()`: 監視プロセスを停止し、差分を検出・保存します。
  - `StatusChanged`: ステータスの変更を ViewModel に通知するためのイベントです。
- **IScreenshotService** (Interface):
  - クリップボード監視によるスクリーンショット保存の契約を定義します。
  - `Start(string savePath)`: クリップボードの監視を開始し、指定されたパスに画像を保存するようにします。
  - `Stop()`: クリップボードの監視を停止します。
- **ScreenshotService**:
  - `IScreenshotService`の実装クラスです。
  - クリップボードを監視し、画像がコピーされた際にファイルとして保存する責務を持ちます。
- **MonitoringService**:
  - `IMonitoringService`の実装クラスです。監視フローの制御に責任を持ちます。
  - `IEvidenceExtractionService` を利用して、スナップショットの作成や差分抽出などの具体的なファイル操作を委譲します。
  - `IScreenshotService` を利用して、監視期間中のスクリーンショット自動保存を制御します。
  - 差分がなかった場合や、処理中にエラーが発生した場合は、`IUserInteractionService` を介してユーザーへの通知を要求します。

- **IEvidenceExtractionService** (Interface):
  - エビデンスの抽出に関する具体的なファイル操作の契約を定義します。
  - `CreateSnapshot(string snapshotPath, IEnumerable<MonitoringTarget> targets)`: 指定されたパスに、監視対象のスナップショットを作成します。
  - `ExtractEvidence(string snapshot1Path, string snapshot2Path, string evidencePath)`: 2 つのスナップショットを比較し、差分を指定されたパスに保存します。
- **EvidenceExtractionService**:
  - `IEvidenceExtractionService`の実装クラスです。
  - スナップショットの作成、ファイル比較、差分ファイルの保存など、実際のファイルシステム操作に責任を持ちます。

### Helpers

- **IniParser**:
  - INI ファイルを解析する責務を持つヘルパークラスです。
  - 特定のモデル（`AppSettings`など）には依存せず、汎用的な`IniData`オブジェクトを生成します。
