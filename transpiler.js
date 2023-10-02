// Transpiler to Javascript

const grammar = require("./generated/grammar.out");
const debug = require("./debug");

function transpile(code)
{
    try
    {
        const parsed = grammar.parse(code)
        debug.dump(parsed)
    }
    catch (e) {
        if (e.location)
        {
            const lines = code.split(/\r?\n/);
            const {start, end} = e.location
            console.log(lines[start.line - 1])
            if (end.line > start.line)
            {
                if (end.line - start.line > 2)
                    console.log("...")
                else if (end.line - start.line === 2)
                    console.log(lines[start.line])

                console.log(lines[end.line - 1])
                if (end.column === 1)
                    console.log("^")
                else
                    console.log("~".repeat(end.column - 1))
            }
            else if (end.column > start.column + 1)
            {
                console.log(" ".repeat(start.column - 1) + "~".repeat(end.column - start.column))
            }
            else
                console.log(" ".repeat(start.column - 1) + "^")

            console.log(`line ${end.line}: `+ e.message
                .replace("[A-Za-z]", "identifier")
                .replace("[A-Za-z0-9_]", "more letters")
                .replace("[0-9]", "number")
                .replace("[ \\t\\r\\n]", "end of line")
                .replace("[ \\t]", "space")
                .replace("\"\\n\"", "end of line")
                .replace("\"/*\", ", "")
                .replace("\"//\", ", ""))
        }
        else
            console.log(e)
    }
}

module.exports = {
    transpile
};