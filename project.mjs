import { promises } from "fs"
import { parse as parseGrammar } from "./generated/grammar.out.mjs"
import * as tree from "./tree.mjs"
import { transpile } from "./transpiler.mjs"
import path from "path"
import { dump, pretty } from "./debug.mjs"

class Project {
    constructor() {
        this.modules = []
    }
}

class Module {
    constructor(filename, codeTree) {
        this.filename = filename
        this.name = path.parse(filename).name
        this.codeTree = codeTree
        this.dependOn = []
    }
}

class Dependency {
    constructor(moduleName) {
        this.moduleName = moduleName
        this.module = null
    }
}

export async function readProject(mainFilename, autoUse) {
    var project = new Project
    var filesToRead = [mainFilename]
    var useAdded = new Set
    var error = false
    while (filesToRead.length > 0) {
        const filename = filesToRead.shift()
        console.log("Reading " + filename)
        try {
            const sourceCode = await promises.readFile(filename, 'utf8')
            const codeTree = parseModule(sourceCode)
            if (!tree.is(codeTree, tree.MODULE))
                throw new Error("Expecting module")
            const module = new Module(filename, codeTree)
    
            var useList = new Set(autoUse)
            codeTree.statements
            .filter(m => tree.is(m, tree.USE))
            .forEach(use => 
                use.modules.forEach(n => useList.add(n))
            )
    
            useList.forEach(u => module.dependOn.push(new Dependency(u)))
            
            for (const m of module.dependOn) {
                if (!useAdded.has(m.moduleName)) {
                    useAdded.add(m.moduleName)
                    filesToRead.push(path.join(path.dirname(filename), m.moduleName + ".ap"))
                }
            }
    
            project.modules.push(module)
        }
        catch (e) {
            error = true
            console.log(e)
        }
    }

    if (error)
        return null

    for (const m of project.modules) {
        for (const d of m.dependOn) {
            const { moduleName } = d
            for (const m2 of project.modules) {
                if (m2.name == moduleName) {
                    d.module = m2
                }
            }
        }
    }

    dump(project.modules[0])

    for (const m of project.modules)
    {
        console.log(m.filename + " => JS")
        transpile(m.codeTree)
    }
    
    return project
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