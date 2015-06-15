var app = require('http').createServer(handler)
var io  = require('socket.io')(app);
var lockstepio = require('./lockstepio.js')(io);

app.listen(80);

function handler (req, res) {
    res.writeHead(200);
    res.end('<3');
};