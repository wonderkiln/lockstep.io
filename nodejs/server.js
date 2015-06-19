var app = require('http').createServer(handler)
var io  = require('socket.io')(app);
var lockstepio = require('./lockstep.io.js')(io);

app.listen(80);
console.log('Lockstep.IO: Listening on port 80!');
console.log('Connect to Unity locally with the following URL:');
console.log('ws://127.0.0.1:80/socket.io/?EIO=4&transport=websocket');


function handler (req, res) {
    res.writeHead(200);
    res.end('<3');
};