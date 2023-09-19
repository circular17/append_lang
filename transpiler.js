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
            const codeLine = lines[start.line - 1]
            console.log(codeLine)
            if (end.line > start.line)
            {
                const upToEndOfLine = codeLine.length - start.column + 1
                if (upToEndOfLine === 0)
                    console.log(" ".repeat(start.column - 1) + "^")
                else
                    console.log(" ".repeat(start.column - 1) + "~".repeat(upToEndOfLine))
            }
            else if (end.column > start.column + 1)
            {
                console.log(" ".repeat(start.column - 1) + "~".repeat(end.column - start.column))
            }
            else
                console.log(" ".repeat(start.column - 1) + "^")

            console.log(`line ${start.line}: `+ e.message
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