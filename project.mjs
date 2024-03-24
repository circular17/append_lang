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
        if (!tree.is(codeTree, tree.MODULE))
                throw new Error("Expecting module")

        this.filename = filename
        this.name = path.parse(filename).name
        this.codeTree = codeTree
        this.dependOn = []
        this.findIdentifiersRec(codeTree)
    }

    findIdentifiersRec(element) {
        element.types = new Map
        element.functions = new Map
        element.properties = new Map
        element.variables = new Map
        if (tree.is(element, tree.MODULE)) {
            this.findIdentifiers(element, element.statements)
        }
    }

    findIdentifiers(element, content) {
        if (Array.isArray(content)) {
            for (const statement of element.statements) {
                this.findIdentifiers(element, statement)
            }
        } else if (tree.is(content, tree.TYPE_DEF)) {
            const typeDef = content
            if (!element.types.has(typeDef.name))
                element.types.set(typeDef.name, [])
            element.types.get(typeDef.name).push(typeDef)
        } else if (tree.is(content, tree.FUN_DEF)) {
            const funDef = content
            if (!element.functions.has(funDef.name))
                element.functions.set(funDef.name, [])
            element.functions.get(funDef.name).push(funDef)
        } else if (tree.is(content, tree.PROP_DEF)) {
            const propDef = content
            if (element.properties.has(propDef.name))
                tree.leafError(content, `Duplicate property name`,
                    element.properties.get(propDef.name))
            element.properties.set(propDef.name, propDef)
        } else if (tree.is(content, tree.CONST_DEF) ||
                tree.is(content, tree.VAR_DEF)) {
            const def = content
            for (const [name, member] of def.deconstructNames)
            {
                if (element.variables.has(name))
                    tree.leafError(member, `Duplicate variable name ${name}`,
                        element.variables.get(name))
                element.variables.set(name, member)
            }
        }
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
        let sourceCode
        try {
            sourceCode = await promises.readFile(filename, 'utf8')
            const codeTree = parseModule(sourceCode)
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
            displayError(e, sourceCode)
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
        displayError(e, sourceCode)
    }   
}

function displayMessageWithLocation(message, location, sourceCode) {
    const lines = sourceCode.split(/\r?\n/);
    const {start, end} = location
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

    console.log(`line ${end.line}: `+ message
        .replace("[A-Za-z]", "identifier")
        .replace("[A-Za-z0-9_]", "more letters")
        .replace("[0-9]", "number")
        .replace("[ \\t\\r\\n]", "end of line")
        .replace("[ \\t]", "space")
        .replace("\"\\n\"", "end of line")
        .replace("\"/*\", ", "")
        .replace("\"//\", ", ""))
}

function displayError(e, sourceCode) {
    if (e.location)
    {
        displayMessageWithLocation(e.message, e.location, sourceCode)
        if (e.otherLocation) 
            displayMessageWithLocation("Other declaration", e.otherLocation, sourceCode)
    }
    else
        console.log(e)
}