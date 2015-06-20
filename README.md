## Lockstep.IO

A drop in Unity and NodeJS, Socket.IO based "Lockstep" implementation to support rapid development of online games in Unity.

**This open source project is still in very early alpha development and is by no means to be considered production ready despite its current usability! Security, scalability, and stability are less of a present development concern than the ease of use to the average game developer with little or no extra boilerplate involved.**

#### Quick Start: Video Introduction/Tutorial
You can watch a quick video tutorial of how to use Lockstep.IO on Youtube => http://youtu.be/HN_oLZy5tYc

#### Quick Start: Connecting NodeJS and Unity

1. Install NodeJS from https://nodejs.org/ if you don't already have the latest version.

1. Create a new folder for your project, then from the command line initialize the project using:

	```sh
	npm init .
	```

1. NPM will walk you through setting up the project details, and create a `package.json` file for you with the project details and dependencies.

1. Add LockstepIO as a dependency to your NodeJS project:

	```sh
	npm install lockstep.io --save
	```
	
1. Run the LockstepIO development server locally:

	```sh
	node ./node_modules/lockstep.io/nodejs/server.js
	```
	
1. If everything worked correctly your local LockstepIO server should be running:

	```sh
	Lockstep.IO: Listening on port 80!
	Connect to Unity locally with the following URL:
	ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket
	```
	
1. Minimize your server window (the server will only run while open), and either open or create a new Unity project. Navigate to `./node_modules/lockstep.io/unity/` and drag the `LockstepIO` folder and associated `LockstepIO.meta` file into your Unity project library.

1. Add the `LockstepIOComponent` to a single game object which will act as your game's connection to the server, a 	`SocketIOComponent` will be automatically added for you.

1. Click play and wait a moment for the `LockstepIOComponent` and `SocketIOComponent` to connect and synchronize Lockstep timing with the server. Both scripts come default to auto-connect and sync with a local server.

1. Assuming everything is running correctly and no firewalls are blocking the network traffic, the `Lockstep Ready` flag will become checked within a few seconds to signal lockstep synchronization. The `LastLockstepReady` string will contain debug information about all clients connected to the server.

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




#### Technical Discussion: What is Lockstepping?

Starcraft, Age of Empires, and Warcraft 3 all use lockstepping - not this particular library, rather the same idea. Lockstepping forces all user input to be broadcast over the network and executed roughly 200ms into the future; when you click to move a unit, there will always be a 200ms delay before the unit responds to your input. This 200ms, known as the "latency window", provides enough time for that command to reach all other networked players, then for that command to execute in synch across everyone's simulation of the game.

#### Technical Discussion: What is Deterministic Lockstepping?

Physics simulation in *most* games is a "fuzzy" science, where numbers don't always need to be dead accurate, just "close enough" to look realistic. Normally this isn't a problem and the speed trade off pays for itself, but when we need to run multiple identical simulations on different machines (ie. a multiplayer game), that "fuzziness" starts to become a serious issue:

```
If in Player A's simulation the character just dodged the bullet in time, but in Player B's simulation the character didn't dodge that same bullet in time - strictly due to fuzziness - our two simulations are no longer in sync and we aren't sharing a game state any longer. We call this non-deterministic physics.
```

A Deterministic Physics Simulation, where there is literally zero fuziness and even the random number generators are seeded together can in turn reveal



#### Technical Discussion: Is Unity Deterministic?

The random number generator in Unity can be made to be deterministic by shared seeding, which LockstepIO already does for you upon synchronizing lockstep with the server. This isn't the real problem though.

Unity uses two physics engines internally, one for 2D and one for 3D - both of these engines run on floating point calculations; that is to say: they use floats to do their math. Floats are a great way to store numbers fairly accuratly up to a few decimal places, but unfortunately "fairly accurately" means different CPU hardware can use different algorithms for floating point calculations.

If we tell two different computers to add `1.00000001 + 1.00000001`, one computer will likely tell us the answer is `2`, while the other computer tells us the answer is `2.00000002` or even something you wouldn't naturally expect like `2.0000145` (an artifact of low floating point accuracy).

In game physics, a "close enough" answer is faster to calculate, and for almost all circumstances far more than enough accuracy! However, determinism says that these tiny floating point differences can't exist between machines or the state will almost immedately fall out of sync thatnks to the Chaos Effect, also known as the Butterfly Effect.

So as of writing this, no, Unity is not deterministic because of the floating point physics engine. This problem could be corrected with much slower deterministic virtualized floats (similar to what Java can do with special compiler flags), whereby floating point calculations happen at the slower software layer rather than at the much faster the hardware accellerated layer.

#### Technical Discussion: Is Unreal Deterministic?

Unreal unfortunately appears to suffer from the same problem as Unity, whereby physics are only "deterministic enough" to allow frame by frame synchronization of all game details with minimal "wobble". This works great for up to 16 game objects in realtime, but as that number of game objects grow the amount of data being transported quickly explodes out of control.

For example: in a deterministic lockstep simulation, a unit could move across an entire map by sending a single packet "move unit X across the map". Because the simulation is deterministic and lockstep guarantees execution time, each computer finds the exact same path for the unit across the map without communicating any further.

In a non-deterministic lockstep simulation, a unit needs to send packets each frame declaring its position on the "host" machine "unit X is at 21, 20", "unit x is at 21, 21", "unit x is at 21, 22", etc. Instead of one packet to move a single unit we could potentially need millions.

Most first person shooters don't require true determinism because they can handle realtime positioning of 8 or so units without problem, but real time strategies featuring more than 100 units on the map will absolutely require lockstep determinism to work on today's internet connections (even the fastest or local connections choke out at 200 or less units).

## Brought to you by [Inkhorn Games](http://inkhorn.co)
Developed for our upcoming title Battle Brigades ([subreddit](http://www.reddit.com/r/battlebrigades)).

Connect with us:
[Facebook](https://www.facebook.com/inkhorngames) | [Twitter](https://twitter.com/inkhorngames) | [Youtube](http://youtube.com/inkhorncompany)
