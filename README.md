## Lockstep.IO

A drop in Unity and NodeJS, Socket.IO based "Lockstep" implementation to support rapid development of online games in Unity.

#### Quick Start

1. Install NodeJS from https://nodejs.org/ if you don't already have the latest version.

2. Create a new folder for your project, then from the command line initialize the project using:

```
npm init .
```

3. NPM will walk you through setting up the project details, and create a `package.json` file for you with the project details and dependencies.

4. Add LockstepIO as a dependency to your NodeJS project by entering the following into the command line:

```
npm install lockstep.io --save
```


#### What is Lockstepping?

Starcraft, Age of Empires and Warcraft 3 all use lockstepping - not this particular library, rather the same idea. Lockstepping forces all user input to be broadcast over the network to be executed roughly 200ms into the future; Literally when you click to move a unit, there will always be a 200ms delay before the unit responds to your input. This 200ms, known as the "latency window" provides enough time for that command to reach all other networked players, then for that command to execute in synch across everyone's simulation of the game.

#### What is Deterministic Lockstepping?

Physics simulation in *most* games is a "fuzzy" science, where numbers don't always need to be dead accurate, just "close enough" to look realistic. Normally this isn't a problem and the speed trade off pays for itself, but when we need to run multiple identical simulations on different machines (ie. a multiplayer game), that "fuzziness" starts to become a serious issue: 

```
If in Player A's simulation the character just dodged the bullet in time, but in Player B's simulation the character didn't dodge that same bullet in time - strictly due to fuzziness - our two simulations are no longer in sync and we aren't sharing a game state any longer. We call this non-deterministic physics.
```

A Deterministic Physics Simulation, where there is literally zero fuziness and even the random number generators are seeded together can in turn reveal



#### Is Unity Deterministic?

#### Is Unreal Deterministic?

No. Although we don't currently use the Unreal Engine, Unreal does also suffer from non-deterministic physics.
