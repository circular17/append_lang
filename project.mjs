import { promises } from "fs"
import { parse as parseGrammar } from "./generated/grammar.out.mjs"
import * as tree from "./tree.mjs"
import { transpile } from "./transpiler.mjs"
import path from "path"

export async function readProject(mainFilename, autoUse) {
    var project = {
        modules: []
    }
    var filesToRead = [mainFilename]
    var useAdded = new Set
    while (filesToRead.length > 0) {
        const filename = filesToRead.shift()
        try {
            const sourceCode = await promises.readFile(filename, 'utf8')
            const module = parseModule(sourceCode)
            if (!tree.is(module, tree.MODULE))
                throw new Error("Expecting module")
    
            var useList = new Set(autoUse)
            module.statements
            .filter(m => tree.is(m, tree.USE))
            .forEach(use => 
                use.modules.forEach(n => useList.add(n))
            )
    
            var depend = []
            useList.forEach(u => depend.push({ name: u }))
            
            for (const m of depend) {
                if (!useAdded.has(m.name)) {
                    useAdded.add(m.name)
                    filesToRead.push(path.join(path.dirname(filename), m.name + ".ap"))
                }
            }
    
            project.modules.push({
                filename,
                module,
                depend
            })            
        }
        catch (e) {
            console.log("Error reading " + filename)
            console.log(e)
        }
    }
    for (const m of project.modules)
    {
        console.log(m)
        transpile(m.module)
    }
}

function parseModule(sourceCode)
{
    try {
        return parseGrammar(sourceCode)
    }
    catch (e) {
        if (e.location)
        {
            const lines = sourceCode.split(/\r?\n/);
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