<h2><strong>Features</strong></h2>
<p><strong>Gameplay</strong>
 </p>
<ul> <li>Classic Match-3 mechanics with diagonal gravity and custom board shapes (Square, Ring, Diamond, Hourglass)
 </li><li>Cascading combo system with score multipliers &mdash; chain reactions reward exponentially more points
 </li><li>Configurable fruit variety (5–7 types) and board shape selection in the lobby
 </li><li>Hint system powered by <strong>A* Pathfinding</strong> &mdash; after 10 seconds of inactivity, the best available move is highlighted using graph traversal across the board shape
 </li><li>Automatic board shuffle when no valid moves remain
 </li></ul>
<p><strong>Multiplayer</strong>
 </p>
<ul> <li>Online 1v1 via <strong>Unity Lobby + Relay</strong> &mdash; no dedicated server required
 </li><li><strong>Server-authoritative</strong> architecture &mdash; the host validates all moves, resolves matches, applies gravity and broadcasts results to clients via <strong>Netcode for GameObjects RPC</strong>
 </li><li><strong>Event synchronization</strong> &mdash; clients receive swap, destroy and gravity events in a sequenced animation queue, eliminating race conditions
 </li><li>Lobby browser with create, join, kick and leave functionality
 </li><li>Turn-based system with per-player move counters and live score tracking
 </li></ul>
<p><strong>Architecture</strong>
 </p>
<ul> <li><strong>Clean Architecture</strong> &mdash; Core / Data / Infrastructure / Presentation layers with strict dependency direction
 </li><li><strong>Zenject</strong> dependency injection with ProjectContext, SceneContext and per-scene installers
 </li><li><strong>Object Pooling</strong> &mdash; FruitView pool (120 pre-warmed instances) and ScorePopup pool eliminate runtime allocation during gameplay
 </li><li><strong>Canvas Splitting</strong> &mdash; Static, Dynamic HUD, Board and Overlay canvases separated to minimize unnecessary redraws
 </li><li><strong>DOTween + UniTask</strong> &mdash; all animations are fully async with proper cancellation and tween lifecycle management
 </li></ul>
<p><strong>Visual & Audio</strong>
 </p>
<ul> <li><strong>Post Processing</strong> &mdash; Bloom spikes on fruit destruction, Vignette pulse on combos, Chromatic Aberration scaling with combo multiplier, Camera shake on large clears
 </li><li>Smooth fall, swap, destroy and shuffle animations via DOTween Sequences
 </li><li>Contextual SFX (swap, destroy, button clicks) and looping menu music that fades between scenes
 </li><li>Pulse hint animation with CancellationToken-safe async loop
</li></ul>
