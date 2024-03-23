// Abstract syntax tree (AST) functions

function check(tree, error)
{
    if (!isLeaf(tree))
        return tree

    switch (tree._)
    {
        case CONST_DEF:
        case VAR_DEF:
            if (is(tree.type, VOID_TYPE))
                error("Void type cannot be specified for constants and variables")
            if (is(tree.type, GENERIC_PARAM_TYPE))
                error("Generic parameters cannot be used in constants and variables")
            if (is(tree, CONST_DEF)) {
                if (is(tree.type, FUN_TYPE) || is(tree.value, FUN_DEF)
                  || is(tree.value, COMPOSE)) {
                    error("Constants cannot be of function type. Declare them with 'fun' instead.")
                }
            }
            break

        case TYPE_DEF:
        case ALIAS_DEF:
            if (is(tree.type, GENERIC_PARAM_TYPE))
                error("Generic parameters cannot be used in type definitions")
            break

        case CALL:
            checkFunctionCall(tree.fun, error)
            break

        case FUN_DEF:
            if (tree.kind === "sub" && !is(tree.returnType, VOID_TYPE))
                error("Subroutines should have a void return type")
            if (tree.params) {
                if (tree.params.length === 0 && tree.name !== "new")
                    error("Functions always have a parameter (can be void)")
                for (let i = 1; i < tree.params.length; i++)
                    if (is(tree.params[i].type, VOID_TYPE))
                        error("Void type is only allowed for the first parameter")
            }
            break

        case FOR_EACH:
        case REPEAT:
        case WHILE:
            findContinueBreak(tree.body, (node) => node.loop = tree)
    }

    return tree
}

export function findContinueBreak(tree, callback) {
    if (Array.isArray(tree))
    {
        for (const item of tree)
            findContinueBreak(item, callback)
        return
    }

    if (!isLeaf(tree))
        return

    switch (tree._)
    {
        case FOR_EACH:
        case REPEAT:
        case WHILE:
            return

        case BREAK:
        case NEXT:
            callback(tree)
            break

        default:
            for (const prop in tree)
                findContinueBreak(tree[prop], callback)
    }
}

export function fixFunction(f, error)
{
    if (is(f.body, CASE) && f.body.key === undefined) {
        const params = f.params.filter(p => !is(p.type, VOID_TYPE))
            .flatMap(p =>
                p.names.map(
                    n => leaf(RESOLVED_VARIABLE, { ref: n }, error)
                )
            )
        if (params.length === 0)
            error("No parameter to be matched")
        else if (params.length === 1)
            f.body.key = params[0]
        else
            f.body.key = leaf(TUPLE, {
                values: params
            }, error)
    }
    if (f.kind === "fun")
        f.body = addLastReturn(f.body, error)
}

function addLastReturn(body, error)
{
    if (!isLeaf(body) || is(body, TYPE_DEF) || is(body, TRAIT_DEF) || is(body, ALIAS_DEF)
        || is(body, VAR_DEF) || is(body, FUN_DEF) || is(body, RETURN))
        return body

    if (is(body, CODE_BLOCK) || is(body, WISE_BLOCK)) {
        if (body.statements.length > 0)
        {
            const last = body.statements[body.statements.length-1]
            if (is(last, CODE_BLOCK) || is(last, WISE_BLOCK) ||
                is(last, TERNARY) || is(last, CASE))
            {
                addLastReturn(last, error)
            }
            else
            {
                body.statements[body.statements.length-1] = addLastReturn(last, error)
            }
        }
    }
    else if (is(body, TERNARY))
    {
        body.ifTrue = addLastReturn(body.ifTrue, error)
        body.ifFalse = addLastReturn(body.ifFalse, error)
    }
    else if (is(body, CASE))
    {
        body.cases.forEach(c => c.value = addLastReturn(c.value, error))
    }
    else if (!is(body, TYPE_DEF) && !is(body, TRAIT_DEF) && !is(body, ALIAS_DEF)
        && !is(body, VAR_DEF) && !is(body, FUN_DEF) && !is(body, RETURN))
        return leaf(RETURN, { value: body }, error)

    return body
}

function checkFunctionCall(node, error)
{
    if (!isLeaf(node))
        return

    if (is(node, TUPLE))
        node.values.forEach(value => checkFunctionCall(value, error))

    if ([VOID_VALUE, STRING_VALUE, LIST, SET, REC_VALUE, CREATE_OBJECT,
         INTEGER, FLOAT, DICT_VALUE, BOOLEAN, LINKED_LIST].includes(node._))
        error(capitalFirst(leafName[node._]) + " cannot be used as a function")
}

function capitalFirst(text)
{
    return text[0].toUpperCase() + text.slice(1)
}

export function getLambdaVariables(tree)
{
    if (Array.isArray(tree))
    {
        let variables = []
        for (const item of tree)
            variables = variables.concat(getLambdaVariables(item))
        return variables
    }
    if (!isLeaf(tree))
        return []

    switch (tree._)
    {
        case FUN_DEF:
            return []
        case IMPLICIT_PARAM:
            return [tree.name]
        default:
            let variables = []
            for (const prop in tree)
            {
                variables = variables.concat(getLambdaVariables(tree[prop]))
            }
            return variables
    }
}

export function isLeaf(tree)
{
    return tree !== null && typeof tree === 'object' && tree._
}

export function is(tree, kind)
{
    if (!isLeaf(tree))
        return false

    return tree._ === kind
}

export function leaf(kind, leaf, error)
{
    if (!error)
        throw new Error("Error function not provided")
    if (!kind)
        throw new Error("Kind is not defined")
    leaf._ = kind
    return check(leaf, error)
}

export const MODULE = "MODULE"
export const CONST_DEF = "CONST_DEF"
export const VAR_DEF = "VAR_DEF"
export const NAMES = "NAMES"
export const IDENTIFIER = "IDENTIFIER"
export const DECONSTRUCT_TUPLE = "DECONSTRUCT_TUPLE"
export const DECONSTRUCT_RECORD = "DECONSTRUCT_RECORD"
export const DECONSTRUCT_MEMBER = "DECONSTRUCT_MEMBER"
export const DECONSTRUCT_NAME = "DECONSTRUCT_NAME"
export const FUN_DEF = "FUN_DEF"
export const INLINE_ENUM = "INLINE_ENUM"
export const FUN_PARAM_DEF = "FUN_PARAM_DEF"
export const VOID_TYPE = "VOID_TYPE"
export const CODE_BLOCK = "CODE_BLOCK"
export const TYPE_DEF = "TYPE_DEF"
export const ALIAS_DEF = "ALIAS_DEF"
export const UNION_TYPE = "UNION_TYPE"
export const FUN_TYPE = "FUN_TYPE"
export const TUPLE_TYPE = "TUPLE_TYPE"
export const TUPLE_POWER_TYPE = "TUPLE_POWER_TYPE"
export const OPTION_TYPE = "OPTION_TYPE"
export const LIST_TYPE = "LIST_TYPE"
export const SET_TYPE = "SET_TYPE"
export const DICT_TYPE = "DICT_TYPE"
export const REC_TYPE = "REC_TYPE"
export const REC_FIELD_TYPE = "REC_FIELD_TYPE"
export const GENERIC_PARAM_TYPE = "GENERIC_PARAM_TYPE"
export const ANY_TYPE = "ANY_TYPE"
export const DISJUNCTION = "DISJUNCTION"
export const XOR = "XOR"
export const CONJUNCTION = "CONJUNCTION"
export const COMPARISON = "COMPARISON"
export const COMPARISON_OPERAND = "COMPARISON_OPERAND"
export const CONCAT = "CONCAT"
export const ADDITION = "ADDITION"
export const TERM = "TERM"
export const MULTIPLICATION = "MULTIPLICATION"
export const FACTOR = "FACTOR"
export const NOT = "NOT"
export const EXPONENTIATION = "EXPONENTIATION"
export const INDEXING = "INDEXING"
export const VALUE_BY_NAME = "VALUE_BY_NAME"
export const TYPE_BY_NAME = "TYPE_BY_NAME"
export const RESOLVED_VARIABLE = "RESOLVED_VARIABLE"
export const TUPLE = "TUPLE"
export const REC_VALUE = "REC_VALUE"
export const REC_FIELD_VALUE = "REC_FIELD_VALUE"
export const STRING_VALUE = "STRING_VALUE"
export const STRING_PART = "STRING_PART"
export const FORMATTED_VALUE = "FORMATTED_VALUE"
export const IMPLICIT_PARAM = "IMPLICIT_PARAM"
export const VOID_VALUE = "VOID_VALUE"
export const LIST = "LIST"
export const SET = "SET"
export const DEFAULT_VALUE = "DEFAULT_VALUE"
export const TERNARY = "TERNARY"
export const CASE = "CASE"
export const CASE_OPTION = "CASE_OPTION"
export const CAPTURE = "CAPTURE"
export const TAGGED_VALUE = "TAGGED_VALUE"
export const RETURN = "RETURN"
export const YIELD = "YIELD"
export const YIELD_IN = "YIELD_IN"
export const COMPOSE = "COMPOSE"
export const CALL = "CALL"
export const INTEGER = "INTEGER"
export const FLOAT = "FLOAT"
export const DICT_VALUE = "DICT_VALUE"
export const DICT_KEY_VALUE = "DICT_KEY_VALUE"
export const BOOLEAN = "BOOLEAN"
export const MUTABLE_PARAM = "MUTABLE_PARAM"
export const WHILE = "WHILE"
export const REPEAT = "REPEAT"
export const NEXT = "NEXT"
export const BREAK = "BREAK"
export const RANGE = "RANGE"
export const MODIFY_REC = "MODIFY_REC"
export const SPLAT = "SPLAT"
export const GET_MEMBER = "GET_MEMBER"
export const RESUME = "RESUME"
export const LINKED_LIST = "LINKED_LIST"
export const LINKED_LIST_TYPE = "LINKED_LIST_TYPE"
export const EMPTY_LINKED_LIST = "EMPTY_LINKED_LIST"
export const WISE_BLOCK = "WISE_BLOCK"
export const GET_WISE_MEMBER = "GET_WISE_MEMBER"
export const CARTESIAN_PROD = "CARTESIAN_PROD"
export const CARTESIAN_POWER = "CARTESIAN_POWER"
export const PROP_DEF = "PROP_DEF"
export const CONCAT_TYPE = "CONCAT_TYPE"
export const INHERITANCE = "INHERITANCE"
export const SET_DIFF = "SET_DIFF"
export const SET_UNION = "SET_UNION"
export const SET_INTER = "SET_INTER"
export const SET_COMPARISON = "SET_COMPARISON"
export const SET_COMPARISON_OPERAND = "SET_COMPARISON_OPERAND"
export const SPECIALIZE_TYPE = "SPECIALIZE_TYPE"
export const TRAIT_INTER = "TRAIT_INTER"
export const TRAIT_DEF = "TRAIT_DEF"
export const TRAIT_CONSTRAINT = "TRAIT_CONSTRAINT"
export const CREATE_OBJECT = "CREATE_OBJECT"
export const COMPARISON_PATTERN = "COMPARISON_PATTERN"
export const ABSTRACT_BODY = "ABSTRACT_BODY"
export const FOR_EACH = "FOR_EACH"
export const USE = "USE"
export const JS = "JS"

const leafName = {
    MODULE: "module",
    CONST_DEF: "constant definition",
    VAR_DEF: "variable definition",
    NAMES: "names",
    IDENTIFIER: "identifier",
    DECONSTRUCT_TUPLE: "tuple deconstruction",
    DECONSTRUCT_RECORD: "record deconstruction",
    DECONSTRUCT_MEMBER: "record member deconstruction",
    DECONSTRUCT_NAME: "name for deconstruction",
    FUN_DEF: "function definition",
    FUN_PARAM_DEF: "function parameter definition",
    INLINE_ENUM: "inline enumerator",
    VOID_TYPE: "void type",
    CODE_BLOCK: "code block",
    TYPE_DEF: "type definition",
    ALIAS_DEF: "type alias definition",
    UNION_TYPE: "union type",
    FUN_TYPE: "function type",
    TUPLE_TYPE: "tuple type",
    TUPLE_POWER_TYPE: "tuple power type",
    OPTION_TYPE: "option type",
    LIST_TYPE: "list type",
    SET_TYPE: "set type",
    DICT_TYPE: "dictionary type",
    REC_TYPE: "record type",
    REC_FIELD_TYPE: "record field type",
    GENERIC_PARAM_TYPE: "generic parameter type",
    ANY_TYPE: "any type",
    DISJUNCTION: "disjunction",
    XOR: "xor operation",
    CONJUNCTION: "conjunction",
    COMPARISON: "comparison",
    COMPARISON_OPERAND: "comparison operand",
    CONCAT: "concatenation",
    ADDITION: "addition",
    TERM: "term",
    MULTIPLICATION: "multiplication",
    FACTOR: "factor",
    NOT: "not operation",
    EXPONENTIATION: "exponentiation",
    INDEXING: "indexing",
    COMPOSE: "composition",
    VALUE_BY_NAME: "value by name",
    TYPE_BY_NAME: "type by name",
    RESOLVED_VARIABLE: "resolved variable",
    TUPLE: "tuple",
    REC_VALUE: "record value",
    REC_FIELD_VALUE: "record field value",
    STRING_VALUE: "string value",
    STRING_PART: "string part",
    FORMATTED_VALUE: "formatted value",
    IMPLICIT_PARAM: "implicit parameter",
    VOID_VALUE: "void value",
    LIST: "list",
    SET: "set",
    DEFAULT_VALUE: "coalescence",
    TERNARY: "ternary expression",
    CASE: "case expression",
    CASE_OPTION: "case option",
    CAPTURE: "capture name",
    TAGGED_VALUE: "tagged value",
    RETURN: "return statement",
    YIELD: "yield statement",
    YIELD_IN: "recursive yield statement",
    CALL: "function call",
    INTEGER: "integer",
    FLOAT: "floating point value",
    DICT_VALUE: "dictionary value",
    DICT_KEY_VALUE: "dictionary entry value",
    BOOLEAN: "boolean value",
    MUTABLE_PARAM: "mutable parameter",
    WHILE: "while loop",
    REPEAT: "repeat loop",
    NEXT: "next loop instruction",
    BREAK: "break loop instruction",
    RANGE: "range",
    MODIFY_REC: "modified record",
    SPLAT: "splat operation",
    GET_MEMBER: "member expression",
    RESUME: "resume instruction",
    LINKED_LIST: "linked list value",
    LINKED_LIST_TYPE: "linked list type",
    EMPTY_LINKED_LIST: "empty linked list",
    WISE_BLOCK: "wise block",
    GET_WISE_MEMBER: "get member of wise block",
    CARTESIAN_PROD: "cartesian product",
    CARTESIAN_POWER: "cartesian power",
    PROP_DEF: "property definition",
    CONCAT_TYPE: "type concatenation",
    INHERITANCE: "inheritance",
    SET_DIFF: "set difference",
    SET_UNION: "set union",
    SET_INTER: "set intersection",
    SET_COMPARISON: "set comparison",
    SET_COMPARISON_OPERAND: "set comparison operand",
    SPECIALIZE_TYPE: "type specialization",
    TRAIT_INTER: "trait intersection",
    TRAIT_DEF: "trait definition",
    TRAIT_CONSTRAINT: "trait constraint",
    CREATE_OBJECT: "object creation",
    COMPARISON_PATTERN: "comparison pattern",
    ABSTRACT_BODY: "abstract body",
    FOR_EACH: "for loop",
    USE: "use module",
    JS: "javascript"
}