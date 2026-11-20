# AI coding guide

## この変更の開発方針

- 作業前に目的、前提、矛盾、失敗時、境界条件、互換性、完了条件を点検する。
- 同期プール方式の既存APIと割当規則を維持する。ローカル方式の状態を同期プールへ混ぜない。
- SDKは端末のライフサイクルと入力許可を担当し、ゲーム進行、保存形式、観戦用データはゲーム側の責務とする。
- UI・操作フローは具体的なプレビューを提示し、承認後に実装する。独立した内部処理と調査は承認待ちでも進める。
- 実装者から独立したレビュー専用エージェントで設計と差分を点検し、指摘、対応、残課題を記録する。
- ローカル検証を優先する。GitHub Actionsを調査や試行のために繰り返し実行しない。実機未検証を明示し、構造検査で動作確認を代用しない。
- Unityの.metaを追跡する。Library、Temp、生成ビルドをコミットしない。
- 日本語の報告、提案、PR本文はユーザー全体のyomiyasu skillで推敲する。skill自体や推敲記録をSDK配布物へ含めない。読み手はエンジニアのてんてーさんとし、必要な技術用語を使う。
- クラスまたは機能のまとまりごとに実装、検証、コミットする。必要な関連ファイルと.metaを同じコミットに含める。
- コミットメッセージは日本語で変更内容と目的を示す。コードには設計意図、制約、同期・ライフサイクル上の注意を日本語で残し、逐語説明を増やさない。
- 不要な整形、命名変更、生成物更新を機能変更へ混ぜない。
- 既定ブランチへの直接push、force push、マージ、リリースは禁止する。PR送信は完成した差分と本文に対するユーザー承認後に行う。

既存の以下の同期・所有権の不変条件は同期プール方式に適用する。ローカル方式はネットワーク所有権を入力権限の根拠にしない。

Source of truth and edit boundary: lifecycle/authority is `Packages/com.tentee.vrc-pocket-game/Runtime/Core`; common visual state is `Packages/com.tentee.vrc-pocket-game/Runtime/UI`; game rules, persistence, and sync payload are game-owned; optional sample code stays in the package's `Runtime/Sample` and `Editor/Sample`; generic construction stays in `Editor/Install`. Do not make Core reference Sample, a game type, a game key, or genre-specific UI. Do not turn builders into a generic framework beyond their current terminal/UI responsibilities.

Preserve these invariants: pool-only claim writes; exact authoritative `(slot, claimant, generation)` identity; terminal root/session/game ownership travel together; game input goes through `session.CanUseGameInput()`; return approval is synchronous inside the request event; PlayerData keys use a stable gameId namespace, never a pool slot. Keep overlay, external drawer, and modal state distinct; modal input never pauses simulation.

Verification mapping: run `python Tools/verify-package.py` for package boundaries; `PrepareProgramAssetsBatch` for generated Udon assets; `ValidateCurrentScene` or `InstallAndValidateBatch` for scene wiring. For ClientSim copy the smoke driver from the package's `Tests~` directory into the validation project's `Assets`, set Input Handling to Both, use a normal VCC Worlds project with `UDON`, `VRC_SDK_VRCSDK3`, `UDONSHARP`, and `VRC_ENABLE_PLAYER_PERSISTENCE`, and run without `-quit`.
For validator fault checks, copy `Tests~/Validation/PocketGameValidatorFaultDriver.cs` into the validation project's `Assets` and run `PocketGameValidatorFaultDriver.RunBatch`.

Do not reintroduce removed slot APIs, edit generated assets under Packages, claim unsupported Udon limitations, or add agents for routine work. Keep changes narrow and readable.

Optional public game metadata is specified in `Packages/com.tentee.vrc-pocket-game/docs/GAME_MANIFEST.md` and `game-manifest.schema.json`. When creating or editing a game's `pocket-game.json`, use the example, keep its catalog `id` stable, separate catalog translations from in-game `languages`, and state only verified platform/input support. Do not infer catalog approval from a manifest. Keep actual pool size and PlayerData save schema outside this public manifest; `installation.recommendedPoolSize` is advisory.
