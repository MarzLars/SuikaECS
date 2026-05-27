## Main Plan: Suika-style Async Merge Game (ECS)

TL;DR
Use Unity Entities (ECS) + Unity Physics + Input System + a seed-based match flow to build an async, competitive, mobile-first merging/stacking game. Only focus on local mobile player logic in this first implementation. Use Unity Physics for physics stacking. Use EntityCommandBuffer for entity creation and DOTS-friendly IComponentData for game state.  No async multiplayer logic should be implemented, only add comments in locations of the code that will be needed to be modified when async netcode implementation is ready.  Matchmaking returns a spawn seed/sequence; clients run the same sequence locally and upload final score.

**Steps**
1. Discovery & Docs (completed)
   - Confirmed Unity 6000.5 manual docs are indexed; scriptref/API coverage is limited. (Status checked via unity-docs)
2. Project setup (pending)
   - Add/verify packages: `com.unity.entities`, `com.unity.inputsystem`, `com.unity.netcode` (or custom backend client), URP optional.
   - Create InputAction asset for gyroscope + accelerometer fallbacks for editor.
3. Core prototype (parallelizable)
   - Implement deterministic SpawnSequence service (seeded RNG) that returns an ordered stream of item descriptors.
   - Create simple representation for items (value, size, merge-level, visual-id).
4. ECS architecture & systems (implementation)
   - Use EntityCommandBuffer and burst where applicable; keep managed components out of hot loops.
5. Input mapping & mobile integration (parallel with prototype)
   - Implement Input System bindings for `Gyroscope` and `Accelerometer`; also provide editor keyboard fallback (A/D or arrow keys for aim, Space for drop).
   - Map tilt to horizontal aim position; map shake detection to drop.
6. Async multiplayer & match flow
   - Match flow: client requests match -> server returns seed + optional config -> client runs local run -> client uploads final score -> server resolves winner and reward percent.
   - Seed mechanics: seed -> deterministic item sequence and RNG for tie-breakers (avoid any nondet runtime values).
7. Backend & persistence (MVP)
   - Minimal matchmaking service with endpoints: request_match, submit_score, get_leaderboard, get_player_pb.
   - Store player PBs locally and on server.
8. Testing & validation
   - No unit tests.
9. Polishing & extra features (post-MVP)
   - Leaderboards, cosmetic skins, monetization hooks, better visuals, audio.


**Verification**
1. Build a small editor prototype (keyboard  and click controls) that runs the same seeded sequence twice and verifies identical scoring using the deterministic placement algorithm.

**Key Decisions / Recommendations**
- ECS choice: Use `IComponentData` + `SystemBase`/`ISystem` depending on performance needs. Prefer `ISystem` + Burst for hot loops, but `SystemBase` is easier for prototyping. Avoid managed fields inside `ISystem`.
- Spawning: Use EntityCommandBuffer and baker/prefab workflow for visuals; create native-only data in components to keep ECS burstable.

**Further Considerations (short list)**
1. Payouts: model only in-game virtual currency.
