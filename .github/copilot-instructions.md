# DiagramFlow の Copilot インストラクション

## プロジェクト概要

DiagramFlow は、スクロール可能なキャンバス上に図形を配置し、ダイアグラム図を作成・閲覧・操作できる WPF ベースのデスクトップアプリケーションです。本アプリケーションは、ズーム・パン機能を含むキャンバス操作、図形操作、Undo/Redo 機能に重点を置いています。

## 技術スタック

- **フレームワーク**: .NET Framework 4.7.2
- **言語**: C# 7.3
- **UI 技術**: WPF (Windows Presentation Foundation)
- **対象 OS**: Windows 10/11
- **必須ライブラリ**:
  - Fody.PropertyChanged: ViewModel のプロパティ変更通知自動化のため
  - ReactiveProperty: リアクティブなプロパティ・コマンド定義のため

## プロジェクト構成

```
DiagramFlow/
├── DiagramFlow/          # メインアプリケーションプロジェクト
│   ├── App.xaml          # アプリケーション定義
│   ├── MainWindow.xaml   # メインウィンドウ UI
│   ├── MainWindow.xaml.cs # メインウィンドウコードビハインド
│   ├── ViewModels/       # ViewModel クラス
│   └── Properties/       # アセンブリ情報とリソース
├── Documents/            # プロジェクトドキュメント（日本語）
│   ├── RequirementsDefinition.md
│   ├── DiagramEditorSpecification.md
│   └── ImplementationPlan.md
└── DiagramFlow.slnx      # ソリューションファイル
```

## ビルドとテスト

### プロジェクトのビルド
```bash
# MSBuild でビルド（Visual Studio Developer Command Prompt から）
msbuild DiagramFlow.slnx /p:Configuration=Release
```

### テスト
- 現在、自動テストインフラストラクチャは構築されていません
- 手動テストは以下に重点を置いてください：
  - キャンバスズーム操作（Ctrl + マウスホイール）
  - パン操作（右クリック + ドラッグ）
  - ダブルクリックズーム機能
  - ズームスライダーの同期

## コーディング規約

### 一般的なガイドライン

1. **MVVM パターン**: Model-View-ViewModel パターンを厳密に遵守してください
   - View は XAML ファイルで定義します
   - ViewModel はビジネスロジックと UI 状態を処理します
   - データバインディングを使用して View と ViewModel を接続します

2. **プロパティ変更通知**: Fody.PropertyChanged を使用して INotifyPropertyChanged を自動実装します
   - ViewModel は適切な属性で装飾する必要があります
   - 必要な場合を除き、手動でプロパティ変更通知を行わないでください

3. **リアクティブプロパティ**: ReactiveProperty をプロパティとコマンドに使用します
   - 例: `public ReactiveProperty<double> ZoomScale { get; }`

4. **イベント処理**:
   - 直接的な UI イベントハンドラー（マウスイベントなど）にはコードビハインドを優先します
   - ビジネスロジックを含むユーザーアクションには Command を使用します
   - ViewModel にアクセスする際は常に null 参照をチェックしてください

5. **命名規則**:
   - クラス名、メソッド、プロパティには PascalCase を使用します
   - プライベートフィールド: アンダースコア + camelCase（例: `_isPanning`, `_lastMousePosition`）
   - 目的を説明する意味のある名前を使用します

### WPF 固有の規約

1. **レイアウト**: ダイアグラムエディターには `ScrollViewer` と `Canvas` を使用します
2. **変換**: ズーム操作には `ScaleTransform` を使用します
3. **マウス操作**:
   - 左クリック: 図形の選択と移動
   - 右クリック + ドラッグ: パン操作
   - Ctrl + マウスホイール: ズームイン/アウト
   - ダブルクリック: ズームの切り替え
4. **変換後の UI 更新**: スケール変更後に必要に応じて `UpdateLayout()` を呼び出します

### コードスタイル

- 明確性のため `var` の代わりに明示的な型を使用してください
- public メソッドとプロパティには XML ドキュメントコメントを追加してください
- メソッドは焦点を絞り、小さく保ってください（単一責任）
- エッジケースを処理してください（例: ズーム値を 0.1 から 4.0 の間にクランプ）

## 重要な制約事項

1. **使用禁止**:
   - .NET Core または .NET 6+（.NET Framework 4.7.2 を使用する必要があります）
   - 外部 UI コントロールライブラリ（DevExpress、Infragistics など）
   - 標準機能は WPF 組み込みコントロールのみを使用してください

2. **必須使用**:
   - プロパティ変更通知には Fody.PropertyChanged
   - リアクティブプロパティとコマンドには ReactiveProperty

## 一般的なタスク

### 新しい ViewModel の追加
1. `ViewModels` フォルダーにクラスを作成します
2. 自動通知のために Fody.PropertyChanged 属性を使用します
3. ReactiveProperty を使用してプロパティを定義します
4. DataContext バインディングを介して View に接続します

### ズーム/パン動作の変更
1. `Documents/RequirementsDefinition.md` の要件を確認します
2. イベントハンドラーのために `MainWindow.xaml.cs` コードビハインドを更新します
3. 必要に応じて ViewModel プロパティを更新します
4. マウスホイールとスライダーの同期でテストします

### 新しい図形の追加
1. 適切なフォルダーに図形クラスを定義します
2. Canvas レンダリングロジックを更新します
3. 選択と操作ロジックを実装します
4. Undo/Redo への影響を考慮します

## パフォーマンスに関する考慮事項

- ズームとパン操作は UI のフリーズなく応答性を保つ必要があります
- 高頻度なマウスホイールイベントを効率的に処理してください
- パフォーマンス問題を避けるため `UpdateLayout()` は慎重に使用してください

## 将来の拡張機能（現在のスコープ外）

- 図形の選択・複数選択
- Undo/Redo 機能
- 図形データの保存・読み込み
- タッチ操作対応
- 図形間の接続線
- ミニマップ表示

## ドキュメント言語について

`Documents/` フォルダー内のほとんどのドキュメントは日本語で記述されています。主な要件は以下の通りです：
- キャンバスズーム機能（最小: 10%, 最大: 400%）
- 右クリックドラッグによるパン
- アニメーション付きダブルクリックズーム
- ズームスライダーの同期

## 新しい貢献者向けスタートガイド

1. WPF ワークロードがインストールされた Visual Studio を用意してください
2. Visual Studio で `DiagramFlow.slnx` を開いてください
3. NuGet パッケージ（Fody.PropertyChanged、ReactiveProperty）を復元してください
4. アプリケーションをビルドして実行してください
5. 詳細な要件については `Documents/` フォルダー内のドキュメントを確認してください
