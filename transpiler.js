// Transpiler to Javascript

const grammar = require("./generated/grammar.out");
const debug = require("./debug");
const tree = require("./tree")

function transpile(code)
{
    try
    {
        const parsed = grammar.parse(code)

        console.log(toJS(parsed))
        //debug.dump(parsed)
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

function toJS(node)
{
    if (!tree.isLeaf(node))
    {
        if (node === null)
            return "<null>"
        const nodeType = typeof(node)
        return "<" + nodeType + " " + debug.pretty(node).substring(0, 20) + ">"
    }

    const map = mapJS[node._]
    if (map) {
        const mapType = typeof(map)
        switch (mapType)
        {
            case "string":
                return "'" + map + "'"

            case "function":
                return map(node)

            default:
                throw "Map type " + mapType + " not handled"
        }
    }
    else
        throw "Map not found for leaf type " + node._
}

function toBracketJS(node)
{
    if (tree.isLeaf(node) && [tree.LIST, tree.SET, tree.REC_VALUE, tree.VOID_VALUE,
        tree.INTEGER, tree.FLOAT, tree.TUPLE, tree.DICT_VALUE, tree.BOOLEAN, tree.VALUE_BY_NAME, tree.IMPLICIT_PARAM,
        tree.GET_MEMBER, tree.GET_WISE_MEMBER, tree.CREATE_OBJECT, tree.CALL,
        tree.TYPE_BY_NAME, tree.VOID_TYPE, tree.ANY_TYPE, tree.LIST_TYPE,
        tree.SET_TYPE, tree.DICT_TYPE, tree.REC_TYPE].includes(node._))
        return toJS(node)
    else
        return "(" + toJS(node) + ")"
}

function indent(code) {
    return code.split("\n").map(s => "    " + s).join("\n")
}

function escapeJS(text) {
    return "\"" + text
        .replaceAll("\\", "\\\\")
        .replaceAll("\"", "\\\"")
    + "\"" // to refine
}

function transpileType(type) {
    return escapeJS(toJS(type))
}

mapJS = {
    MODULE: (leaf) => {
        return leaf.statements.map(statement => toJS(statement)).join("\n")
    },
    CONST_DEF: (leaf) => {
        let result = "const "
        if (leaf.type)
            result += "/* " + toJS(leaf.type) + " */ "
        if (tree.is(leaf.names, tree.NAMES))
        {
            result += leaf.names.identifiers.join(" = ")
        }
        else
            result += toJS(leaf.names)
        return result + " = " + toJS(leaf.value)
    },
    VAR_DEF: (leaf) => {
        let result = "let "
        if (leaf.type)
            result += "/* " + toJS(leaf.type) + " */ "
        if (tree.is(leaf.names, tree.NAMES))
        {
            result += leaf.names.identifiers.join(" = ")
        }
        else
            result += toJS(leaf.names)
        if (leaf.value !== null)
            result += " = " + toJS(leaf.value)
        else
            result += " /* not defined */"
        return result
    },
    DECONSTRUCT_TUPLE: (leaf) => {
        return "[" + leaf.elements.map(element => toJS(element)).join(", ") + "]"
    },
    DECONSTRUCT_RECORD: (leaf) => {
        return "{" + leaf.elements.map(element => toJS(element)).join(", ") + "}"
    },
    DECONSTRUCT_MEMBER: (leaf) => {
        if (leaf.value)
            return leaf.member + ": " + toJS(leaf.value)
        else
            return leaf.member
    },
    DECONSTRUCT_NAME: (leaf) => {
        return leaf.name
    },
    FUN_DEF: (leaf) => {
        let params = "(" + leaf.params.map(p => toJS(p)).join(", ") + ")"
        if (leaf.effects && leaf.effects.length > 0) params += " /* effects */"
        let body = toJS(leaf.body)
        let result
        if (leaf.name !== null || leaf.kind === "enum")
        {
            if (!tree.is(leaf.body, tree.CODE_BLOCK))
                body = "{\n" + indent(body) + "\n}"
            result = "function" + (leaf.kind === "enum" ? "*" : "") + " " + (leaf.name ?? "")
                + params + " " + body
        }
        else
            result = params + " => " + body
        if (leaf.isAsync)
            result = "async " + result
        return result
    },
    FUN_PARAM_DEF: (leaf) => {
        return "/* " + toJS(leaf.type) + " */ " + leaf.names.join(", ")
    },
    INLINE_ENUM: (leaf) => {
        let body = toJS(leaf.body)
        if (!tree.is(leaf.body, tree.CODE_BLOCK))
            body = "{\n" + indent(body) + "\n}"
        return (leaf.isAsync ? "async " : "") + "function* " + body + "()"
    },
    VOID_TYPE: () => "()",
    CODE_BLOCK: (leaf) => {
        return (leaf.effects && leaf.effects.length > 0
            ? "/* effects */ " : "") +
            "{\n" + indent(leaf.statements.map(s => toJS(s)).join("\n")) + "\n}"
    },
    TYPE_DEF: (leaf) => "/* type " + leaf.genericParams.map(p => p + "-") + leaf.name + " " +
        toJS(leaf.type) + " */",
    ALIAS_DEF: (leaf) => "/* alias " + leaf.genericParams.map(p => p + "-") + leaf.name + " = " +
        toJS(leaf.type) + " */",
    UNION_TYPE: (leaf) => leaf.types.map(t => toJS(t)).join(" | "),
    FUN_TYPE: (leaf) => (leaf.isAsync ? "async " : "") + leaf.params.map(t => toJS(t)).join("->"),
    TUPLE_TYPE: (leaf) => leaf.types.map(t => toBracketJS(t)).join("*"),
    TUPLE_POWER_TYPE: (leaf) => toBracketJS(leaf.base) + "^" + toJS(leaf.power),
    OPTION_TYPE: (leaf) => toBracketJS(leaf.type) + "?",
    LIST_TYPE: (leaf) => "[" + toJS(leaf.type) + "]",
    SET_TYPE: (leaf) => "set{" + toJS(leaf.type) + "}",
    DICT_TYPE: (leaf) => "dict{" + toJS(leaf.key) + " -> " + toJS(leaf.value) + "}",
    REC_TYPE: (leaf) => {
        return "{\n" + indent(leaf.members.map(member => toJS(member)).join("\n")) + "\n}"
    },
    REC_FIELD_TYPE: (leaf) => {
        if (leaf.modifier === "base")
            return leaf.modifier + " " +
                leaf.names.join("/") + " = " + toJS(leaf.defaultValue)
        else
            return (leaf.modifier ? leaf.modifier + " " : "") +
                leaf.names.join("/") + " " + toJS(leaf.type) +
                (leaf.defaultValue ? ": " + toJS(leaf.defaultValue) : "")
    },
    GENERIC_PARAM_TYPE: (leaf) => "@" + leaf.name,
    ANY_TYPE: () => "any",
    DISJUNCTION: (leaf) => leaf.operands.map(o => toBracketJS(o)).join(" | "),
    XOR: (leaf) => leaf.operands.map(o => toBracketJS(o)).join(" ^ "),
    CONJUNCTION: (leaf) => leaf.operands.map(o => toBracketJS(o)).join(" & "),
    COMPARISON: (leaf) => {
        let comparisons = []
        for (let i = 0; i < leaf.operands.length - 1; i++) {
            comparisons.push(toBracketJS(leaf.operands[i].value) + " " + toJS(leaf.operands[i+1]))
        }
        return comparisons.join(" && ")
    },
    COMPARISON_OPERAND: (leaf) => {
      return leaf.operator + " " + toBracketJS(leaf.value)
    },
    CONCAT: (leaf) =>
        toBracketJS(leaf.operands[0]) + ".concat(" + leaf.operands.slice(1).map(o => toJS(o)).join(", ") + ")",
    ADDITION: (leaf) => {
        let result =
            (leaf.terms[0].sign !== "+"
            ? leaf.terms[0].sign + " " : "") +
            toBracketJS(leaf.terms[0].value)
        return result + leaf.terms.slice(1).map(term => " " + toJS(term))
    },
    TERM: (leaf) => leaf.sign + " " + toBracketJS(leaf.value),
    MULTIPLICATION: (leaf) => {
        let result =
            (leaf.factors[0].operator !== "*"
                ? leaf.factors[0].operator : "") +
            toBracketJS(leaf.factors[0].value)
        return result + leaf.factors.slice(1).map(factor => " " + toJS(factor))
    },
    FACTOR: (leaf) => leaf.operator + " " + toBracketJS(leaf.value),
    NOT: (leaf) => "!" + toBracketJS(leaf.value),
    EXPONENTIATION: (leaf) => toBracketJS(leaf.base) + "**" + toBracketJS(leaf.power),
    INDEXING: (leaf) => toBracketJS(leaf.list) + "[" + toJS(leaf.index) + "]",
    COMPOSE: (leaf) => {
        return "(x) => " + leaf.functions.map(f => toBracketJS(f) + "(").join("") +
            "x" + ")".repeat(leaf.functions.length)
    },
    VALUE_BY_NAME: (leaf) => {
        return leaf.namespace.map(n => n+".").join("") + leaf.name
    },
    TYPE_BY_NAME: (leaf) => {
        return leaf.namespace.map(n => n+".").join("") + leaf.name
    },
    TUPLE: (leaf) => {
        return "/* tup */[" + leaf.values.map(value => toJS(value)).join(", ") + "]"
    },
    REC_VALUE: (leaf) => {
        return "{\n" + indent(leaf.members.map(member => toJS(member)).join("\n")) + "\n}"
    },
    REC_FIELD_VALUE: (leaf) => {
        return leaf.name + ": " + toJS(leaf.value)
    },
    STRING_VALUE: (leaf) => {
        if (leaf.parts.length === 0)
            return "\"\""
        else
            return leaf.parts.map(part => toJS(part)).join(" + ")
    },
    STRING_PART: (leaf) => {
        return escapeJS(leaf.value)
    },
    FORMATTED_VALUE: (leaf) => toBracketJS(leaf.value) + ".toString()",
    IMPLICIT_PARAM: (leaf) => leaf.name,
    VOID_VALUE: () => "undefined",
    LIST: (leaf) => {
        return "[" + leaf.values.map(value => toJS(value)).join(", ") + "]"
    },
    SET: (leaf) => {
        return "new Set([" + leaf.values.map(value => toJS(value)).join(", ") + "])"
    },
    DEFAULT_VALUE: (leaf) => {
        return "[" + leaf.values.map(v => toJS(v)).join(", ") + "].reduce(" +
            "(a, c) => a?._ !== \"none\" ? a.value : c)"
    },
    TERNARY: (leaf) => {
        let ifTrue = toJS(leaf.ifTrue)
        if (tree.is(leaf.ifTrue, tree.TERNARY))
            ifTrue = "{\n" + indent(ifTrue) + "\n}"
        let result = "if (" + toJS(leaf.key) + ") " + ifTrue
        if (leaf.ifFalse)
            result += " else " + toJS(leaf.ifFalse)
        return result
    },
    CASE: (leaf) => {
        const cases = leaf.cases.map(option => toJS(option)).join("\n")
        return "switch (" + toJS(leaf.key) + ") {\n" +
            indent(cases) + "\n}"
    },
    CASE_OPTION: (leaf) => {
        let value = tree.is(leaf.value, tree.CODE_BLOCK)
            ? leaf.value.statements.map(s => toJS(s)).join("\n")
            : toJS(leaf.value)
        return leaf.patterns.map(pattern => "case " + toJS(pattern) + ":\n").join("") +
            indent(value + "\nbreak")
    },
    CAPTURE: (leaf) => (leaf.type ? "/* " + toJS(leaf.type) + " */ " : "") + leaf.name,
    TAGGED_VALUE: (leaf) => {
        return "{ _: " + transpileType(leaf.tag) + ", value: " + toJS(leaf.value) + " }"
    },
    RETURN: (leaf) => {
        return "return " + toJS(leaf.value)
    },
    YIELD: (leaf) => {
        return "yield " + toJS(leaf.value)
    },
    YIELD_IN: (leaf) => {
        return "yield* " + toJS(leaf.enumerator)
    },
    CALL: (leaf) => {
        return toJS(leaf.fun) + "(" + leaf.params.map(p => toJS(p)).join(", ") + ")"
    },
    INTEGER: (leaf) => {
        return leaf.value + "n"
    },
    FLOAT: (leaf) => {
        return leaf.value + ""
    },
    DICT_VALUE: (leaf) => {
        return "{ " + leaf.elements.map(e => toJS(e)).join(", ") + " }"
    },
    DICT_KEY_VALUE: (leaf) => {
        return toJS(leaf.key) + ": " + toJS(leaf.value)
    },
    BOOLEAN: (leaf) => {
        return leaf.value ? "true" : "false"
    },
    ASSIGN: (leaf) => {
        if (leaf.operator !== "++=") {
            return toJS(leaf.variable) + " " +
                (leaf.operator === ":=" ? "=" : leaf.operator) +
                " " + toJS(leaf.value)
        } else
        {
            return toBracketJS(leaf.variable) + ".push(..." +
                toBracketJS(leaf.value) + ")"
        }
    },
    MUTABLE_PARAM: (leaf) => "{ ref: " + toJS(leaf.value) + " }",
    WHILE: (leaf) =>
        (leaf.label ? leaf.label + ": " : "") +
        "while (" + toJS(leaf.condition) + ") " + toJS(leaf.body),
    ITER: (leaf) => {
        return (leaf.label ? leaf.label + ": " : "") +
            "while (true) {\n" + indent(toJS(leaf.body) + "\nbreak") + "\n}"
    },
    NEXT: (leaf) => "continue" + (leaf.label ? " " + leaf.label : ""),
    BREAK: (leaf) => "break" + (leaf.label ? " " + leaf.label : ""),
    RANGE: (leaf) => "range(\"" + leaf.op + "\", " +
        ([leaf.first, leaf.lastOrCount].concat(leaf.step ? [leaf.step] : [])).map(toJS).join(", ") + ")",
    MODIFY_REC: (leaf) => "{ ..." + toBracketJS(leaf.record) +
        leaf.changes.map(c => ", " + toJS(c)).join("") + " }",
    SPLAT: (leaf) => "..." + toJS(leaf.value),
    GET_MEMBER: (leaf) => {
        return toJS(leaf.container) + "." + leaf.path.join(".")
    },
    RESUME: "resume instruction",
    LINKED_LIST: (leaf) => {
        return "{ hd: " + toJS(leaf.head) + ", tl: " + toJS(leaf.tail) + " }"
    },
    LINKED_LIST_TYPE: (leaf) => toBracketJS(leaf.type) + "<>",
    EMPTY_LINKED_LIST: function() {
        return "[]"
    },
    WISE_BLOCK: (leaf) => {
        return "{\n" +
            indent("const $ = " + toJS(leaf.key) + "\n" +
            leaf.statements.map(s => toJS(s)).join("\n"))
            + "\n}"
    },
    GET_WISE_MEMBER: (leaf) => "$." + leaf.path.join("."),
    CARTESIAN_PROD: "cartesian product",
    CARTESIAN_POWER: "cartesian power",
    PROP_DEF: (leaf) => leaf.name + " " + toJS(leaf.type),
    CONCAT_TYPE: (leaf) => leaf.types.map(toBracketJS).join(" ++ "),
    INHERITANCE: (leaf) => "..." + toJS(leaf.parent),
    SET_DIFF: "set difference",
    SET_UNION: "set union",
    SET_INTER: "set intersection",
    SET_COMPARISON: "set comparison",
    SET_COMPARISON_OPERAND: "set comparison operand",
    SPECIALIZE_TYPE: (leaf) => {
        return leaf.params.map(p => toJS(p) + "-") + toJS(leaf.base)
    },
    WITH_EFFECT: (leaf) => "/* effects */ " + toJS(leaf.value),
    TRAIT_INTER: (leaf) => leaf.traits.map(t => toBracketJS(t)).join(" & "),
    TRAIT_DEF: (leaf) =>
        "/* trait " + leaf.genericParams.map(p => p + "-") + leaf.name +
        (leaf.alias ? " (" + leaf.alias + ")" : "") + " {\n" +
        indent(leaf.features.map(feature => toJS(feature)).join("\n")) + "\n} */",
    TRAIT_CONSTRAINT: (leaf) => {
        return leaf.type + (leaf.constraint ? " " + toJS(leaf.constraint) : "")
    },
    CREATE_OBJECT: (leaf) => {
        return "new " + toJS(leaf.type) +
            (tree.is(leaf.params, tree.TUPLE)
                ? toJS(leaf.params)
                : "(" + toJS(leaf.params) + ")")
    },
    COMPARISON_PATTERN: (leaf) => {
        return leaf.operator + " " + toJS(leaf.value)
    },
    ABSTRACT_BODY: () => { return "/* abstract */" },
    FOR_EACH: (leaf) => {
        return (leaf.label ? leaf.label + ": " : "") +
            "for (const " + leaf.variable + " of " + toJS(leaf.key) + ") " + toJS(leaf.body)
    }
}

module.exports = {
    transpile
};