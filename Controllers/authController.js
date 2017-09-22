const mongoose = require('mongoose');
const User = mongoose.model('User');
const hasha = require('hasha');

exports.getUser = async (req, res) => {
    const password = hasha(req.body.password, {algorithm: 'whirlpool'});
	const username = req.body.username;
  	const user = await User.findOne({ username, password });
  	if (user) {
		return res.send(user);
  	} else {
  	  return res.send({ error: "User not found", });
  	}
};

exports.createUser = (req, res) => {
    const password = hasha(req.body.password, {algorithm: 'whirlpool'});
	const user = new User({
		username: req.body.username,
		password: password,
	}).save((err, user, numberOfRow) => {
		return res.send(user);
	});
};
