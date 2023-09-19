// Abstract syntax tree (AST) functions

function check(tree, error)
{
    if (!isLeaf(tree))
        return tree

    switch (tree._)
    {
        case CONST_DEF:
        case VAR_DEF:
            if (isVoid(tree.type))
                error("Void type cannot be specified for constants and variables")
            if (isGenericParamType(tree.type))
                error("Generic parameters cannot be used in constants and variables")
            break;

        case TYPE_DEF:
            if (isGenericParamType(tree.type))
                error("Generic parameters cannot be used in type definitions")
            break;

        case CALL:
            checkFunction(tree.fun, error)
    }

    return tree
}

function checkFunction(node, error)
{
    if (!isLeaf(node))
        return

    if (isNodeOf(node, TUPLE))
        node.values.forEach(value => checkFunction(value, error))

    if ([VOID_VALUE, STRING_VALUE, LIST, SET,
         INTEGER, FLOAT, DICT_VALUE, BOOLEAN, LINKED_LIST].includes(node._))
        error(capitalFirst(leafName[node._]) + " cannot be used as a function")
}

function capitalFirst(text)
{
    return text[0].toUpperCase() + text.slice(1)
}

function getLambdaVariables(tree)
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

function isLeaf(tree)
{
    return tree !== null && typeof tree === 'object' && tree._
}

function isVoid(tree)
{
    return isNodeOf(tree, "void")
}

function isGenericParamType(tree)
{
    return isNodeOf(tree, "genericParamType")
}

function isNodeOf(tree, kind)
{
    if (!isLeaf(tree))
        return false

    return tree._ === kind
}

function leaf(kind, leaf, error)
{
    if (!error)
        throw new Error("Error function not provided")
    if (!kind)
        throw new Error("Kind is not defined")
    leaf._ = kind
    return check(leaf, error)
}

const MODULE = "MODULE"
const CONST_DEF = "CONST_DEF"
const VAR_DEF = "VAR_DEF"
const NAMES = "NAMES"
const DECONSTRUCT_TUPLE = "DECONSTRUCT_TUPLE"
const DECONSTRUCT_RECORD = "DECONSTRUCT_RECORD"
const DECONSTRUCT_MEMBER = "DECONSTRUCT_MEMBER"
const DECONSTRUCT_NAME = "DECONSTRUCT_NAME"
const FUN_DEF = "FUN_DEF"
const FUN_PARAM_DEF = "FUN_PARAM_DEF"
const VOID_TYPE = "VOID_TYPE"
const CODE_BLOCK = "CODE_BLOCK"
const TYPE_DEF = "TYPE_DEF"
const UNION_TYPE = "UNION_TYPE"
const TAGGED_TYPE = "TAGGED_TYPE"
const FUN_TYPE = "FUN_TYPE"
const TUPLE_TYPE = "TUPLE_TYPE"
const TUPLE_POWER_TYPE = "TUPLE_POWER_TYPE"
const VOIDABLE_TYPE = "VOIDABLE_TYPE"
const LIST_TYPE = "LIST_TYPE"
const SEQ_TYPE = "SEQ_TYPE"
const SET_TYPE = "SET_TYPE"
const DICT_TYPE = "DICT_TYPE"
const REC_TYPE = "REC_TYPE"
const REC_MEMBER_TYPE = "REC_MEMBER_TYPE"
const NAMED_TYPE = "NAMED_TYPE"
const GENERIC_PARAM_TYPE = "GENERIC_PARAM_TYPE"
const ANY_TYPE = "ANY_TYPE"
const DISJUNCTION = "DISJUNCTION"
const XOR = "XOR"
const CONJUNCTION = "CONJUNCTION"
const COMPARISON = "COMPARISON"
const COMPARISON_OPERAND = "COMPARISON_OPERAND"
const CONCAT = "CONCAT"
const ADDITION = "ADDITION"
const TERM = "TERM"
const MULTIPLICATION = "MULTIPLICATION"
const FACTOR = "FACTOR"
const NOT = "NOT"
const EXPONENTIATION = "EXPONENTIATION"
const INDEXING = "INDEXING"
const VALUE_BY_NAME = "VALUE_BY_NAME"
const TUPLE = "TUPLE"
const REC_VALUE = "REC_VALUE"
const REC_MEMBER_VALUE = "REC_MEMBER_VALUE"
const STRING_VALUE = "STRING_VALUE"
const STRING_PART = "STRING_PART"
const FORMATTED_VALUE = "FORMATTED_VALUE"
const IMPLICIT_PARAM = "IMPLICIT_PARAM"
const VOID_VALUE = "VOID_VALUE"
const LIST = "LIST"
const SET = "SET"
const DEFAULT_VALUE = "DEFAULT_VALUE"
const TERNARY = "TERNARY"
const CASE = "CASE"
const CASE_OPTION = "CASE_OPTION"
const CAPTURE = "CAPTURE"
const TAGGED_VALUE = "TAGGED_VALUE"
const RETURN = "RETURN"
const COMPOSE = "COMPOSE"
const CALL = "CALL"
const INTEGER = "INTEGER"
const FLOAT = "FLOAT"
const DICT_VALUE = "DICT_VALUE"
const DICT_KEY_VALUE = "DICT_KEY_VALUE"
const BOOLEAN = "BOOLEAN"
const ASSIGN = "ASSIGN"
const MUTABLE_PARAM = "MUTABLE_PARAM"
const WHILE = "WHILE"
const RANGE = "RANGE"
const MODIFY_REC = "MODIFY_REC"
const SPLAT = "SPLAT"
const GET_MEMBER = "GET_MEMBER"
const RESUME = "RESUME"
const LINKED_LIST = "LINKED_LIST"
const LINKED_LIST_TYPE = "LINKED_LIST_TYPE"
const EMPTY_LINKED_LIST = "EMPTY_LINKED_LIST"
const WISE_BLOCK = "WITH_BLOCK"
const GET_WISE_MEMBER = "GET_WITH_MEMBER"
const CARTESIAN_PROD = "CARTESIAN_PROD"
const CARTESIAN_POWER = "CARTESIAN_POWER"
const PROP_DEF = "PROP_DEF"
const INHERITANCE = "INHERITANCE"
const SET_DIFF = "SET_DIFF"
const SET_UNION = "SET_UNION"
const SET_INTER = "SET_INTER"
const SET_COMPARISON = "SET_COMPARISON"
const SET_COMPARISON_OPERAND = "SET_COMPARISON_OPERAND"

leafName = {
    MODULE: "module",
    CONST_DEF: "constant definition",
    VAR_DEF: "variable definition",
    NAMES: "names",
    DECONSTRUCT_TUPLE: "tuple deconstruction",
    DECONSTRUCT_RECORD: "record deconstruction",
    DECONSTRUCT_MEMBER: "record member deconstruction",
    DECONSTRUCT_NAME: "name for deconstruction",
    FUN_DEF: "function definition",
    FUN_PARAM_DEF: "function parameter definition",
    VOID_TYPE: "void type",
    CODE_BLOCK: "code block",
    TYPE_DEF: "type definition",
    UNION_TYPE: "union type",
    TAGGED_TYPE: "tagged type",
    FUN_TYPE: "function type",
    TUPLE_TYPE: "tuple type",
    TUPLE_POWER_TYPE: "tuple power type",
    VOIDABLE_TYPE: "voidable type",
    LIST_TYPE: "list type",
    SEQ_TYPE: "sequence type",
    SET_TYPE: "set type",
    DICT_TYPE: "dictionary type",
    REC_TYPE: "record type",
    REC_MEMBER_TYPE: "record member type",
    NAMED_TYPE: "named type",
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
    TUPLE: "tuple",
    REC_VALUE: "record value",
    REC_MEMBER_VALUE: "record member value",
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
    CALL: "function call",
    INTEGER: "integer",
    FLOAT: "floating point value",
    DICT_VALUE: "dictionary value",
    DICT_KEY_VALUE: "dictionary entry value",
    BOOLEAN: "boolean value",
    ASSIGN: "assignment",
    MUTABLE_PARAM: "mutable parameter",
    WHILE: "while loop",
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
    INHERITANCE: "inheritance",
    SET_DIFF: "set difference",
    SET_UNION: "set union",
    SET_INTER: "set intersection",
    SET_COMPARISON: "set comparison",
    SET_COMPARISON_OPERAND: "set comparison operand"
}

module.exports = {
    leaf,
    getLambdaVariables,
    isNodeOf,
    MODULE,
    CONST_DEF,
    VAR_DEF,
    NAMES,
    DECONSTRUCT_TUPLE,
    DECONSTRUCT_RECORD,
    DECONSTRUCT_MEMBER,
    DECONSTRUCT_NAME,
    FUN_DEF,
    FUN_PARAM_DEF,
    VOID_TYPE,
    CODE_BLOCK,
    TYPE_DEF,
    UNION_TYPE,
    TAGGED_TYPE,
    FUN_TYPE,
    TUPLE_TYPE,
    TUPLE_POWER_TYPE,
    VOIDABLE_TYPE,
    LIST_TYPE,
    SEQ_TYPE,
    SET_TYPE,
    DICT_TYPE,
    REC_TYPE,
    REC_MEMBER_TYPE,
    NAMED_TYPE,
    GENERIC_PARAM_TYPE,
    ANY_TYPE,
    DISJUNCTION,
    XOR,
    CONJUNCTION,
    COMPARISON,
    COMPARISON_OPERAND,
    CONCAT,
    ADDITION,
    TERM,
    MULTIPLICATION,
    FACTOR,
    NOT,
    EXPONENTIATION,
    INDEXING,
    COMPOSE,
    VALUE_BY_NAME,
    TUPLE,
    REC_VALUE,
    REC_MEMBER_VALUE,
    STRING_VALUE,
    STRING_PART,
    FORMATTED_VALUE,
    IMPLICIT_PARAM,
    VOID_VALUE,
    LIST,
    SET,
    DEFAULT_VALUE,
    TERNARY,
    CASE,
    CASE_OPTION,
    CAPTURE,
    TAGGED_VALUE,
    RETURN,
    CALL,
    INTEGER,
    FLOAT,
    DICT_VALUE,
    DICT_KEY_VALUE,
    BOOLEAN,
    ASSIGN,
    MUTABLE_PARAM,
    WHILE,
    RANGE,
    MODIFY_REC,
    SPLAT,
    GET_MEMBER,
    RESUME,
    LINKED_LIST,
    LINKED_LIST_TYPE,
    EMPTY_LINKED_LIST,
    WISE_BLOCK,
    GET_WISE_MEMBER,
    CARTESIAN_PROD,
    CARTESIAN_POWER,
    PROP_DEF,
    INHERITANCE,
    SET_DIFF,
    SET_UNION,
    SET_INTER,
    SET_COMPARISON,
    SET_COMPARISON_OPERAND
};