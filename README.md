# Unity LLM組み込みゲーム

For Honor風を採用した3Dアクション戦闘にLLMでリアルタイム判断を行うAIを組み込んでみるプロジェクト。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


### 環境
- **Unity:** Unity6.2（6000.2.2f1）
- **対象プラットフォーム:** Windows PC

### 使用LLM関連情報
- **API:** LLMUnity
- **使用モデル:** 未定（1-2Bのモデル予定）

## アーキテクチャ
![概略図](https://github.com/cushionA/LearningAIGame/blob/main/%E4%BB%95%E6%A7%98%E6%9B%B8/%E6%88%A6%E9%97%98%E3%82%B7%E3%82%B9%E3%83%86%E3%83%A0%E6%A6%82%E7%95%A5%E5%9B%B3.png?raw=true)

### 現在の主要クラス
| クラス名 | 役割 | 責任 |
|---------|------|------|
| `StateSystem` | 状態管理 | 全システムの状態を一元管理 |
| `BattleCharacterController` | 統合制御 | システム間の調整と実行制御 |
| `BaseSystem<T>` | 基底システム | UniRx→R3によるリアクティブ |
| `CharacterSetting` | キャラクター設定 | ScriptableObjectでキャラの振る舞いを決める |

### システム構成図
```
LLM (AI判断システム)
    ↓ パラメータ補正
StateSystem (状態管理)
    ↑ 行動報告・状態判定
BaseSystem<T>継承クラス
    ↑ 実行インターフェース
BattleCharacterController (統合制御)
```
### Git関連メモ
- **直接mainにはpushできません。**
- **mainへの変更統合時はプルリクエスト必須です**
- **リポジトリへのアクセス権限が切れた場合は座布団までお声掛けください**
- **アセットを含むプロジェクトのため、privateリポジトリです**

## プロジェクト構造

```
Assets/Scripts/
├── Core/                           # 核となるシステム
│   ├── BattleCharacterController.cs
│   ├── StateSystem.cs
│   └── Base/BaseSystem.cs
├── Systems/                        # 各機能システム
│   ├── MovementSystem.cs
│   ├── AttackSystem.cs
│   ├── DefenseSystem.cs
│   └── EnergySystem.cs
├── Controllers/                    # 制御クラス
│   ├── PlayerController.cs
│   └── AIController.cs
├── Data/                          # データ定義
│   └── CharacterSettings.cs
└── Utilities/                     # ユーティリティ
    └── CombatUtilities.cs
```

### 開発ガイドライン

- **Git規約:** [リンク](https://github.com/cushionA/LearningAIGame/blob/main/%E4%BB%95%E6%A7%98%E6%9B%B8/GitHub%E9%81%8B%E7%94%A8%E3%83%9E%E3%83%8B%E3%83%A5%E3%82%A2%E3%83%AB.md)
- **コードスタイル:** Unity標準 + EditorConfig準拠
- **コーディング規約:** [リンク](https://github.com/cushionA/LearningAIGame/blob/main/%E4%BB%95%E6%A7%98%E6%9B%B8/%E3%82%B3%E3%83%BC%E3%83%87%E3%82%A3%E3%83%B3%E3%82%B0%E8%A6%8F%E7%B4%84.md)

## ライセンス
このプロジェクトは [MIT License](LICENSE) の下で公開されています。

## 外部ソース

このプロジェクトは以下のアセット・ライブラリを使用しています：

- [UniTask](https://github.com/Cysharp/UniTask) - 非同期処理ライブラリ
- [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes) - インスペクター拡張
- [NuGet For Unity](https://github.com/GlitchEnzo/NuGetForUnity) - パッケージ管理システム
- [R3](https://github.com/Cysharp/R3) - 次世代リアクティブライブラリ（NuGet For Unity経由でインストール）
- [UniRx](https://github.com/neuecc/UniRx) - リアクティブプログラミング（R3移行後削除予定）
- [LitMotion](https://github.com/AnnulusGames/LitMotion) - 高性能Tweenライブラリ
- [Robot Kyle (URP)](https://assetstore.unity.com/packages/3d/characters/robots/robot-kyle-urp-4696) - 3Dキャラクターモデル

