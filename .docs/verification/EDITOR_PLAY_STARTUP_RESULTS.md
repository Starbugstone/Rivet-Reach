# Editor Play startup correction — 2026-09-08

The user opened Main and pressed Play correctly, but saw only the blue sky. The Editor log reported `MissingComponentException` in `AvatarView.CreateAnimation` while setting `Animator.applyRootMotion`. This interrupted `Expedition.Awake` during player creation, before the interface was initialized.

The missing-component fallback used `GetComponent<Animator>() ?? AddComponent<Animator>()`. In the Editor, Unity's missing-component sentinel bypassed the CLR null-coalescing fallback. The Windows player checks did not expose this Editor-specific failure. [AvatarView.cs](../../Assets/RivetReach/Code/Player/AvatarView.cs) now uses `TryGetComponent` and adds an Animator when the component is absent. Existing model geometry, clips, skins and import settings are unchanged. [Unity's component API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/GameObject.TryGetComponent.html) documents this presence check.

## Reproduce the regression check

Open the saved `Assets/RivetReach/Scenes/Main.unity` in Unity 6000.4.4f1, outside Play mode, and choose **Rivet Reach → Verify Editor Play startup**. Alternatively, the existing local build-request poller accepts `verify-editor` in `Logs/build-request.txt`. The [Editor-only verifier](../../Assets/RivetReach/Editor/EditorPlayVerification.cs) enters and exits Play twice, without preparing assets, rebuilding the player, changing scenes or changing the saved appearance selection. It refuses to begin with unsaved scene edits or an active Play session.

Each cycle checks ordinary Bootstrap, the title canvas and Start Expedition button, the player's body/hand/portrait animation graphs, and repeated Animator creation for both models, both skins and full-body/hand views. It invokes the title button, waits for usable spawn terrain, verifies the Game-view camera, then briefly drives movement and mining animation. It records errors through domain reloads and captures the title and world. Reports go to `Logs/EditorPlayVerification/`.

Ordinary Play never enables this verification. Stop Play and allow compilation to finish before restarting after a runtime code change; recompiling a running session does not recreate its nonserialized gameplay state.

## Observed result

The existing Windows Unity 6000.4.4f1 Editor completed **two Play/Stop cycles and all 60 checks**, with no errors or exceptions recorded during those cycles. The current project has normal scene/domain reload enabled. [Result](editor-play-result.txt) · [Individual checks](editor-play-checks.txt).

The updated Windows development build also succeeded with **zero errors/warnings**, and the domain suite passed **22,371 assertions**. All **37 standalone character checks** passed again, including both animation graphs, all model/skin combinations, mining and movement transitions. [Build/domain result](editor-play-build.txt) · [Character result](editor-play-avatar-report.json).

The actual Game-view captures show the menu and usable terrain with first-person hands. These are Editor captures, separate from the earlier standalone demo:

![Editor title menu](editor-play-title.png)

![Editor gameplay after Start Expedition](editor-play-world.png)

This checks Editor startup and the listed animation/gameplay transitions. It does not measure performance, test hot reloading an active session, or repeat the full terrain/inventory suite.
