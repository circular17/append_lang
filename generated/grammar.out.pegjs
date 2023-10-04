// grammar using a custom PEG format
// it needs to be preprocessed by grammar-preprocessor

/************* MODULE STRUCTURE *************/

module
    = __ s:moduleStatements __
        { return tree.leaf(tree.MODULE, { statements: s }, error) }

moduleStatements // []
    = hd:moduleStatement tl:(statementSeparator s:moduleStatement { return s })*
        { return [hd].concat(tl).flat() }
    / "" { return [] }

statement
    = defs
    / vars
    / typeDefs
    / fun
    / branch

moduleStatement // [] or leaf
    = statement
    / explicitModuleBlock

statementSeparator
    = _ d:"." " " _ { return d }
    / eol

defs // []
    = "let" _ hd:def tl:(_ ";" __ d:def { return d })*
        { return [hd].concat(tl) }

def
    = _ names:(names / deconstruct) _ type:type? _ "=" _ v:branch
        { return tree.leaf(tree.CONST_DEF, { names, type, value: v }, error) }

vars // []
    = "var" _ hd:var tl:(_ ";" __ v:var { return v })*
        { return [hd].concat(tl) }

var
    = names:(names / deconstruct) _ "=" _ v:branch
        { return tree.leaf(tree.VAR_DEF, { names, type: null, value: v }, error) }
    / names:(names / deconstruct) _ type:type _ v:("=" _ v:branch { return v })?
        { return tree.leaf(tree.VAR_DEF, { names, type, value: v }, error) }

names = i:identifiers { return tree.leaf(tree.NAMES, { identifiers:i }, error) }

deconstruct
    = "(" __ hd:deconstructElement tl:(_ "," __ e:deconstructElement { return e })* __ ")"
        { return tree.leaf(tree.DECONSTRUCT_TUPLE, { elements: [hd].concat(tl) }, error) }
    / "{|" __ hd:deconstructMember tl:(_ "," __ e:deconstructMember { return e })* __ "|}"
        { return tree.leaf(tree.DECONSTRUCT_RECORD, { elements: [hd].concat(tl) }, error) }

deconstructMember
    = member:id value:(_ colon __ e:(deconstructElement / deconstruct) { return e })?
        { return tree.leaf(tree.DECONSTRUCT_MEMBER, { member, value }, error) }

deconstructElement
    = id:id { return tree.leaf(tree.DECONSTRUCT_NAME, { name: id }, error) }
    / deconstruct

typeDefs // []
    = "type" _ hd:typeDef tl:(_ ";" __ t:typeDef { return t })*
        { return [hd].concat(tl) }
    / "trait" _ t:traitDef {
        return [t]
    }
    / "alias" _ hd:aliasDef tl:(_ ";" __ t:aliasDef { return t })*
        { return [hd].concat(tl) }

traitDef
    = traitConstraints:traitConstraints _ genericParams:genericParams _ name:id alias:(_ "(" o:overridableOp ")" { return o })?
    __ "{" __ features:features __ "}"
        { return tree.leaf(tree.TRAIT_DEF, {
            name,
            traitConstraints,
            genericParams,
            alias,
            features
        }, error) }

traitConstraints
    = leftAngleBracket _ hd:traitConstraint tl:(_ "," __ c:traitConstraint { return c })* _ rightAngleBracket
        { return [hd].concat(tl) }
    / "" { return [] }
traitConstraint
    = type:id constraint:(_ t:type { return t })?
        { return tree.leaf(tree.TRAIT_CONSTRAINT, { type, constraint }, error) }

features
    = hd:feature tl:((_ "," __ / eol) f:feature { return f })*
        { return [hd].concat(tl) }

feature
    = funHeader
    / propHeader
    / traitInheritance

traitInheritance
    = "..." parent:typeByName { return tree.leaf(tree.INHERITANCE, { parent }, error) }

typeDef
    = traitConstraints:traitConstraints _ genericParams:genericParams _ name:typeId _ type:type
        { return tree.leaf(tree.TYPE_DEF, {
            name,
            traitConstraints,
            genericParams,
            type
        }, error) }

genericParams
    = (i:typeId _ "-" { return i })*

aliasDef
    = genericParams:genericParams _ name:typeId _ "=" _ type:type
        { return tree.leaf(tree.ALIAS_DEF, {
            name,
            genericParams,
            type
        }, error) }

explicitModuleBlock
    = "do" effects:effects? __ s:moduleStatements __ when:when? __ "end"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s, effects: effects ?? [], when }, error) }

when
    = "when" (eol pipe _ / _) c:caseBody { return c }

/************* FUNCTION DEFINITION *************/

fun
    = f:funHeader _ body:funBody {
        f.body = body
        return f
    }

funHeader
    = isGlobal:isGlobal isAsync:isAsync purity:purity
    kind:funKind _ traitConstraints:traitConstraints _ hd:funParam __ "\"" keywordName:$(keyword?) name:(id / overridableOp)? "\""
    tl:(__ p:params { return p })? returnType:(_ colon __ t:type { return t })?
    effects:(__ "with" _ e:identifiers { return e })? {
        if (keywordName)
            error(`The keyword '${keywordName}' cannot be used as an identifier`)
        if (!name)
            error("The name of the function is not specified")
        return tree.leaf(tree.FUN_DEF, {
            isGlobal,
            isAsync,
            purity,
            kind,
            name,
            traitConstraints,
            params: [hd].concat(tl ?? []),
            effects: effects ?? [],
            returnType
        }, error)
    }

isGlobal
    = "global" _ { return true }
    / "" { return false }

isAsync
    = "async" _ { return true }
    / "" { return false }

funParam
    = mut:("mut" _)? names:identifiers type:(_ t:type { return t })?
        { return tree.leaf(tree.FUN_PARAM_DEF, { names, type, mutable: !!mut }, error) }
    / void
        { return tree.leaf(tree.FUN_PARAM_DEF, {
            names:[""],
            type: tree.leaf(tree.VOID_TYPE, {}, error)
        }, error) }

params // []
    = hd:funParam tl:(_ ";" __ p:funParam { return p })*
        { return [hd].concat(tl) }

funKind
    = "fun" / "sub" / "enum"

purity
    = p:"state" _ { return p }
    / "" { return "pure" }

funBody
    = __ "=>" __ v:branch { return v }
    / __ "=>" _ "?" { return null }
    / __ explicitFunBlock
    / case

explicitFunBlock
    = "do" effects:effects? __ s:funStatements __ when:when? __ "end"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s, effects: effects ?? [], when }, error) }

funStatements // []
    = hd:funStatement tl:(statementSeparator s:funStatement { return s })*
        { return [hd].concat(tl).flat() }
    / "" { return [] }

funStatement // [] or leaf
    = statement
    / explicitFunBlock
    / return

return
    = "return" _ value:branch
        { return tree.leaf(tree.RETURN, { value }, error) }
    / "yield" _ value:branch
            { return tree.leaf(tree.YIELD, { value }, error) }
    / "resume" { return tree.leaf(tree.RESUME, { }, error) }
    / "next" { return tree.leaf(tree.NEXT, { }, error) }
    / "break" { return tree.leaf(tree.BREAK, { }, error) }

/************* BRANCHING *************/

branch
    = optionWithPipe
    / loop

loop
    = "while" _ condition:valueExpr __ body:(branch / inlineBlock / return)
        { return tree.leaf(tree.WHILE, { condition, body }, error) }
    / "iter" body:(branch / inlineBlock / return)
        { return tree.leaf(tree.ITER, { body }, error) }

optionWithPipe
    = value:pipedExpr option:(ternary / case / wiseBlock)? {
        if (option) {
            option.key = value
            return option
        }
        else
            return value
    }

optionNoPipe
    = value:valueExpr option:(ternary / wiseBlock)? {
        if (option) {
            option.key = value
            return option
        }
        else
            return value
    }

wiseBlock
    = _ "wise" __ "{" __ statements:funStatements __ close:"}"? {
        if (!close)
            error("Expecting \")\" to close the 'wise' block")
        return tree.leaf(tree.WISE_BLOCK, {
            statements
        }, error)
    }

ternary
    = __ "?" __ ifTrue:(branch / inlineBlock / return) ifFalse:(__ "else" __ v:(branch / inlineBlock / return) { return v })?
        { return tree.leaf(tree.TERNARY, { ifTrue, ifFalse }, error) }
case
    = _ ("case" _ / eol pipe _) c:caseBody { return c }

inlineBlock
    = "{" !(([-+&*^] / [><] "="? / "!=") "}") __ statements:funStatements __ "}"
        { return tree.leaf(tree.CODE_BLOCK, { statements }, error) }

/************* PATTERN MATCHING *************/

caseBody
    = hd:caseOption tl:(__ pipe _ o:caseOption { return o })*
    otherValue:(__ "other" __ value:(branch / inlineBlock / return) { return value })? {
        var options = [hd].concat(tl)
        if (otherValue)
            options.push(tree.leaf(tree.CASE_OPTION, {
                pattern: tree.leaf(tree.CAPTURE, { name: "_" }, error),
                value: otherValue }, error))
        return tree.leaf(tree.CASE, { options }, error)
    }

caseOption
    = hd:pattern tl:(_ "," __ p:pattern { return p })* __ "->" __ value:(branch / inlineBlock / return)
        { return tree.leaf(tree.CASE_OPTION, { patterns: [hd].concat(tl), value }, error) }

pattern
    = taggedPattern
    / valueExpr

taggedPattern
    = tag:tag value:(_ pattern)?
        { return tree.leaf(tree.TAGGED_VALUE, {
            tag,
            value: value ?? tree.leaf(tree.VOID_VALUE, { }, error)
        }, error) }
    / linkedListPattern

linkedListPattern
    = head:untaggedPattern tail:(_ "::" __ t:linkedListPattern { return t })* {
        if (tail)
            return tree.leaf(tree.LINKED_LIST, {
                head,
                tail
            }, error)
        else
            return head
    }

untaggedPattern
    = capture
    / comparisonPattern
    / tuplePattern
    / recPattern
    / listPattern
    / setPattern
    / dictPattern

comparisonPattern
    = operator:(rightAngleBracket / ">=" / leftAngleBracket / "<=" / "in") _ value:aboveComparison

tuplePattern
    = "(" startEllipsis:(_ "...")? __ hd:pattern tl:(_ "," __ v:pattern { return v })*
    endEllipsis:(_ "..." _)? close:(__ ")")? {
        if (!close) {
            if (tl.length > 0)
                error("Expecting \")\" to close the tuple")
            else
                error("Expecting matching \")\"")
        }
        return tl.length > 0
            ? tree.leaf(tree.TUPLE, { values: [hd].concat(tl), startEllipsis: !!startEllipsis, endEllipsis: !!endEllipsis  }, error)
            : hd
        }

recPattern
    = "{|" _ ellipsis:("..." _)? "|}"
        { return tree.leaf(tree.REC_VALUE, { members: [], ellipsis: !!ellipsis }, error) }
    / "{|" __ hd:recMemberPattern tl:(_ "," __ m:recMemberPattern { return m })*
    ellipsis:(_ "..." _)? close:(__ "|}")? {
        if (!close) error("Expecting \"|\x7d\" to close the record")
        return tree.leaf(tree.REC_VALUE, { members: [hd].concat(tl), ellipsis: !!ellipsis }, error)
    }

recMemberPattern
    = name:id _ colon __ value:pattern
        { return tree.leaf(tree.REC_MEMBER_VALUE, { name, value }, error) }

listPattern
    = "[" _ ellipsis:("..." _)? "]"
        { return tree.leaf(tree.LIST, { values: [], startEllipsis: false, endEllipsis: !!ellipsis }, error) }

    / "[" startEllipsis:(_ "...")? _ hd:pattern tl:(_ "," __ v:pattern { return v })*
    endEllipsis:(_ "..." _)? close:(__ "]")? {
        if (!close) error("Expecting \"]\" to close the list")
        return tl.length > 0
            ? tree.leaf(tree.LIST, { values: [hd].concat(tl), startEllipsis: !!startEllipsis, endEllipsis: !!endEllipsis }, error)
            : hd
    }

setPattern
    = "set" _ "{" _ ellipsis:("..." _)? "}"
        { return tree.leaf(tree.SET, { values: [], ellipsis: !!ellipsis }, error) }

    / "set" _ "{" __ hd:pattern tl:(_ "," __ v:pattern { return v })* __ ellipsis:("..." _)? close:"}"? {
        if (!close) error("Expecting \"\x7d\" to close the set")
        return tl.length > 0
            ? tree.leaf(tree.SET, { values: [hd].concat(tl), ellipsis: !!ellipsis }, error)
            : hd
        }

dictPattern
    = "dict" _ "{" _ ellipsis:("..." _)? "}"
        { return tree.leaf(tree.DICT_VALUE, { elements: [], ellipsis: !!ellipsis }, error) }

    / "dict" _ "{" __ hd:dictKeyPattern tl:(_ "," __ m:dictKeyPattern { return m })
    __ ellipsis:("..." _)? close:"}"? {
        if (!close) error("Expecting \"\x7d\" to close the dictionary")
        return tree.leaf(tree.DICT_VALUE, { elements: [hd].concat(tl), ellipsis: !!ellipsis }, error)
    }

dictKeyPattern
    = key:valueExpr _ "->" __ value:pattern
        { return tree.leaf(tree.DICT_KEY_VALUE, { key, value }, error) }

capture
    = "@" id:id type:(_ t:tupleType { return t })?
        { return tree.leaf(tree.CAPTURE, {
            name: id,
            type: type ?? tree.leaf(tree.ANY_TYPE, { }, error)
        }, error) }
    / id:"_" { return tree.leaf(tree.CAPTURE, { name: id }, error) } // any value

/************* TYPE DEFINITIONS *************/

type = unionType

unionType
    = hd:functionType tl:(_ pipe __ t:functionType { return t })* {
        if (tl.length > 0) {
            const types = [hd].concat(tl)
            if (types.some(t => tree.isVoidType(t)))
                error("Void type cannot be in an union")
            return tree.leaf(tree.UNION_TYPE, { types }, error)
        }
        else
            return hd
    }

functionType
    = isAsync:isAsync purity:purity hd:voidTypeOrNot tl:(_ "->" __ t:voidTypeOrNot { return t })* {
        return tl.length > 0
            ? tree.leaf(tree.FUN_TYPE, {
                isAsync,
                purity,
                params: [hd].concat(tl)
            }, error)
            : hd }

voidTypeOrNot
    = voidType
    / concatType

concatType
    = hd:tupleType tl:(_ "++" __ tupleType)* {
        return tl.length > 0
            ? tree.leaf(tree.CONCAT_TYPE, { records: [hd].concat(tl) }, error)
            : hd
    }

tupleType
    = hd:tuplePowerType tl:(_ "*" __ t:tuplePowerType { return t })* {
        return tl.length > 0
            ? tree.leaf(tree.TUPLE_TYPE, { types: [hd].concat(tl) }, error)
            : hd }
tuplePowerType
    = base:optionType power:(_ powerOp __ i:integer { return i })? {
        return power
            ? tree.leaf(tree.TUPLE_POWER_TYPE, { base, power }, error)
            : base }
optionType
    = base:traitIntersection isOption:"?"? {
        return isOption
            ? tree.leaf(tree.OPTION_TYPE, { type: base }, error)
            : base }

traitIntersection
    = hd:specializeType tl:(__ "&" __ t:specializeType { return t })* {
        if (tl.length > 0)
            return tree.leaf(tree.TRAIT_INTER, { traits: [hd].concat(tl) }, error)
        else
            return hd
    }

specializeType
    = hd:(genericParamType / atomicType) tl:(_ "-" __ t:(genericParamType / atomicType))* {
        let types = [hd].concat(tl)
        if (types.length > 1)
            return tree.leaf(tree.SPECIALIZE_TYPE, { base: types.pop(), params: types }, error)
        else
            return hd
    }
genericParamType = "@" id:id { return tree.leaf(tree.GENERIC_PARAM_TYPE, { name: id }, error) }

atomicType
    = anyType
    / traitAlias
    / listType
    / linkedListType
    / setType
    / dictType
    / recType
    / voidType
    / typeByName
    / "(" __ t:type __ ")" { return t }
    / "#" { error("Unexpected \"#\" in type expression") }

listType = "[" __ elementType:type __ "]" { return tree.leaf(tree.LIST_TYPE, { type: elementType }, error) }
linkedListType = "::" _ elementType:type { return tree.leaf(tree.LINKED_LIST_TYPE, { type: elementType }, error) }

setType = "set" _ "{" __ elementType:type __ "}" { return tree.leaf(tree.SET_TYPE, { type: elementType }, error) }

dictType
    = "dict" _ "{" __ key:tupleType _ "->" __ value:tupleType __ "}"
        { return tree.leaf(tree.DICT_TYPE, { key, value }, error) }

recType
    = "{|" __
    hd:recMemberType tl:((_ "," __ / eol) m:recMemberType { return m })*
    close:(__ "|}")? {
        if (!close)
            error("Expecting \"|\x7d\" to close the record type")
        return tree.leaf(tree.REC_TYPE, {
            members: [hd].concat(tl)
        }, error)
    }
recMemberType
    = recInheritance
    / recFieldType
    / propDef
    / fun
    / constructor

recInheritance
    = "..." parent:specializeType { return tree.leaf(tree.INHERITANCE, { parent }, error) }

recFieldType
    = modifier:recFieldTypeModifier? names:(_ i:identifiers { return i }) type:(_ t:type { return t })?
    defaultValue:(_ ":" __ value:valueExpr { return value })? {
        if (!type && !defaultValue)
            error("The type or the value of the member must be specified")
        return tree.leaf(tree.REC_MEMBER_TYPE, { modifier, names, type, defaultValue }, error)
    }
    / modifier:"base" _ names:identifiers _ "=" __ defaultValue:valueExpr
        { return tree.leaf(tree.REC_MEMBER_TYPE, { modifier, names, defaultValue }, error) }
recFieldTypeModifier
    = "const" / "init" / "var"

propDef
    = p:propHeader getter:getterBody setter:setter? {
        p.getter = getter
        p.setter = setter
        return p
    }
propHeader
    = purity:(s:"state" _ { return s })? "prop" _ name:id _ type:type?
        { return tree.leaf(tree.PROP_DEF, {
            purity: purity ?? "pure",
            name,
            type
        }, error) }
setter
    = __ "set" _ body:lambda { return body }
getterBody
    = __ "=>" __ v:branch { return v }
    / __ "{" __ s:funStatements __ "}"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s }, error) }

lambda
    = isAsync:isAsync purity:purity kind:(m:"sub" _ { return m })? "=>" __ value:optionNoPipe
        { return tree.leaf(tree.FUN_DEF, {
            isAsync,
            purity,
            kind: kind ?? "fun",
            name: null,
            params: tree.getLambdaVariables(value),
            body: value,
            returnType: null
        }, error) }
    / isAsync:isAsync purity:purity kind:("sub" / colon) _ traitConstraints:traitConstraints _
    params:params __ returnType:(colon _ t:type __ { return t })? body:lambdaBody
        { return tree.leaf(tree.FUN_DEF, {
             isAsync,
             purity,
             kind: kind === ":" ? "fun" : kind,
             name: null,
             traitConstraints,
             params: params,
             body: body,
             returnType: returnType
         }, error) }
     / isAsync:isAsync purity:purity "enum" __ returnType:(colon _ t:type __ { return t })? body:lambdaBody
        { return tree.leaf(tree.INLINE_ENUM, {
            isAsync,
            purity,
            body: body,
            returnType: returnType ?? "any"
        }, error) }

lambdaBody
    = "=>" __ v:optionNoPipe { return v }
    / "{" __ s:funStatements __ "}"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s }, error) }
    / case

constructor
    = isAsync:isAsync "new" returnType:(_ pipe _ t:type { return t })? body:constructorBody
        { return tree.leaf(tree.FUN_DEF, {
            isAsync,
            kind: "fun",
            name: "new",
            params: [],
            body,
            returnType
        }, error) }
constructorBody
    = __ s:funStatements __ when:when? __ "end"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s, when }, error) }

voidType = void { return tree.leaf(tree.VOID_TYPE, {}, error) }

anyType = "any" { return tree.leaf(tree.ANY_TYPE, {}, error) }

/************* EXPRESSIONS *************/

pipedExpr
    = option:valueExpr pipeCalls:(__ "\\" __ c:appendCall { return c })* {
        if (pipeCalls.length > 0)
        {
            var result = pipeCalls.pop()
            var current = result
            while (pipeCalls.length > 0)
            {
                var param = pipeCalls.pop()
                current.params.unshift(param)
                current = param
            }
            current.params.unshift(option)
            return result
        }
        else
            return option
    }

valueExpr = assignment

assignment
    = variable:withEffect assign:assignValue? {
        if (assign)
        {
            assign.variable = variable
            return assign
        }
        else
            return variable
    }

assignValue
    = _ op:assignmentOp __ value:branch
        { return tree.leaf(tree.ASSIGN, { operator: op, value }, error) }

withEffect
    = value:default effects:effects? {
        if (effects)
            return tree.leaf(tree.WITH_EFFECT, { value, effects }, error)
        else
            return value
    }

effects // []
    = _ "with" _ hd:default tl:(_ "," __ d:default { return d })*
        { return [hd].concat(tl) }

default = hd:disjunction tl:(__ "??" __ d:default { return d })* {
        return tl.length > 0
            ? tree.leaf(tree.DEFAULT_VALUE, { values: [hd].concat(tl) }, error)
            : hd
    }

disjunction
    = hd:xor tl:(__ "or" __ o:xor { return o })* {
        return tl.length > 0
            ? tree.leaf(tree.DISJUNCTION, { operands: [hd].concat(tl) }, error)
            : hd
        }
xor
    = hd:conjunction tl:(__ "xor" __ o:conjunction { return o })* {
        return tl.length > 0
            ? tree.leaf(tree.XOR, { operands: [hd].concat(tl) }, error)
            : hd
        }
conjunction
    = hd:comparison tl:(__ "&" !"=" __ o:comparison { return o })* {
        return tl.length > 0
            ? tree.leaf(tree.CONJUNCTION, { operands: [hd].concat(tl) }, error)
            : hd
        }

comparison
    = firstOperand:aboveComparison otherOperands:otherComparisonOperand* {
        return otherOperands.length > 0
            ? tree.leaf(tree.COMPARISON, {
                operands: [tree.leaf(tree.COMPARISON_OPERAND, { operator: null, value: firstOperand }, error)].concat(otherOperands)
            }, error)
            : firstOperand
        }
otherComparisonOperand
    = __ operator:comparisonOp __ operand:aboveComparison
        { return tree.leaf(tree.COMPARISON_OPERAND, { operator, value: operand }, error) }

aboveComparison = setComparison

setComparison
    = firstOperand:aboveSetComparison otherOperands:otherSetComparisonOperand* {
        return otherOperands.length > 0
            ? tree.leaf(tree.SET_COMPARISON, {
                operands: [tree.leaf(tree.SET_COMPARISON_OPERAND, { operator: null, value: firstOperand }, error)].concat(otherOperands)
            }, error)
            : firstOperand
        }
otherSetComparisonOperand
    = _ operator:setComparisonOp __ operand:aboveSetComparison
        { return tree.leaf(tree.SET_COMPARISON_OPERAND, { operator, value: operand }, error) }

aboveSetComparison = linkedList

linkedList
    = head:modifyRec tail:(__ "::" __ tl:linkedList { return tl })? {
        if (tail)
            return tree.leaf(tree.LINKED_LIST, { head, tail }, error)
        else
            return head
    }

modifyRec
    = set:concat changes:(__ "<|" _ hd:modifyMember tl:(__ "<|" c:modifyMember { return c })* { return [hd].concat(tl) })* {
        if (changes.length > 0)
            return tree.leaf(tree.MODIFY_REC, { set, changes }, error)
        else
            return set
    }

modifyMember
    = name:id _ colon __ value:valueExpr
        { return tree.leaf(tree.REC_MEMBER_VALUE, { name, value }, error) }
    / "!" _ name:id
        { return tree.leaf(tree.REC_MEMBER_VALUE, { name, value: null }, error) } // remove

concat
    = hd:addition tl:(__ concatOp __ other:addition { return other })* {
        return tl.length > 0
            ? tree.leaf(tree.CONCAT, { operands: [hd].concat(tl) }, error)
            : hd
        }

addition
    = hd:firstTerm tl:otherTerm* {
        return tl.length > 0 || hd.sign !== "+"
            ? tree.leaf(tree.ADDITION, { terms: [hd].concat(tl) }, error)
            : hd.value
        }
firstTerm
    = sign:additiveOp? _ term:term
        { return tree.leaf(tree.TERM, { sign:sign ?? "+", value:term }, error) }

otherTerm
    = _ sign:additiveOp __ term:term
        { return tree.leaf(tree.TERM, { sign, value:term }, error) }

term = call

call
    = c:nestedCall {
        if (tree.isNodeOf(tree.MUTABLE_PARAM))
            error("Mutable keyword can only used to pass parameters")
        return c
    }

nestedCall
    = left:callParam appendCall:appendCall? {
        if (appendCall)
        {
            appendCall.params.unshift(left)
            return appendCall
        }
        else
            return left
    }

appendCall
    = _ fun:multiplication defer:(_ "defer")? params:rightParams?
        { return tree.leaf(tree.CALL, { fun, defer: !!defer, params: params ?? [] }, error) }

rightParams
    = _ hd:call tl:(_ ";" __ c:nestedCall { return c })*
        { return [hd].concat(tl) }

callParam
    = mut:"mut"? _ value:setDiff {
        if (mut)
            return tree.leaf(tree.MUTABLE_PARAM, { value }, error)
        else
            return value
    }

setDiff
    = hd:setUnion tl:(__ "{-}" __ t:setUnion { return t })* {
        if (tl.length > 0)
            return tree.leaf(tree.SET_DIFF, { sets: [hd].concat(tl) }, error)
        else
            return hd
    }

setUnion
    = hd:setInter tl:(__ "{+}" __ t:setInter { return t })* {
          if (tl.length > 0)
              return tree.leaf(tree.SET_UNION, { sets: [hd].concat(tl) }, error)
          else
              return hd
      }

setInter
    = hd:cartesianProd tl:(__ "{&}" __ t:cartesianProd { return t })* {
          if (tl.length > 0)
              return tree.leaf(tree.SET_INTER, { sets: [hd].concat(tl) }, error)
          else
              return hd
      }

cartesianProd
    = hd:range tl:(__ "{*}" __ r:range { return r })* {
        if (tl.length > 0)
            return tree.leaf(tree.CARTESIAN_PROD, { sets: [hd].concat(tl) }, error)
        else
            return hd
    }

cartesianPower
    = base:range power:(_ "{^}" __ r:range { return r })* {
        if (tl.length > 0)
            return tree.leaf(tree.CARTESIAN_POWER, { base, power }, error)
        else
            return hd
    }

range
    = first:multiplication to:rangeSuffix? {
        if (to)
        {
            to.first = first
            return to
        }
        else
            return first
    }
rangeSuffix
    = _ op:rangeOp __ lastOrCount:multiplication step:(_ "by" __ s:multiplication { return s })?
        { return tree.leaf(tree.RANGE, { lastOrCount, op, step }, error) }

multiplication
    = hd:factor tl:otherFactor* {
        return tl.length > 0
            ? tree.leaf(tree.MULTIPLICATION, {
                factors: [tree.leaf(tree.FACTOR, { operator:"*", value:hd }, error)].concat(tl)
            }, error)
            : hd
        }
otherFactor
    = __ operator:multiplicativeOp __ factor:factor
        { return tree.leaf(tree.FACTOR, { operator, value:factor }, error) }
factor
    = not
    / exponentiation;

not = notOp f:factor
    { return tree.leaf(tree.NOT, { value:f }, error) }

exponentiation
    = base:indexing power:(_ powerOp __ p:indexing { return p })? {
        return power
            ? tree.leaf(tree.EXPONENTIATION, { base, power }, error)
            : base
        }

indexing
    = list:compose index:(_ "[" __ i:branch __ "]" { return i })? {
        return index
            ? tree.leaf(tree.INDEXING, { list, index }, error)
            : list
        }

compose
    = hd:taggedAtom tl:(_ composeOp __ taggedAtom)* {
        if (tl.length > 0)
            return tree.leaf(tree.COMPOSE, { functions: [hd].concat(tl) }, error)
        else
            return hd
    }

/************* ATOMS *************/

taggedAtom
    = tag:tag value:(__ v:taggedAtom { return v })?
        { return tree.leaf(tree.TAGGED_VALUE, { tag, value }, error) }
    / atom

atom
    = operatorFun
    / tuple
    / recValue
    / list
    / set
    / dictValue
    / float
    / string
    / newObject
    / getMember
    / lambda
    / voidValue
    / boolean

operatorFun
    = "(" op:overridableOp ")"
        { return tree.leaf(tree.VALUE_BY_NAME, { op }, error) }

voidValue
    = void { return tree.leaf(tree.VOID_VALUE, {}, error) }
    / "<>" { return tree.leaf(tree.EMPTY_LINKED_LIST, {}, error) }

implicitParam
    = "$" [a-z]
        { return tree.leaf(tree.IMPLICIT_PARAM, { name: text() }, error) }
    / "$" { error("Expecting letter for implicit parameter after \"$\"") }

tuple
    = "(" __ hd:branch tl:(_ "," __ v:branch { return v })* close:")"? {
        if (!close) {
            if (tl.length > 0)
                error("Expecting \")\" to close the tuple")
            else
                error("Expecting matching \")\"")
        }
        return tl.length > 0
            ? tree.leaf(tree.TUPLE, { values: [hd].concat(tl) }, error)
            : hd
        }

recValue
    = "{|" __ members:recMembers? close:(__  "|}") {
        if (!close) error("Expecting \"|\x7d\" to close the record")
        return tree.leaf(tree.REC_VALUE, { members: members ?? [] }, error)
    }
recMembers
    = hd:recMemberValue tl:(_ ("," _ / eol) m:recMemberValue { return m })*
recMemberValue
    = recMemberFieldValue
    / propDef
    / fun
    / splat
recMemberFieldValue
    = modifier:(m:recMemberFieldValueModifier _)? name:id _ type:type? _ colon __ value:branch
        { return tree.leaf(tree.REC_MEMBER_VALUE, { modifier, name, type, value }, error) }
recMemberFieldValueModifier
    = "const" / "var"

splat
    = "..." value:valueByName
        { return tree.leaf(tree.SPLAT, { value }, error) }

list = "[" _ elements:listElements? close:(_ "]")? {
        if (!close) error("Expecting \"]\" to close the list")
        return tree.leaf(tree.LIST, { values: elements ?? [] }, error)
    }
listElements
    = __ hd:(splat / branch) tl:(_ "," __ v:(splat / branch) { return v })* __
        { return [hd].concat(tl) }

set = "set" _ "{" _ elements:setElements? close:(_ "}")? {
        if (!close) error("Expecting \"\x7d\" to close the set")
        return tree.leaf(tree.SET, { values: elements ?? [] }, error)
    }
setElements
    = __ hd:(splat / branch) tl:(_ "," __ v:(splat / branch) { return v })* __
        { return [hd].concat(tl) }

dictValue
    = "dict" _ "{" _ elements:dictElements? _ close:"}"? {
        if (!close) error("Expecting \"\x7d\" to close the dictionary")
        return tree.leaf(tree.DICT_VALUE, { elements: elements ?? [] }, error)
    }
dictElements
    = __ hd:dictKeyValue tl:(_ "," __ m:dictKeyValue { return m })* __
        { return [hd].concat(tl) }
dictKeyValue
    = key:valueExpr _ "->" __ value:branch
        { return tree.leaf(tree.DICT_KEY_VALUE, { key, value }, error) }
    / splat

newObject
    = "new" _ type:specializeType __ params:tuple
        { return tree.leaf(tree.CREATE_OBJECT, { type, params }, error) }

getMember
    = container:(valueByName / implicitParam)
    path:("." id:id { return id })* {
        if (path.length > 0)
            return tree.leaf(tree.GET_MEMBER, { container, path }, error)
        else
            return container
    }
    / path:("." id:id { return id })+ {
        return tree.leaf(tree.GET_WISE_MEMBER, { path }, error)
    }

valueByName
    = namespace:(i:id "--" { return i })* name:id
        { return tree.leaf(tree.VALUE_BY_NAME, { name, namespace }, error) }
    / "me"

typeByName
    = namespace:(i:id "--" { return i })* name:typeId
        { return tree.leaf(tree.TYPE_BY_NAME, { name, namespace }, error) }

typeId
    = id
    / "enum"
    / traitAlias

traitAlias
    = "(" o:overridableOp ")" { return o }

boolean
    = value:("yes" / "no")
        { return tree.leaf(tree.BOOLEAN, { value: value === "yes" }, error) }

tag
    = "#" specializeType

/************* TOKENS *************/

identifiers
    = hd:id tl:(_ "/" __ id:id { return id })*
        { return [hd].concat(tl) }

id = $(!keyword "_"? [A-Za-z] [A-Za-z0-9_]*)

integer = [0-9] [0-9_]* ([eE] "+"? [0-9]+)?
    { return tree.leaf(tree.INTEGER, { value: BigInt(text()) }, error) }

float = beforePoint:$([0-9] [0-9_]*) afterPoint:$("." [0-9] [0-9_]*)? expPart:$([eE] [+-]? [0-9]+)? {
        if (afterPoint || expPart && expPart.includes("-") )
            return tree.leaf(tree.FLOAT, { value: parseFloat(text()) }, error)
        else
        {
            if (expPart)
            {
                const expValue = expPart.substring(1)
                const factor = BigInt(10) ** BigInt(expValue)
                return tree.leaf(tree.INTEGER, { value: BigInt(beforePoint) * factor }, error)
            }
            else
                return tree.leaf(tree.INTEGER, { value: BigInt(text()) }, error)
        }
    }

void = "()"

_ = [ \t]*
__ = ([ \t\r\n] / comment)*
comment
    = "//" [^\r\n]*
    / "/*" (!"*/" .)* "*/"
eol = ([ \t] / comment)* "\r"? "\n" __

keyword = ("let" / "var" / "fun" / "sub" / "mut" / "do" / "end" / "return" / "yield" / "state"
    / "new" / "const" / "init" / "base" / "prop" / "me" / "with"
    / "type" / "any" / "enum" / "set" / "dict" / "yes" / "no" / "trait" / "alias"
    / "wise" / "else" / "while" / "iter" / "next" / "break" / "case" / "other" / "when" / "resume"
    / "in" / "by" / "or" / "xor" / "global" / "async" / "defer") ![A-Za-z0-9_]

assignmentOp = ":=" / "*=" / "/=" / "%=" / "+=" / "-=" / "++="
colon = $(":" ![=:])

pipe = $("|" !"}")
logicOp = "or" / "xor" / $("&" !"=")
comparisonOp = $("=" !">") / "!=" / rightAngleBracket / ">=" / leftAngleBracket / "<=" / "in"
leftAngleBracket = $("<" ![>=<|])
rightAngleBracket = $(">" ![>=<])
concatOp = $("++" !"=")
additiveOp = $(("+" / "-") !([=>] / ".."))
multiplicativeOp = $(("*" / "/" / "%") !"=")
notOp = $("!" !"=")
powerOp = $("^" !"=")
composeOp = $(">>" !"=")
setComparisonOp = "{=}" / "{!=}" / "{<}" / "{<=}" / "{>}" / "{>=}"
setOp = "{-}" / "{+}" / "{&}" / "{*}" / "{^}"
tildeOp = "~" // unused
rangeOp = "..<" / "..=" / "..>=" / "..>" / "+.." / "-.."
overridableOp
    = logicOp
    / comparisonOp
    / concatOp
    / additiveOp
    / multiplicativeOp
    / notOp
    / powerOp
    / composeOp
    / setComparisonOp
    / setOp
    / rangeOp

string = "\"" parts:(literalPart / formattedValue)*  "\""
    { return tree.leaf(tree.STRING_VALUE, { parts }, error) }

literalPart = chars:literalChar+
    { return tree.leaf(tree.STRING_PART, { value: chars.join("") }, error) }
formattedValue
    = "{" _ value:branch _ "}"
    { return tree.leaf(tree.FORMATTED_VALUE, { value }, error) }
literalChar
    = [^"{}]
    / "{{" { return "\x7b" }
    / "}}" { return "\x7d" }
    / "\"\"" { return "\"" }
