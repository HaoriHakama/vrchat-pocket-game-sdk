# Local terminals

The local mode is an optional alternative to the synchronized pool. Each client uses its own local state on **one scene-authored terminal**, regardless of instance player capacity. There is no runtime instantiation, Player Object, synchronized claim table, or `VRCObjectSync`. Existing pooled scenes, installers and game APIs remain available.

## Install the sample

Use the same Unity/Worlds SDK prerequisites and TMP Essential Resources as the pooled sample. Run **Tools > VRC Pocket Game SDK > Install Local Counter Sample**, save the scene, then **Validate Current Scene**. The local and pooled installers use different scene root names and can coexist. Reinstalling replaces only the matching sample root (Undo is available).

Interact with the kiosk to call the terminal; interact again to recall it to the kiosk. Carry it with Pickup, use +1 or Pickup Use, and choose Settings > Save & stow to return it. Confirmation modals block game input and recall. Other players cannot see this terminal or its count. The local sample uses `tentee.pocket.localcounter.v1` PlayerData keys so it does not compete with the pooled counter sample.

## Game integration

1. Prepare program assets with `PocketGameTerminalBuilder.PreparePrograms(typeof(YourLocalGame))`.
2. Add `PocketGameLocalTerminal` to an **active kiosk outside the terminal root**. Add the kiosk collider and configure its backing Udon Interact settings. The controller must remain active while the terminal is stowed to receive `OnPlayerRestored`.
3. Call `PocketGameTerminalBuilder.CreateLocal(parent, name, controller, theme, profile)`. It returns the normal layout slots, UI and `LocalTerminal`; `Session` is null. It creates Rigidbody, Pickup and input relay, but no synchronized session or ObjectSync.
4. Create the game under `parts.GameStateSlot`. For a `PocketGameBehaviour` game, assign `localTerminal = controller`, leave `terminalSession` null, assign `ui = parts.Ui`, and set `controller.gameEvents` to its backing Udon behaviour. Raw event-based games can use `controller.CanUseGameInput()` and `controller.IsLocalClaimant()` directly.
5. All UdonSharp behaviours in the terminal must declare `BehaviourSyncMode.None`. Graph behaviours must use `SyncType.None`. Remove synchronized fields, serialization, deserialization and network event calls from the local game. Converting a synchronized game requires auditing its state and ownership assumptions; changing only the pool size or removing ObjectSync is insufficient.
6. Finish game UI wiring, set `parts.Root.SetActive(false)`, call `CopyToUdon` on the installation root including the kiosk, and run `PocketGameSceneValidator.ValidateLocal(controller)` or `ValidateAll()`.

The validator checks program assets, mutually exclusive controller wiring, root pickup/input/UI references, initial storage state, absence of pool/session/ObjectSync components, game event hooks and local sync modes. It checks structure, not multiplayer convergence or arbitrary game logic.

`PocketGameBehaviour` retains the existing `terminalSession` field and hooks. Its input/claimant helpers route to `localTerminal` when assigned; assigning both fails input authorization. Games remain responsible for gating every UI/input entry point and for pausing their simulation while stowed. Opening a modal does not pause simulation.

## Lifecycle and persistence

The controller waits for the local player's `OnPlayerRestored` before accepting a call. It activates the root, resets UI, enables pickup, and sends `PocketTerminal_OnClaimed`. Games load their own data in that hook; an inactive game may not have received the earlier restore event. No network ownership acquisition is needed to authorize input. Remote player departure, late join and ownership transfer do not change the controller's local claim state.

Recall drops the locally held pickup, moves the root to the kiosk and sends `PocketTerminal_OnRecalled`; it preserves game state. Return approval is synchronous inside `PocketTerminal_RequestReturn`, using `PocketTerminal_ApproveReturn()` or the base class hook. A game that refuses or does not approve stays active. Approval is not carried into later requests.

An approved local return sends `PocketTerminal_OnReturnStarted`, drops/disables pickup, clears the claim and resets UI, sends `PocketTerminal_OnReleased` then `PocketTerminal_OnReturnSucceeded`, and deactivates the root. Both final hooks run before deactivation. Unlike pool returns there are no ownership retries, so the local controller does not emit failed/cancelled return events. Success means local storage completed, **not** that a server acknowledged persistence. Save policy, namespace, schema and errors belong to the game; do not approve if the game needs to keep the terminal open.

PlayerData is still VRChat persistence and has its own synchronization/storage costs. The local presentation is not a promise of zero network traffic. See the [official PlayerData timing requirements](https://creators.vrchat.com/worlds/udon/persistence/player-data/).

## Future spectator extension

This PR implements no remote screen, pose replication or spectator synchronization. Keep game simulation/persistence behind game events and the local input gate. A later game-owned adapter can export a small transient pose/game snapshot to per-player synchronized objects. A separate presentation consumer can render that snapshot without invoking game input, simulation or save hooks. Do not put the full local game into Player Objects merely to show its screen. The transport, payload and spectator lifecycle remain unspecified; no general sync framework is introduced here.

## Cost and validation boundaries

The intended build-side saving is fewer serialized terminal GameObjects/components than a capacity-sized pool. Shared textures, meshes, scripts and other assets may already be deduplicated. Terminal count ratios do not predict build time or download size ratios. Measure Build & Publish separately from runtime rendering, CPU, memory and networking. Each client still loads one full local game; displaying remote screens later would add runtime cost.

For reproducible local checks and outstanding real-client coverage, see `Tests~/Local/README.md`. Pickup motion without ObjectSync is local according to the [official Pickup documentation](https://creators.vrchat.com/worlds/components/vrc_pickup/), but simultaneous VR/Desktop holding, theft prevention and ownership behaviour still require real VRChat clients. ClientSim and static validation cannot establish those outcomes.
