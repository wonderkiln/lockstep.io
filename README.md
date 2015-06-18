## Lockstep.IO

A drop in Unity and NodeJS, Socket.IO based "Lockstep" implementation to support rapid development of online games in Unity.

#### What is Lockstepping?

Starcraft, Age of Empires and Warcraft 3 all use deterministic lockstepping - not this particular library, rather the same idea. Lockstepping forces all user input to be broadcast over the network to be executed roughly 200ms into the future; Literally when you click to move a unit, there will always be a 200ms delay before the unit responds to your input. This 200ms, known as the "latency window" provides enough time for that command to reach all other networked players, then for that command to execute in synch across everyone's simulation of the game.

#### What is Deterministic Lockstepping?

Physics simulation in games is actually a pretty "fuzzy" science, where numbers don't really need to be dead accurate, just "close enough" to look real. Normally this isn't a problem, but when we need to run multiple identical simulations on different machines and hardware the "fuzz" starts to become a serious problem.

#### Is Unity Deterministic?

#### Is Unreal Deterministic?

No. Although we don't currently use the Unreal Engine, Unreal does also suffer from non-deterministic physics.
