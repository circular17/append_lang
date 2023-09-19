// Utility functions for debugging

const util = require("util");

function dump(obj)
{
    console.log(util.inspect(obj, {showHidden: false, depth: null, colors: true}))
}

module.exports = {
    dump
};