// Utility functions for debugging

import util from "util"

export function pretty(obj)
{
    return util.inspect(obj, {showHidden: false, depth: 5, colors: true})
}

export function dump(obj)
{
    console.log(pretty(obj))
}