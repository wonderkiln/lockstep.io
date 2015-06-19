module.exports = function (io) {
	var ref = {
		io: io,
		commandDelay: 0,
		sockets: {},
		randomSeed: parseInt(Math.random() * 10000),
		onConnection: function (socket) {
			console.log('connected: ' + socket.id);

			// if we have no host make host
			if (!ref.host) {
				socket.is_host = true;
				ref.host = socket.id;
			}

			// stash socket reference by id
			ref.sockets[socket.id] = {socket: socket};

			// provide random seed
			socket.emit('lockstep.io:seed', {randomSeed: ref.randomSeed});

			// handle disconnects
			socket.on('disconnect', function() {
				console.log('disconnected: ' + socket.id);

				// if the host disconnected
				if (ref.host == socket.id) {
					
					// delete host reference
					delete ref['host'];

					// attempt to assign a new host here...
					///todo
				}

				// delete socket reference
				delete ref.sockets[socket.id];
			});

			// handle sync events
			socket.on('lockstep.io:sync', function (ntp) {
				if (socket.is_host) {
					socket.emit('lockstep.io:sync', {t0: ntp.t0, t1: ntp.t0});	
				} else {
					socket.emit('lockstep.io:sync', {t0: ntp.t0, t1: ref.sockets[ref.host].sync.localNow});	
				}
			});

			// handle sync complete (ready) events
			socket.on('lockstep.io:ready', function (ready) {


				console.log(ready);
				if (ready.roundTrip * 3 > ref.commandDelay) {
					ref.commandDelay = ready.roundTrip * 3;
				}

				ref.sockets[socket.id].sync = ready;
				var clients = {};
				for (var id in ref.sockets) {
					clients[id] = ref.sockets[id].sync;
				}
				var pkg = {commandDelay: ref.commandDelay, clients: clients};
				ref.io.emit('lockstep.io:ready', pkg);
			});
			socket.on('lockstep.io:cmd:issue', function (command) {
				ref.io.emit('lockstep.io:cmd:issue', command);
			});
		}
	};
	ref.io.on('connection', ref.onConnection);
	return ref;
};
