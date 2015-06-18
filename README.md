## Lockstep.IO

A drop in Unity and NodeJS, Socket.IO based "Lockstep" implementation to support rapid development of online games in Unity.

#### Quick Start: Connecting NodeJS and Unity

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

```sh
node ./node_modules/lockstep.io/nodejs/server.js
```

6. If everything worked correctly your local LockstepIO server should be running:

```sh
Lockstep.IO: Listening on port 80!
Connect to Unity locally with the following URL:
ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket
```

7. Minimize your server window (the server will only run while open), and either open or create a new Unity project. Navigate to `./node_modules/lockstep.io/unity/` and drag the `LockstepIO` folder and associated `LockstepIO.meta` file into your Unity project library.

8. Add the `LockstepIOComponent` to a single game object which will act as your game's connection to the server, a 	`SocketIOComponent` will be automatically added for you.

9. Click play and wait a moment for the `LockstepIOComponent` and `SocketIOComponent` to connect and synchronize Lockstep timing with the server. Both scripts come default to auto-connect and sync with a local server.

10. Assuming everything is running correctly and no firewalls are blocking the network traffic, the `Lockstep Ready` flag will become checked within a few seconds to signal lockstep synchronization. The `LastLockstepReady` string will contain debug information about all clients connected to the server. 

#### Quick Start: The Issue Execute Command Cycle

With lockstepping, all commands are first **Issued** to the server, the server then acting as a relay, broadcasts that command to all players whilst ensuring they all have time to receive the command across the latency window with plenty of time left to **Execute** the command in-sync with the other players.

This means that when a 50ms network transport delay exists between players (ie 50ms of lag), commands can be queued to execute 150ms in the future from the time they are issued, allowing ample time for the command to cross the network to everyone before executing, and thus allowing everyone to see the command happen at the same time regardless of network speed. *The command delay is currently handled adaptively and will increase to accomodate slower players.*

LockstepIO tries to simplify this delayed-command-relay-loop by providing you with two easy functions:

```csharp
public void IssueCommand (JSONObject Command)
```

Which takes your command to broadcast to everyone, attaches a lockstep command delay and relays that command through the server, to all the other players, including the player who issued it. During the frame the command is set to execute, the execute command function will be called:

```csharp
public void ExecuteCommand (JSONObject Command)
```

Adding networked commands is as easy as sending them through the network with Issue and adding code to Execute to actually respond to those commands.



---- flack below


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
