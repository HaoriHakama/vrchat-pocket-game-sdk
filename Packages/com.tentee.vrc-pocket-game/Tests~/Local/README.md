# Local mode validation

Use a disposable VCC Worlds project, Unity 2022.3.22f1, Input Handling **Both**, and TMP Essential Resources. Include UDON, VRC_SDK_VRCSDK3, UDONSHARP and VRC_ENABLE_PLAYER_PERSISTENCE in the build target's scripting defines. Install this checkout as a local package. Never run these scene-replacing tests in an authored world.

Copy `PocketGameLocalValidation.cs` and `PocketGameLocalSmoke.cs` to `Assets/Editor`, and `PocketGameLocalSmokeDriver.cs` and `PocketGameLocalReturnProbe.cs` to `Assets` (outside Editor). For the existing pooled regression smoke, copy `Tests~/ClientSim/PocketGameSmokeDriver.cs` to `Assets` too.

Run Unity with `-batchmode -nographics -projectPath <project> -logFile <log>` and:

- `-executeMethod PocketGameLocalValidation.RunBatch -quit`: installs both samples; validates initially stowed state; rejects ObjectSync, conflicting UI wiring, missing game program and synchronized Graph; prints GameObject/component counts of each complete installation root (including inactive children).
- `-executeMethod PocketGameLocalSmoke.RunBatch` **without -quit**: runs actual Udon in ClientSim. Checks stowed input rejection, call, game input, Pickup Use event routing after Udon Start, modal input/recall blocking, recall without losing count, refused/stale approval, request/started/released/succeeded order before deactivation, storage and re-call loading PlayerData. The probe deliberately approves/refuses returns. The save namespace is unique to the test run. The driver exits Unity with 0/1.
- `-executeMethod VrcPocketGame.Editor.PocketGameClientSimSmoke.RunBatch` **without -quit**: existing pooled regression suite, using its existing driver.

These are single-editor simulations. Event injection does not test controller hardware, raycast usability, real pickup holding, multiple VRChat clients, or persistence round trips to VRChat servers.

## Real-client checklist (not verified by these tests)

Use two VRChat clients in one instance, including a non-master. Each must call a terminal concurrently and carry it to a different place. Increment separately, recall and stow one while the other continues. Check VR Pickup Use and Desktop pickup/use/uGUI controls. Join late, leave with each player/master, and change ownership while the other is holding/playing; local input must remain available. Confirm another player cannot see, move, steal or stow the local terminal. Test refused returns, reset confirmation, save restoration on rejoin and failure/loading states in the game's persistence implementation. Repeat the pooled sample regression independently.

Measure build duration and output size separately under matching Unity/SDK/assets/target/build settings and cold/warm cache conditions. Structure counts alone are not a build benchmark. Measure runtime CPU, rendering, memory and traffic independently.
