// grammar using a custom PEG format
// it needs to be preprocessed by grammar-preprocessor

/************* MODULE STRUCTURE *************/

module
    = __ s:moduleStatements? __
        { return tree.leaf(tree.MODULE, { statements: s ?? [] }, error) }

moduleStatements // []
    = hd:moduleStatement tl:(statementSeparator s:moduleStatement { return s })*
        { return [hd].concat(tl).flat() }

moduleStatement // [] or leaf
    = defs
    / vars
    / typeDefs
    / explicitBlock
    / fun
    / branch

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

typeDef
    = _ name:id _ "=" _ type:type
        { return tree.leaf(tree.TYPE_DEF, { name, type }, error) }

explicitBlock
    = "do" __ s:moduleStatements? __ when:when? __ "end"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s ?? [], when }, error) }

when
    = "when" (eol _ "|"? _ / __) c:caseBody { return c }

/************* FUNCTION DEFINITION *************/

fun
    = global:("global" _)? async:("async" _)? kind:funKind _ hd:funParam __ "\"" name:(id / overridableOp) "\"" _
    tl:(__ p:params { return p })? _ returnType:(colon __ t:type { return t })? _ body:funBody
        { return tree.leaf(tree.FUN_DEF, {
            kind, async: !!async, global: !!global,
            name,
            params: [hd].concat(tl ?? []),
            body,
            returnType
        }, error) }

funParam
    = mut:("mut" _)? names:identifiers _ type:type?
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
    = "fun" / "sub" / "seq"

funBody
    = __ "=>" __ v:branch { return v }
    / __ explicitFunBlock
    / case

explicitFunBlock
    = "do" __ s:statements? __ when:when? __ "end"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s ?? [], when }, error) }

statements // []
    = hd:statement tl:(statementSeparator s:statement { return s })*
        { return [hd].concat(tl).flat() }

statement // [] or leaf
    = moduleStatement
    / return

return
    = "return" _ value:branch
        { return tree.leaf(tree.RETURN, { value }, error) }
    / "yield" _ value:branch
            { return tree.leaf(tree.YIELD, { value }, error) }
    / "resume" { return tree.leaf(tree.RESUME, { }, error) }

/************* BRANCHING *************/

branch
    = optionWithPipe
    / loop

loop
    = "while" _ condition:valueExpr __ body:(branch / inlineBlock / return)
        { return tree.leaf(tree.WHILE, { condition, body }, error) }

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
    = _ "wise" __ "{" __ statements:statements __ close:"}"? {
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
    = _ ("case" __ "|"? _ / eol _ "|" _) c:caseBody { return c }

inlineBlock
    = "{" !(([-+&*^] / [><] "="? / "!=") "}") __ statements:statements? __ "}"
        { return tree.leaf(tree.CODE_BLOCK, { statements }, error) }

/************* PATTERN MATCHING *************/

caseBody
    = hd:caseOption tl:(__ "|" _ o:caseOption { return o })*
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
    = operator:(">" / ">=" / $("<" ![>|]) / "<=" / "in") _ value:aboveComparison

tuplePattern
    = "(" startEllipsis:(_ "...")? __ hd:pattern tl:(_ "," __ v:pattern { return v })* __
    endEllipsis:("..." _)? close:")"? {
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
    / "{|" __ hd:recMemberPattern tl:(_ "," __ m:recMemberPattern { return m })* __
    ellipsis:("..." _)? close:"|}"? {
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
    __ endEllipsis:("..." _)? close:"]"? {
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
    = hd:taggedType tl:(_ "|" __ t:taggedType { return t })* {
        return tl.length > 0
            ? tree.leaf(tree.UNION_TYPE, { params: [hd].concat(tl) }, error)
            : hd }

taggedType
    = tag:tag type:(_ t:taggedType { return t })?
        { return tree.leaf(tree.TAGGED_TYPE, {
            tag,
            type: type ?? tree.leaf(tree.VOID_TYPE, { }, error)
        }, error) }
    / functionType

functionType
    = hd:inheritance tl:(_ "->" __ t:inheritance { return t })* {
        return tl.length > 0
            ? tree.leaf(tree.FUN_TYPE, { params: [hd].concat(tl) }, error)
            : hd }

inheritance
    = hd:tupleType tl:(_ "++" __ tupleType)* {
        return tl.length > 0
            ? tree.leaf(tree.INHERITANCE, { records: [hd].concat(tl) }, error)
            : hd
    }

tupleType
    = hd:tuplePowerType tl:(_ "*" __ t:tuplePowerType { return t })* {
        return tl.length > 0
            ? tree.leaf(tree.TUPLE_TYPE, { types: [hd].concat(tl) }, error)
            : hd }
tuplePowerType
    = base:voidableType power:(_ powerOp __ i:integer { return i })? {
        return power
            ? tree.leaf(tree.TUPLE_POWER_TYPE, { base, power }, error)
            : base }
voidableType
    = nonVoidable:nonVoidableType isVoidable:"?"? {
        return isVoidable
            ? tree.leaf(tree.VOIDABLE_TYPE, { type: nonVoidable }, error)
            : nonVoidable }
nonVoidableType
    = namedType
    / anyType
    / genericParamType
    / listType
    / linkedListType
    / seqType
    / setType
    / dictType
    / recType
    / voidType
    / "(" __ t:type __ ")" { return t }

listType = "[" __ elementType:type __ "]" { return tree.leaf(tree.LIST_TYPE, { type: elementType }, error) }
linkedListType = "::" _ elementType:type { return tree.leaf(tree.LINKED_LIST_TYPE, { type: elementType }, error) }

seqType
    = "seq" _ hd:inheritance tl:(_ "->" __ t:inheritance { return t })* {
        return tl.length > 0
            ? tree.leaf(tree.SEQ_TYPE, { params: [hd].concat(tl) }, error)
            : hd }
    / "seq" _ "->" __ returnType:inheritance
        { return tree.leaf(tree.SEQ_TYPE, { params: [returnType] }, error) }

setType = "set" _ "{" __ elementType:type __ "}" { return tree.leaf(tree.SET_TYPE, { type: elementType }, error) }

dictType
    = "dict" _ "{" __ key:tupleType _ "->" __ value:tupleType __ "}"
        { return tree.leaf(tree.DICT_TYPE, { key, value }, error) }
recType
    = "{|" __ hd:recMemberType tl:((_ "," / eol) __ m:recMemberType { return m })* __ "|}"
        { return tree.leaf(tree.REC_TYPE, { members: [hd].concat(tl) }, error) }
recMemberType
    = regularMemberType
    / propDef
    / constructor

regularMemberType
    = modifier:recMemberTypeModifier? _ names:identifiers _ type:type?
    defaultValue:(_ "=" __ value:valueExpr { return value })?
        { return tree.leaf(tree.REC_MEMBER_TYPE, { modifier, names, type, defaultValue }, error) }
recMemberTypeModifier
    = "const" / "base" / "init" / "var"

propDef
    = "prop" _ name:id _ type:type? __ getter:getterBody setter:setter?
        { return tree.leaf(tree.PROP_DEF, {
            name,
            type,
            getter,
            setter
        }, error) }
setter
    = __ "set" _ body:lambda { return body }
getterBody
    = "=>" __ v:branch { return v }
    / "{" __ s:statements? __ "}"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s ?? [] }, error) }

lambda
    = async:("async" _)? kind:(m:("seq" / "sub") _ { return m })? "=>" __ value:optionNoPipe
        { return tree.leaf(tree.FUN_DEF, {
            kind: kind ?? "fun", async: !!async,
            name: null,
            params: tree.getLambdaVariables(value),
            body: value,
            returnType: null
        }, error) }
    / async:("async" _)? kind:(m:("seq" / "sub") _ { return m })? colon _ params:params __ returnType:(colon _ t:type { return t })? __ body:lambdaBody
        { return tree.leaf(tree.FUN_DEF, {
             kind: kind ?? "fun", async: !!async,
             name: null,
             params: params,
             body: body,
             returnType: returnType
         }, error) }
     / async:("async" _)? "seq" __ body:lambdaBody
        { return tree.leaf(tree.FUN_DEF, {
            kind: "seq", async: !!async,
            name: null,
            params: [],
            body: body,
            returnType: null
        }, error) }

lambdaBody
    = "=>" __ v:optionNoPipe { return v }
    / "{" __ s:statements? __ "}"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s ?? [] }, error) }
    / case

constructor
    = async:("async" _)? "new" returnType:(_ "|" _ t:type { return t })? body:constructorBody
        { return tree.leaf(tree.FUN_DEF, {
            kind: "sub", name: "new",
            async: !!async,
            params: [],
            body,
            returnType
        }, error) }
constructorBody
    = __ s:statements? __ when:when? __ "end"
        { return tree.leaf(tree.CODE_BLOCK, { statements: s ?? [], when }, error) }

voidType = void { return tree.leaf(tree.VOID_TYPE, {}, error) }

namedType = valueByName
genericParamType = "@" id:id { return tree.leaf(tree.GENERIC_PARAM_TYPE, { name: id }, error) }
anyType = "any" { return tree.leaf(tree.ANY_TYPE, {}, error) }

/************* EXPRESSIONS *************/

pipedExpr
    = option:valueExpr pipeCall:(_ "\\" __ c:appendCall { return c })? {
        if (pipeCall)
        {
            pipeCall.params.unshift(option)
            return pipeCall
        }
        else
            return option
    }

valueExpr = assignment

assignment
    = variable:default assign:assignValue? {
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
    = _ fun:(multiplication / "new") defer:(_ "defer")? params:rightParams?
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
    = first:multiplication to:(_ op:(".." [=<] / "+..") __ lastOrCount:multiplication)? {
        if (to)
            return tree.leaf(tree.RANGE, { first, lastOrCount: to.last, op: to.op }, error)
        else
            return first
    }

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
    = "{|" _ members:recMembers? _ close:"|}"? {
        if (!close) error("Expecting \"|\x7d\" to close the record")
        return tree.leaf(tree.REC_VALUE, { members: members ?? [] }, error)
    }
recMembers
    = __ hd:recMemberValue tl:(_ "," __ m:recMemberValue { return m })* __
recMemberValue
    = modifier:(m:recMemberValueModifier _)? name:id _ colon __ value:branch
        { return tree.leaf(tree.REC_MEMBER_VALUE, { modifier, name, value }, error) }
    / splat
recMemberValueModifier
    = "const" / "var"

splat
    = "..." value:valueByName
        { return tree.leaf(tree.SPLAT, { value }, error) }

list = "[" _ elements:listElements? _ close:"]"? {
        if (!close) error("Expecting \"]\" to close the list")
        return tree.leaf(tree.LIST, { values: elements ?? [] }, error)
    }
listElements
    = __ hd:(splat / branch) tl:(_ "," __ v:(splat / branch) { return v })* __
        { return [hd].concat(tl) }

set = "set" _ "{" _ elements:setElements? _ close:"}"? {
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

getMember
    = container:(valueByName / implicitParam)
    path:("." id:(id / "new") { return id })* {
        if (path.length > 0)
            return tree.leaf(tree.GET_MEMBER, { container, path }, error)
        else
            return container
    }
    / path:("." id:id { return id })+ {
        return tree.leaf(tree.GET_WISE_MEMBER, { path }, error)
    }

valueByName
    = hd:id tl:("--" id:id { return id })* {
        let ids = [hd].concat(tl)
        const name = ids.pop()
        return tree.leaf(tree.VALUE_BY_NAME, { name, namespace: ids }, error)
    }

boolean
    = value:("yes" / "no")
        { return tree.leaf(tree.BOOLEAN, { value: value === "yes" }, error) }

/************* TOKENS *************/

identifiers
    = hd:id tl:(_ "/" __ id:id { return id })*
        { return [hd].concat(tl) }

id = $(!keyword "_"? [A-Za-z] [A-Za-z0-9_]*)

tag
    = $("#" "_"? [A-Za-z] [A-Za-z0-9_]*)

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

keyword = ("let" / "var" / "fun" / "sub" / "mut" / "do" / "end" / "return" / "yield"
    / "new" / "const" / "init" / "base" / "prop"
    / "type" / "any" / "seq" / "set" / "dict" / "yes" / "no"
    / "wise" / "else" / "while" / "case" / "other" / "when" / "resume"
    / "in" / "or" / "xor" / "global" / "async" / "defer") ![A-Za-z0-9_]

assignmentOp = ":=" / "*=" / "/=" / "%=" / "+=" / "-=" / "++="
colon = $(":" ![=:])

logicOp = "or" / "xor" / $("&" !"=")
comparisonOp = $("=" !">") / "!=" / $(">" ![>=<]) / ">=" / $("<" ![>|]) / "<=" / "in";
concatOp = $("++" !"=")
additiveOp = $(("+" / "-") !([=>] / ".."))
multiplicativeOp = $(("*" / "/" / "%") !"=")
notOp = $("!" !"=")
powerOp = $("^" !"=")
composeOp = $(">>" !"=")
setComparisonOp = "{=}" / "{!=}" / "{<}" / "{<=}" / "{>}" / "{>=}"
setOp = "{-}" / "{+}" / "{&}" / "{*}" / "{^}"
tildeOp = "~" // unused
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
