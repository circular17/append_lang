// Utility functions for debugging

const util = require("util");

function pretty(obj)
{
    return util.inspect(obj, {showHidden: false, depth: null, colors: true})
}

function dump(obj)
{
    console.log(pretty(obj))
}

module.exports = {
    dump,
    pretty
};