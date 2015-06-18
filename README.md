## Lockstep.IO

A drop in Unity and NodeJS, Socket.IO based "Lockstep" implementation to support rapid development of online games in Unity.

#### Quick Start

1. Install NodeJS from https://nodejs.org/ if you don't already have the latest version.

2. Create a new folder for your project, then from the command line initialize the project using:

```sh
npm init .
```

3. NPM will walk you through setting up the project details, and create a `package.json` file for you with the project details and dependencies.

4. Add LockstepIO as a dependency to your NodeJS project:

```sh
npm install lockstep.io --save
```

5. Run the LockstepIO development server locally:

```
node ./node_modules/lockstep.io/nodejs/server.js
```

6. If everything worked correctly your local LockstepIO server should be running:

```
Lockstep.IO: Listening on port 80!
Connect to Unity locally with the following URL:
ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket
```

7. Minimize your server window (the server will only run while open), and either open or create a new Unity project. Navigate to `./node_modules/lockstep.io/unity/` and drag the `LockstepIO` folder and associated `LockstepIO.meta` file into your Unity project library.

8. Add the `LockstepIOComponent` to a single game object which will act as your game's connection to the server.


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
