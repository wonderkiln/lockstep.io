var app = require('http').createServer(handler)
var io  = require('socket.io')(app);
var lockstepio = require('./lockstep.io.js')(io);

app.listen(80);

function handler (req, res) {
    res.writeHead(200);
    res.end('<3');
};