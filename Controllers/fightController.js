const mongoose = require('mongoose');
const Fight = mongoose.model('Fight');
const User = mongoose.model('User');
const STARTPA = 8;
const STARTPM = 3;
const STARTHP = 75;
const DISCONNECTIONTIMEOUT = 30;
let clients = new Array();
const spell1List = {1 : {
		offset: 0,
		speed: 200,
		damages: 35,
	}, 2 : {
		offset: 0,
		speed: 200,
		damages: 35,
	}, 3 : {
		offset: 0,
		speed: 200,
		damages: 35,
	},
}
let fights = new Array();
let emitTo;

const refreshPm = (fid, user) => {
	fights[fid][user].pmReloading = 1;
	setTimeout(() => {
		fights[fid][user].pm++;
		emitTo(fights[fid][user].id, "addPm", null);
		if (fights[fid][user].pm < 3) {
			refreshPm(fid, user);
		} else {
			fights[fid][user].pmReloading = 0;
		}
	}, 50);
};


exports.getFight = async (req, res) => {
	const fight = await Fight.findOne({ $or: [{ userTwo: null, }, { $and: [ { userOne: req.body.id, }, { winner: null, }, ] }, { $and: [ { userTwo: req.body.id, }, { winner: null, }, ] }]});
	if (fight && fights[fight._id]) {
		if (fight.userOne.toString() === req.body.id) {
			clients[req.body.socketid].currentFight = fight._id;
			fights[fight._id][0].id = req.body.socketid;
			fight.save((err, fgt, numberOfRow) => {
				if (fight.userTwo === null) {
					res.send('found');
				} else {
					clearTimeout(fights[fight._id][0].timeout);
					emitTo(fights[fight._id][1].id, "userIsBack", null);
					emitTo(fights[fight._id][0].id, "launch", null);
					emitTo(fights[fight._id][1].id, "launch", null);
					res.send('found');
				}
			});
			//Il a quitter et reviens..
		} else {
			//Il y a un fight
			fight.userTwo = req.body.id;
			clients[req.body.socketid].currentFight = fight._id;
			fights[fight._id][1].id = req.body.socketid;
			fight.save((err, fgt, numberOfRow) => {
				clearTimeout(fights[fight._id][1].timeout);
				emitTo(fights[fight._id][0].id, "userIsBack", null);
				emitTo(fights[fight._id][0].id, "launch", null);
				emitTo(fights[fight._id][1].id, "launch", null);
				res.send('found');
			});
		}
	} else {
		if (fight && !fights[fight._id]) {
			fight.winner = "none";
			fight.save((err, fgt, numberOfRow) => {
				//Il n'y en a pas donc je crée un fight et attend un autre utilisateur
				const newFight = new Fight({
					userOne: req.body.id,
				}).save((err, fgt, numberOfRow) => {
					clients[req.body.socketid].currentFight = fgt._id;
					fights[fgt._id] = getNewFight(req.body.socketid);
					res.json('found');
				});
			});
		} else {
			//Il n'y en a pas donc je crée un fight et attend un autre utilisateur
			const newFight = new Fight({
				userOne: req.body.id,
			}).save((err, fgt, numberOfRow) => {
				clients[req.body.socketid].currentFight = fgt._id;
				fights[fgt._id] = getNewFight(req.body.socketid);
				res.json('found');
			});
		}
	}
};

const getNewFight = (socketid, spell1, spell2, spell3) => {
	return ({ 0 : {
			x: parseInt(getRandomBetween(30, 0)),
			z: parseInt(getRandomBetween(3, 1)),
			rotation: 0,
			pa: STARTPA,
			paReloading: 0,
			pm: STARTPM,
			pmReloading: 0,
			hp: STARTHP,
			id: socketid,
			reconnectionChance: 1,
		}, 1 : {
			x: parseInt(getRandomBetween(30, 0)),
			z: parseInt(getRandomBetween(17, 15)),
			rotation: 180,
			pa: STARTPA,
			paReloading: 0,
			pm: STARTPM,
			pmReloading: 0,
			hp: STARTHP,
			id: null,
			reconnectionChance: 1,
		}
	});
};

const getRandomBetween = (a, b) => {
	const ret = Math.floor(Math.random() * (a - b + 1) + b);
	return ret;
};

exports.getPositions = async (req, res) => {
	const fight = await Fight.findOne({ $or: [{ userTwo: null, }, { $and: [ { userOne: req.body.id, }, { winner: null, }, ] }, { $and: [ { userTwo: req.body.id, }, { winner: null, }, ] }]});
	if (fight && fights[fight._id]) {
		if (req.body.id === fight.userOne) {
			res.send(`${fights[fight._id][0].x},0,${fights[fight._id][0].z},${fights[fight._id][0].rotation}|${fights[fight._id][1].x},0,${fights[fight._id][1].z},${fights[fight._id][1].rotation}`);
		} else {
			res.send(`${fights[fight._id][1].x},0,${fights[fight._id][1].z},${fights[fight._id][1].rotation}|${fights[fight._id][0].x},0,${fights[fight._id][0].z},${fights[fight._id][0].rotation}`);
		}
	}
};

const updatePositions = async (dir, id, userId) => {
	const fight = await Fight.findOne({ $or: [{ userTwo: null, }, { $and: [ { userOne: userId, }, { winner: null, }, ] }, { $and: [ { userTwo: userId, }, { winner: null, }, ] }]});
	let user = null;
	if (fight && fights[fight._id]) {
		user = (fights[fight._id][1].id === id) ? 1 : 0;
		other = user === 1 ? 0 : 1;
		if (dir === "top" && fights[fight._id][user].z < 18 && fights[fight._id][user].pm > 0) {
			fights[fight._id][user].pm -= 1;
			if (fights[fight._id][user].pmReloading === 0) {
				refreshPm(fight._id, user);
			}
			fights[fight._id][user].z += 1;
			emitTo(fights[fight._id][other].id, dir, null);
		} else if (dir === "bottom" && fights[fight._id][user].z > 0 && fights[fight._id][user].pm > 0) {
			fights[fight._id][user].pm -= 1;
			if (fights[fight._id][user].pmReloading === 0) {
				refreshPm(fight._id, user);
			}
			fights[fight._id][user].z -= 1;
			emitTo(fights[fight._id][other].id, dir, null);
		} else if (dir === "right" && fights[fight._id][user].x < 32 && fights[fight._id][user].pm > 0) {
			fights[fight._id][user].pm -= 1;
			if (fights[fight._id][user].pmReloading === 0) {
				refreshPm(fight._id, user);
			}
			fights[fight._id][user].x += 1;
			emitTo(fights[fight._id][other].id, dir, null);
		} else if (dir === "left" && fights[fight._id][user].x > 0 && fights[fight._id][user].pm > 0) {
			fights[fight._id][user].pm -= 1;
			if (fights[fight._id][user].pmReloading === 0) {
				refreshPm(fight._id, user);
			}
			fights[fight._id][user].x -= 1;
			emitTo(fights[fight._id][other].id, dir, null);
		} else if (dir === "rLeft") {
			fights[fight._id][user].rotation -= 90;
			emitTo(fights[fight._id][other].id, dir, null);
		} else if (dir === "rRight") {
			fights[fight._id][user].rotation += 90;
			emitTo(fights[fight._id][other].id, dir, null);
		}
	} else {
		console.log('No fight found, update failed.');
	}
};

const spellUpdate = (fightid, spell, user, x, z, angle) => {
	const f = fights[fightid];
	const other = user === 1 ? 0 : 1;
	// C'est l'utilisateur 1
	if (x === f[other].x && z === f[other].z) {
		console.log("Il y a touche");
	} else if (x > 33 || x < 0 || z > 19 || z < 0) {
		console.log("Cible raté");
	} else {
		angle = (angle > 0) ? angle % 360 : -angle % 360;
		switch (angle) {
			case 0:
				z++;
				break;
			case 90:
				x++;
				break;
			case 180:
				z--;
				break;
			case 270:
				x--;
				break;
		}
		setTimeout(spellUpdate.bind(this, fightid, spell, user, x, z, angle), spell.speed);
	}
};

const spell1Launched = async (fid, userid) => {
	const f = fights[fid];
	const user = (userid === f[0].id) ? 0 : 1;
	const other = (user === 1) ? 0 : 1;
	emitTo(f[user].id, "spellLaunched", { spellid: clients[f[user].id].spell1, x: f[user].x, z: f[user].z, me: true, r: f[user].rotation, } );
	emitTo(f[other].id, "spellLaunched", { spellid: clients[f[user].id].spell1, x: f[user].x, z: f[user].z, me: false, r: f[user].rotation, } );
	spellUpdate(fid, spell1List[clients[f[user].id].spell1], user, f[user].x, f[user].z, f[user].rotation);
};

const stopFight = (fightid, user) => {
	if (fights[fightid]) {
		const u = (user === fights[fightid][0].id) ? 0 : 1;
		const other = (u === 0) ? 1 : 0;
		if (fights[fightid][u].reconnectionChance > 0) {
			fights[fightid][u].reconnectionChance -= 1;
			emitTo(fights[fightid][other].id, "userLeft", null);
			fights[fightid][u].timeout = setTimeout(() => {
				console.log("le looser : ", user, " a perdu le fight : ", fightid);
			}, DISCONNECTIONTIMEOUT * 1000);
		} else {
			console.log("Combat fini. Il a déco trop de fois");
		}
	}
};

exports.socket = (socket) => {
	console.log("----- User : ", socket.id, " -----");
	clients[socket.id] = socket;

	// clients[socket.id].emit("UpdateHp", { hp: 50, });

	socket.on('disconnect', () => {
		stopFight(clients[socket.id].currentFight, socket.id);
		console.log("il da déco");
		clients[socket.id] = null;
	});

	socket.on('updateMySocketId', async (data) => {
		User.findById(data.id, (err, user) => {
			console.log(user.equiped1);
			clients[socket.id].spell1 = user.equiped1;
			clients[socket.id].spell2 = user.equiped2;
			clients[socket.id].spell3 = user.equiped3;
		});
		const fight = await Fight.findOne({ $or: [{ userTwo: null, }, { $and: [ { userOne: data.id, }, { winner: null, }, ] }, { $and: [ { userTwo: data.id, }, { winner: null, }, ] }]});
		if (fight && fights[fight._id]) {
			if (data.id === fight.userOne.toString()) {
				fights[fight._id][0].id = socket.id;
			} else {
				fights[fight._id][1].id = socket.id;
			}
		}
	});

	socket.on("launchSpell1", (data) => {
		spell1Launched(clients[socket.id].currentFight, socket.id);
	});

	socket.on("top", (data) => {
		updatePositions("top", socket.id, data.id);
	});

	socket.on("right", (data) => {
		updatePositions("right", socket.id, data.id);
	});

	socket.on("left", (data) => {
		updatePositions("left", socket.id, data.id);
	});

	socket.on("bottom", (data) => {
		updatePositions("bottom", socket.id, data.id);
	});

	socket.on("rLeft", (data) => {
		updatePositions("rLeft", socket.id, data.id);
	});

	socket.on("rRight", (data) => {
		updatePositions("rRight", socket.id, data.id);
	});

	emitTo = (index, message, data) => {
		if (clients[index]) {
			if (data) {
				clients[index.toString()].emit(message, data);
			} else {
				clients[index.toString()].emit(message);
			}
		}
	};
};
