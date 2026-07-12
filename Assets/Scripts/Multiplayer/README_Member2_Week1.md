# Skyfall Arena - Member 2 Week 1 (Local Multiplayer Input)

## Added Scripts

- `LocalPlayerInputFrame.cs`
- `ILocalPlayerInputSource.cs`
- `LocalInputSourceRegistry.cs`
- `LocalPlayerInputSource.cs`
- `LocalInputSubscriber.cs`
- `CharacterRosterConfig.cs`
- `LocalMultiplayerManager.cs`
- `Debug/LocalInputDebugReceiver.cs`

## Unity Setup (No changes to Member 1 scripts required)

1. Open your arena scene (for example `Assets/Scenes/TestArena.unity`).
2. Create an empty GameObject named `LocalMultiplayerManager`.
3. Attach `LocalMultiplayerManager` component.
4. Assign:
   - `Arena Manager`: drag the existing object that has `Environment.ArenaManager`.
   - `Knight Prefab`: `Assets/Prefabs/Knight.prefab`
   - `Ninja Prefab`: `Assets/Prefabs/Ninja.prefab`
   - `Sorcerer Prefab`: assign when a Sorcerer prefab exists.
5. Set `Minimum Players To Start = 2`.
6. Set `Maximum Players` as needed (2-4).
7. (Optional) Enable `Auto Start When Minimum Joined` for fast testing.

## Lobby Controls

### Keyboard Player 1 (WASD split)
- Join: `F`
- Character Prev/Next: `Q` / `E`
- Confirm start: `Enter`
- Leave lobby: `Backspace`

### Keyboard Player 2 (Arrow split)
- Join: `Numpad 0`
- Character Prev/Next: `Numpad 7` / `Numpad 9`
- Confirm start: `Numpad Enter`
- Leave lobby: `Numpad -`

### Gamepad
- Join: `Start`
- Character Prev/Next: `Dpad Left` / `Dpad Right`
- Confirm start: `Start`
- Leave lobby: `Select`

## Runtime Input Contract for Member 1

- Each spawned player gets a `LocalPlayerInputSource` component.
- Member 1 can subscribe via:
  - `LocalInputSourceRegistry.TryGetSource(playerId, out source)`
  - `source.InputFrameReceived += ...`
- For convenience, inherit `LocalInputSubscriber` and override `OnInputFrame(frame)`.

## Quick Parallel Input Test

1. Create two temporary Rigidbody2D objects in scene.
2. Attach `LocalInputDebugReceiver`:
   - Object A: `Player Id = 1`
   - Object B: `Player Id = 2`
3. Play mode:
   - Join P1 with `F`.
   - Join P2 with `Numpad 0`.
   - Start match with `Enter` (or auto-start).
4. Verify both objects move/jump simultaneously without input conflicts.

