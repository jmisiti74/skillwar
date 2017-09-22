const mongoose = require('mongoose');
const passportLocalMongoose = require('passport-local-mongoose');

const fightSchema = new mongoose.Schema({
	userOne: {
		type: String,
		default: null,
	},
	userTwo: {
		type: String,
		default: null,
	},
	winner: {
		type: String,
		default: null,
	},
});

fightSchema.statics.findAndModify = function fAndM(query, callback) {
	return this.collection.findAndModify(query, callback);
};

module.exports = mongoose.model('Fight', fightSchema);
