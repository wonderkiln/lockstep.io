module.exports = function (io) {
	var ref = {
		io: io,
		commandDelay: 60,
		sockets: {},
		stateSync: {},
		randomSeed: parseInt(Math.random() * 10000),
		onConnection: function (socket) {
			
			console.log('connected: ' + socket.id);

			// if we have no host make host
			if (!ref.host) {
				socket.is_host = true;
				ref.host = socket.id;
			}
			ref.sockets[socket.id] = {socket: socket};
			socket.on('disconnect', function() {
				console.log('disconnected: ' + socket.id);

				if (ref.host == socket.id) {
					delete ref['host'];
				}
				delete ref.sockets[socket.id];
			});
			socket.emit('lockstep.io:seed', {randomSeed: ref.randomSeed});
			socket.on('lockstep.io:sync', function (ntp) {
				if (socket.is_host) {
					socket.emit('lockstep.io:sync', {t0: ntp.t0, t1: ntp.t0});	
				} else {
					socket.emit('lockstep.io:sync', {t0: ntp.t0, t1: ref.sockets[ref.host].sync.localNow});	
				}
			});
			socket.on('lockstep.io:ready', function (ready) {
				ref.sockets[socket.id].sync = ready;
				var clients = {};
				for (var id in ref.sockets) {
					clients[id] = ref.sockets[id].sync;
				}
				var pkg = {commandDelay: ref.commandDelay, clients: clients};
				ref.io.emit('lockstep.io:ready', pkg);
			});
			socket.on('lockstep.io:cmd:issue', function (command) {
				// if this command is a state sync (ENUM 0, stateSync)
				if (command.dispatcher == 0) {
					if (!ref.stateSync.hasOwnProperty(command.lockstepTime)) {
						ref.stateSync[command.lockstepTime] = {};
					}
					ref.stateSync[command.lockstepTime][socket.id] = command.hash;
					if (socket.is_host && command.lockstepTime % 30 == 0) {
						console.log(ref.stateSync[command.lockstepTime]);
					}
					return; // return out for now, don't relay sync
				} 
				ref.io.emit('lockstep.io:cmd:issue', command);
			});
		},
		issueCommand: function (command) {
			command.atLockstep = new Date().getTime() + ref.commandDelay;
			ref.io.emit('lockstep.io:cmd:issue', command);
		}
	};
	ref.io.on('connection', ref.onConnection);
	return ref;
};
