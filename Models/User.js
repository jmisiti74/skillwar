const mongoose = require('mongoose');
const passportLocalMongoose = require('passport-local-mongoose');
const userSchema = new mongoose.Schema({
	username: {
		type: String,
		required: 'Please Supply a Username',
		unique: true,
		trim: true,
	},
	password: {
		type: String,
		required: 'Please Supply a Username',
		trim: true,
	},
	elo: {
		type: Number,
		default: 0,
	},
	experience: {
		type: Number,
		default: 0,
	},
	equiped1: {
		type: Number,
		default: 1
	},
	equiped2: {
		type: Number,
		default: 201
	},
	equiped3: {
		type: Number,
		default: 401
	},
	items: {
		type: Array,
		default: [1, 201, 401],
	},
});

userSchema.statics.findAndModify = function fAndM(query, callback) {
	return this.collection.findAndModify(query, callback);
};

module.exports = mongoose.model('User', userSchema);
