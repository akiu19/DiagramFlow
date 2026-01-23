# 実装計画書

## 1. プロジェクト初期設定と基盤構築

### 1.1 パッケージ導入
NuGetパッケージマネージャーを使用して以下のライブラリを導入する。
*   **.NET Framework 4.7.2** と互換性のあるバージョンを選定すること。
    *   `PropertyChanged.Fody`: ViewModelの通知自動化
    *   `ReactiveProperty`: リアクティブプロパティ・コマンド (v8.x系推奨 ※v9以降は.NET 6+の可能性があるため注意)
    *   `Newtonsoft.Json`: データ永続化
    *   `Microsoft.Xaml.Behaviors.Wpf`: Behavior実装用

### 1.2 フォルダ構成
以下のディレクトリ構造を作成する。
*   `Models`: 純粋なデータクラス (DTO/Entity)
*   `ViewModels`: アプリケーションロジック
*   `Views`: XAML, UserControl
*   `Behaviors`: キャンバス操作、ドラッグ操作などの添付ビヘイビア
*   `Services`: Undo/Redo, ファイルIOなどの機能クラス
*   `Converters`: 値変換用

---

## 2. データモデル・ViewModelの実装

### 2.1 Modelレイヤー
JSONシリアライズ対象となるDTOを定義する。
*   `NodeDto`: ID, X, Y, Width, Height, Text, Color
*   `ConnectionDto`: ID, SourceNodeId, SourcePort, TargetNodeId, TargetPort
*   `DiagramDto`: Nodeリスト, Connectionリスト, ZoomLevel

### 2.2 ViewModelレイヤー (基本)
*   `NodeViewModel`:
    *   位置・サイズ・テキストをFodyまたはReactivePropertyで定義。
    *   選択状態フラグ (`IsSelected`)。
*   `ConnectorViewModel`:
    *   接続元・接続先の `NodeViewModel` への参照を持つ。
    *   座標更新ロジックの実装。
*   `MainViewModel`:
    *   `ObservableCollection<NodeViewModel>`
    *   `ObservableCollection<ConnectorViewModel>`
    *   `ScaleValue` (ズーム率)
    *   選択中アイテムの管理

---

## 3. UIレイアウトとCanvas操作の実装 (View/Behaviors)

### 3.1 メイン画面構成 (`MainWindow.xaml`)
*   `Grid` レイアウト
    *   上部：ツールバー (矩形追加ボタン, 保存/開くボタン)
    *   中央：`ScrollViewer` > `Grid` (背景) > `Canvas` (ItemsControlで図形描画)
    *   下部/周辺：ズームスライダー

### 3.2 ズーム・パン機能の実装 (Behaviors)
コードビハインドを避けるため、`Behavior` として実装する。
*   `ZoomBehavior`:
    *   対象: `Canvas` (またはその親 `Grid`) の `LayoutTransform` (`ScaleTransform`)
    *   トリガー: `Ctrl` + マウスホイール, ダブルクリック
    *   機能: マウス位置を中心とした拡大縮小計算。
*   `PanBehavior`:
    *   対象: `ScrollViewer`
    *   トリガー: 右クリックドラッグ
    *   機能: `ScrollToHorizontalOffset` 等を使用したスクロール位置更新。カーソルを `Hand` に変更。

---

## 4. 図形操作の実装

### 4.1 図形描画 (ItemsControl)
*   `Canvas` 上に `ItemsControl` を配置し、`ItemsSource` にNodesをバインド。
*   `ItemContainerStyle` で `Canvas.Left`, `Canvas.Top` をViewModelとバインド。
*   `DataTemplate` で矩形 (`Border`, `TextBox`) を定義。

### 4.2 図形の移動 (MoveBehavior)
*   図形(`Border`)に対するドラッグ操作を検知。
*   ドラッグ量に応じて ViewModel の X, Y を更新。
*   **Undo/Redo対応の準備**: ドラッグ開始時に初期位置を記録し、終了時にコマンドを発行する設計とする。

### 4.3 図形の選択 (Selection)
*   左クリックでの単一選択。
*   `Ctrl`+クリックでの複数選択。
*   `Canvas` 上のドラッグによるラバーバンド選択の実装。

### 4.4 図形のリサイズ
*   選択時にリサイズハンドル (Adorner または Template内のThumb) を表示。
*   Thumbドラッグイベントで Width/Height を更新。
*   最小サイズ (30x30) の制約適用。

---

## 5. 接続線の実装

### 5.1 接続の描画
*   `ItemsControl` (またはNodesと同じItemsControl内のCompositeCollection) でConnectorsを描画。
*   `Line` または `Path` を使用。
*   座標計算: 接続元・先のNodeの位置とサイズから、各ポート(上下左右中央)の座標を動的に計算。

### 5.2 接続操作
*   Nodeテンプレートの4辺に透明なポート（または目印）を配置。
*   ポートからのドラッグ開始で「接続作成モード」へ移行。
*   仮の接続線（ラバーバンド線）を表示。
*   別Nodeのポート上でドロップして接続確定。

---

## 6. Undo/Redoシステムの実装

### 6.1 UndoService
*   2本のスタック (`UndoStack`, `RedoStack`) を管理。
*   `ICommand` パターンを拡張した `QUndoCommand` (仮) インターフェースを定義 (`Execute`, `Unexecute`)。

### 6.2 コマンドの実装
以下の操作をコマンドクラス化する。
*   `AddNodeCommand`
*   `RemoveNodeCommand` / `RemoveConnectionCommand`
*   `MoveNodeCommand` (移動終了時に生成)
*   `ResizeNodeCommand`
*   `ConnectCommand`

---

## 7. ファイル操作と仕上げ

### 7.1 JSON保存/ロード
*   `MainViewModel` の状態を `DiagramDto` に変換してJSON化。
*   ロード時は一旦 ViewModel をクリアして再生成。

### 7.2 その他機能
*   クリップボード (Ctrl+C, Ctrl+V)
*   Deleteキー削除処理
*   アニメーション (ダブルクリックズーム)

---

## 8. 実装順序（推奨）

1.  **Phase 1**: プロジェクト作成、パッケージ導入、データを表示できるだけの最小構成 (Model/VM/View)。
2.  **Phase 2**: キャンバス操作 (ズーム・パン) の実装。
3.  **Phase 3**: 図形移動、選択機能の実装。
4.  **Phase 4**: 接続線の描画と作成機能。
5.  **Phase 5**: Undo/Redoシステムの実装。
6.  **Phase 6**: ファイル保存・読み込み、その他ブラッシュアップ。
