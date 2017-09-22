const [major, minor] = process.versions.node.split('.').map(parseFloat);
if (major < 7 || (major === 7 && minor < 6)) {
	console.log('Node.js version is too old. Please use 7.6 or above');
	process.exit();
}

if (process.env.NODE_ENV !== 'production') {
	require('dotenv').config();
}

const http = require('http');
const express = require('express');
const cors = require('cors');
const app = express();
const bodyParser = require('body-parser');
const mongoose = require('mongoose');
const passport = require('passport');
const path = require('path');
const expressValidator = require('express-validator');

app.set('port', (process.env.PORT || 8081));
app.use(cors());
mongoose.connect('mongodb://xjejevbx:lololo01@bvb3-shard-00-00-w0yqj.mongodb.net:27017,bvb3-shard-00-01-w0yqj.mongodb.net:27017,bvb3-shard-00-02-w0yqj.mongodb.net:27017/bvb3?ssl=true&replicaSet=bvb3-shard-0&authSource=admin', { useMongoClient: true });
mongoose.Promise = global.Promise;
mongoose.connection.on('error', (err) => {
	console.error(`${err.message}`);
});

require('./Models/User');
require('./Models/Fight');
const User = mongoose.model('User');
const Fight = mongoose.model('Fight');
const authController = require('./Controllers/authController');
const fightController = require('./Controllers/fightController');
app.use(express.static(path.join(__dirname, 'public')));
app.use(bodyParser.urlencoded({
    extended: false
}));

app.post('/fight',
	fightController.getFight,
);

app.post('/getPositions',
	fightController.getPositions,
);

app.post('/signin',
	authController.getUser,
);

app.post('/signup',
	authController.createUser,
);

app.get('/getGameRar', (req, res) => {
	res.download(__dirname + "/Public/SW.rar");
});

app.get('/getGameZip', (req, res) => {
	res.download(__dirname + "/Public/SW.zip");
});

app.get('/', (req, res) => {
	res.send("Bienvenue sur SkillWar, veuillez télécharger notre jeu sur le site : ce lien <a href='/getGameZip'>ici le zip</a> et <a href='/getGameRar'>ici le rar</a>.");
});

app.get('*', function(req, res){
  res.send('Erreur 404', 404);
});

const server = app.listen(app.get('port'), () => {
	console.log('Node app is running on port', app.get('port'));
});

const io = require('socket.io').listen(server);

io.sockets.on('connection', fightController.socket);
