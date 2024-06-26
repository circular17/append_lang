// grammar using a custom PEG format
// it needs to be preprocessed by grammar-preprocessor

/************* MODULE STRUCTURE *************/

module
    = __ s:moduleStatements __
        => #MODULE { statements: s }

moduleStatements // []
    = hd:moduleStatement tl:(statementSeparator s:moduleStatement => s)*
        { return hd::tl.flat() }
    / "" => []

statement
    = constDefs
    / propDef
    / js
    / vars
    / typeDefs
    / fun
    / branch

moduleStatement // [] or leaf
    = use
    / statement
    / explicitModuleBlock

statementSeparator
    = _ d:"." " " _ => d
    / eol

use = "use" _ hd:id tl:(_ "," __ id:id => id)*
        => #USE { modules: hd::tl }

js = "`" code:$[^`]+ "`"
        => #JS { code }

constDefs // []
    = "let" _ hd:constDef tl:(_ ";" __ d:constDef => d)*
        => hd::tl

constDef
    = _ names:(names / deconstruct) _ type:type? _ "=" _ v:branch
        => #CONST_DEF { names, type, value: v }

vars // []
    = "var" _ hd:var tl:(_ ";" __ v:var => v)*
        => hd::tl

var
    = names:(names / deconstruct) _ "=" _ v:branch
        => #VAR_DEF { names, type: null, value: v }
    / names:(names / deconstruct) _ type:type _ v:("=" _ v:branch => v)?
        => #VAR_DEF { names, type, value: v }

names = i:identifiers => #NAMES { identifiers: i }

deconstruct
    = "(" __ hd:deconstructElement tl:(_ "," __ e:deconstructElement => e)* __ ")"
        => #DECONSTRUCT_TUPLE { elements: hd::tl }
    / "{|" __ hd:deconstructMember tl:(_ "," __ e:deconstructMember => e)* __ "|}"
        => #DECONSTRUCT_RECORD { elements: hd::tl }

deconstructMember
    = memberName:id decontructValue:(_ colon __ e:(deconstructElement / deconstruct) => e)?
        => #DECONSTRUCT_MEMBER { memberName, decontructValue }

deconstructElement
    = id:id => #DECONSTRUCT_NAME { name: id }
    / deconstruct

typeDefs // []
    = "type" _ t:typeDef {
        return [t]
    }
    / "trait" _ t:traitDef {
        return [t]
    }
    / "alias" _ hd:aliasDef tl:(_ ";" __ t:aliasDef => t)*
        => hd::tl

traitDef
    = genericParams:traitConstraints _ name:id alias:(_ "(" o:overridableOp ")" => o)?
    __ "{" __ features:features __ "}"
        => #TRAIT_DEF {
            name,
            genericParams,
            alias,
            features
        }

traitConstraints
    = leftAngleBracket _ hd:traitConstraint tl:(_ "," __ c:traitConstraint => c)* _ rightAngleBracket
        => hd::tl
    / "" => []
traitConstraint
    = type:id constraint:(_ t:type => t)?
        => #TRAIT_CONSTRAINT { type, constraint }

features
    = hd:feature tl:((_ "," __ / eol) f:feature => f)*
        => hd::tl

feature
    = f:funHeader {
        f.body = #ABSTRACT_BODY {}
        return f
    }
    / p:propHeader {
        p.getter = #ABSTRACT_BODY {}
        return p
    }
    / traitInheritance

traitInheritance
    = "..." parent:typeByName => #INHERITANCE { parent }

typeDef
    = genericParams:traitConstraints _ name:typeId _ type:type
        => #TYPE_DEF {
            name,
            genericParams,
            type
        }

aliasDef
    = genericParams:genericParams _ name:typeId _ "=" _ type:type
        => #ALIAS_DEF {
            name,
            genericParams,
            type
        }

genericParams
    = (i:typeId _ "-" => i)*

explicitModuleBlock
    = "do" effects:effects? __ s:moduleStatements __ when:when? __ "end"
        => #CODE_BLOCK { statements: s, effects: effects ?? [], when }

when
    = "when" (eol pipe _ / _) c:caseBody => c

/************* FUNCTION DEFINITION *************/

fun
    = f:funHeader _ body:funBody {
        f.body = body
        tree.fixFunction(f, error)
        return f
    }
    / visibility:visibility kind:funKind __ name:funName __ "=" __ expr:valueExpr
    => #FUN_DEF {
        visibility,
        kind,
        name,
        genericParams: [],
        expr,
        returnType: "kind" === "sub" ? #VOID_TYPE {}: #ANY_TYPE {}
    }
    / visibility:visibility kind:funKind __ name:funName __ "=>" __ body:valueExpr
    => #FUN_DEF {
        visibility,
        kind,
        name,
        genericParams: [],
        body,
        returnType: "kind" === "sub" ? #VOID_TYPE {}: #ANY_TYPE {}
    }

funHeader
    = visibility:visibility isAsync:isAsync purity:purity
    kind:funKind _ genericParams:traitConstraints _ hd:funParam __ "\"" name:funName "\""
    tl:(__ p:params => p)? returnType:(__ "->" __ t:type => t)?
    effects:(__ "with" _ e:identifiers => e)?
        => #FUN_DEF {
            visibility,
            isAsync,
            purity,
            kind,
            name,
            genericParams,
            params: [hd].concat(tl ?? []),
            effects: effects ?? [],
            returnType: returnType ?? (kind === "sub" ? #VOID_TYPE {}: #ANY_TYPE {})
        }

funName = keywordName:$(keyword?) name:(id / overridableOp)? {
        if (keywordName)
            error(`The keyword '${keywordName}' cannot be used as an identifier`)
        if (!name)
            error("The name of the function is not specified")
        return name
    }

visibility
    = "export" _ => "export"
    / "" => "normal"

isAsync
    = "async" _ => true
    / "" => false

funParam
    = mut:("mut" _)? names:identifiers type:(_ t:type => t)?
        => #FUN_PARAM_DEF {
            names,
            type: type ?? #ANY_TYPE {},
            mutable: !!mut
        }
    / voidType
        => #FUN_PARAM_DEF {
            names:[#VOID_VALUE {}],
            type: #VOID_TYPE {},
            mutable: false
        }

params // []
    = hd:funParam tl:(_ ";" __ p:funParam => p)*
        => hd::tl

funKind
    = "fun" / "sub" / "enum"

purity
    = p:"state" _ => p
    / "" => "pure"

funBody
    = __ "=>" __ v:branch => v
    / __ "=>" _ "?" => #ABSTRACT_BODY { }
    / __ b:explicitFunBlock => b
    / case

explicitFunBlock
    = "do" effects:effects? __ s:funStatements __ when:when? __ "end"
        => #CODE_BLOCK { statements: s, effects: effects ?? [], when }

funStatements // []
    = hd:funStatement tl:(statementSeparator s:funStatement => s)*
        { return hd::tl.flat() }
    / "" => []

funStatement // [] or leaf
    = statement
    / explicitFunBlock
    / return

return
    = "return" _ value:branch
        => #RETURN { value }
    / "yield" _ "in" _ enumerator:branch
            => #YIELD_IN { enumerator }
    / "yield" _ value:branch
            => #YIELD { value }
    / "resume" => #RESUME { }
    / "next" => #NEXT { }
    / "break" => #BREAK { }

/************* BRANCHING *************/

branch
    = optionWithPipe
    / loop

loop
    = "while" _ condition:valueExpr __ body:loopBody
        => #WHILE { condition, body }
    / "repeat" body:loopBody
        => #REPEAT { body }

loopBody
    = branch
    / inlineBlock
    / return

optionWithPipe
    = value:pipedExpr option:(ternary / case / wise / for)? {
        if (option) {
            option.key = value
            return option
        }
        else
            return value
    }

for
    = _ "each" _ "@" _ variable:id __ body:loopBody
        => #FOR_EACH { 
            variable: #VALUE_BY_NAME { name: variable, namespace: [] }, 
            body
        }
    / _ "each" body:case {
        const variableId = #IDENTIFIER { name: null }
        const variableName = #NAMES { identifiers: [variableId] }
        const variableDef = #CONST_DEF { names: variableName, type: null, value: null }
        body.key = #RESOLVED_VARIABLE { ref: variableId }
        return #FOR_EACH { variable: variableDef, body }
    }

optionNoPipe
    = value:valueExpr option:(ternary / wise)? {
        if (option) {
            option.key = value
            return option
        }
        else
            return value
    }

optionNoCase
    = value:pipedExpr option:(ternary / wise / for)? {
        if (option) {
            option.key = value
            return option
        }
        else
            return value
    }

optionalTernary
    = value:pipedExpr option:ternary? {
        if (option) {
            option.key = value
            return option
        }
        else
            return value
    }

wise
    = _ "wise" __ body:(branch / inlineBlock / return) {
        return #WISE {
            body
        }
    }

ternary
    = __ "?" __ ifTrue:(branch / inlineBlock / return) ifFalse:(__ "else" __ v:(branch / inlineBlock / return) => v)?
        => #TERNARY { ifTrue, ifFalse }
case
    = _ (_ pipe _ / eol pipe _) c:caseBody => c

inlineBlock
    = "{" !(([-+&*^] / [><] "="? / "!=") "}") __ statements:funStatements __ "}"
        => #CODE_BLOCK { statements, effects: [] }

/************* PATTERN MATCHING *************/

caseBody
    = hd:caseOption tl:(__ pipe _ o:caseOption => o)*
    otherValue:(__ "-->" __ value:(branch / inlineBlock / return) => value)? {
        var cases = hd::tl
        if (otherValue)
            cases.push(#CASE_OPTION {
                patterns: [#CAPTURE { name: "_", type: #ANY_TYPE { } }],
                value: otherValue })
        return #CASE { cases }
    }

caseOption
    = hd:pattern tl:(_ "," __ p:pattern => p)* __ "->" __ value:(optionNoCase / inlineBlock / return)
        => #CASE_OPTION { patterns: hd::tl, value }

pattern
    = taggedPattern
    / valueExpr

taggedPattern
    = tag:tag value:(_ p:pattern => p)?
        => #TAGGED_VALUE {
            tag,
            value: value ?? #VOID_VALUE { }
        }
    / linkedListPattern

linkedListPattern
    = head:untaggedPattern tail:(_ "::" __ t:linkedListPattern => t)? {
        if (tail)
            return #LINKED_LIST {
                head,
                tail
            }
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
        => #COMPARISON_PATTERN { operator, value }

tuplePattern
    = "(" startEllipsis:(_ "...")? __ hd:pattern tl:(_ "," __ v:pattern => v)*
    endEllipsis:(_ "..." _)? close:(__ ")")? {
        if (!close) {
            if (tl.length > 0)
                error("Expecting \")\" to close the tuple")
            else
                error("Expecting matching \")\"")
        }
        return tl.length > 0
            ? #TUPLE { values: hd::tl, startEllipsis: !!startEllipsis, endEllipsis: !!endEllipsis  }
            : hd
        }

recPattern
    = "{|" _ ellipsis:("..." _)? "|}"
        => #REC_VALUE { members: [], ellipsis: !!ellipsis }
    / "{|" __ hd:recMemberPattern tl:(_ "," __ m:recMemberPattern => m)*
    ellipsis:(_ "..." _)? close:(__ "|}")? {
        if (!close) error("Expecting \"|\x7d\" to close the record")
        return #REC_VALUE { members: hd::tl, ellipsis: !!ellipsis }
    }

recMemberPattern
    = name:id _ colon __ value:pattern
        => #REC_FIELD_VALUE { name, value }

listPattern
    = "[" _ ellipsis:("..." _)? "]"
        => #LIST { values: [], startEllipsis: false, endEllipsis: !!ellipsis }

    / "[" startEllipsis:(_ "...")? _ hd:pattern tl:(_ "," __ v:pattern => v)*
    endEllipsis:(_ "..." _)? close:(__ "]")? {
        if (!close) error("Expecting \"]\" to close the list")
        return tl.length > 0
            ? #LIST { values: hd::tl, startEllipsis: !!startEllipsis, endEllipsis: !!endEllipsis }
            : hd
    }

setPattern
    = "set" _ "{" _ ellipsis:("..." _)? "}"
        => #SET { values: [], ellipsis: !!ellipsis }

    / "set" _ "{" __ hd:pattern tl:(_ "," __ v:pattern => v)* __ ellipsis:("..." _)? close:"}"? {
        if (!close) error("Expecting \"\x7d\" to close the set")
        return tl.length > 0
            ? #SET { values: hd::tl, ellipsis: !!ellipsis }
            : hd
        }

dictPattern
    = "dict" _ "{" _ ellipsis:("..." _)? "}"
        => #DICT_VALUE { elements: [], ellipsis: !!ellipsis }

    / "dict" _ "{" __ hd:dictKeyPattern tl:(_ "," __ m:dictKeyPattern => m)
    __ ellipsis:("..." _)? close:"}"? {
        if (!close) error("Expecting \"\x7d\" to close the dictionary")
        return #DICT_VALUE { elements: hd::tl, ellipsis: !!ellipsis }
    }

dictKeyPattern
    = key:valueExpr _ "->" __ value:pattern
        => #DICT_KEY_VALUE { key, value }

capture
    = "@" id:id type:(_ t:tupleType => t)?
        => #CAPTURE {
            name: id,
            type: type ?? #ANY_TYPE { }
        }
    / id:"_" => #CAPTURE { name: id, type: #ANY_TYPE { } } // any value

/************* TYPE DEFINITIONS *************/

type = unionType

unionType
    = hd:functionType tl:(_ pipe __ t:functionType => t)* {
        if (tl.length > 0) {
            const types = hd::tl
            if (types.some(t => tree.is(t, tree.VOID_TYPE)))
                error("Void type cannot be in a union")
            return #UNION_TYPE { types }
        }
        else
            return hd
    }

functionType
    = isAsync:isAsync purity:purity "fun" params:functionTypeParams? {
        var fixedParams = params ?? [#VOID_TYPE {}]
        if (fixedParams.length <= 1)
            error("Function need an input and a return type")
        return #FUN_TYPE {
                isAsync,
                purity,
                kind: "fun",
                params: fixedParams.slice(0, fixedParams.length - 1),
                returnType: fixedParams[fixedParams.length - 1]
        }
    }
    / voidTypeOrNot

functionTypeParams
    = _ "(" __ hd:unionType tl:(__ "->" __ t:unionType => t)* __ ")"
        => hd::tl

voidTypeOrNot
    = voidType
    / concatType

concatType
    = hd:tupleType tl:(_ concatOp __ tupleType)* {
        return tl.length > 0
            ? #CONCAT_TYPE { types: hd::tl }
            : hd
    }

tupleType
    = hd:tuplePowerType tl:(_ "*" __ t:tuplePowerType => t)* {
        return tl.length > 0
            ? #TUPLE_TYPE { types: hd::tl }
            : hd }
tuplePowerType
    = base:optionType power:(_ powerOp __ i:integer => i)? {
        return power
            ? #TUPLE_POWER_TYPE { base, power }
            : base }
optionType
    = base:traitIntersection isOption:"?"? {
        return isOption
            ? #OPTION_TYPE { type: base }
            : base }

traitIntersection
    = hd:specializeType tl:(__ "&" __ t:specializeType => t)* {
        if (tl.length > 0)
            return #TRAIT_INTER { traits: hd::tl }
        else
            return hd
    }

specializeType
    = hd:linkedListType tl:(_ "-" __ t:linkedListType => t)* {
        let types = hd::tl
        if (types.length > 1)
            return #SPECIALIZE_TYPE { base: types.pop(), params: types }
        else
            return hd
    }

linkedListType = elementType:atomicType tail:(_ "<>")? {
        if (tail)
            return #LINKED_LIST_TYPE { type: elementType }
        else
            return elementType
    }

atomicType
    = anyType
    / listType
    / setType
    / dictType
    / recType
    / voidType
    / typeByName
    / "(" __ t:type __ ")" => t
    / "#" => error("Unexpected \"#\" in type expression")

listType = "[" __ elementType:type __ "]" => #LIST_TYPE { type: elementType }

setType = "set" _ "{" __ elementType:type __ "}" => #SET_TYPE { type: elementType }

dictType
    = "dict" _ "{" __ key:tupleType _ "->" __ value:tupleType __ "}"
        => #DICT_TYPE { key, value }

recType
    = "{|" __
    hd:recMemberType tl:((_ "," __ / eol) m:recMemberType => m)*
    close:(__ "|}")? {
        if (!close)
            error("Expecting \"|\x7d\" to close the record type")
        return #REC_TYPE {
            members: hd::tl
        }
    }
recMemberType
    = recInheritance
    / recFieldType
    / propDef
    / fun
    / constructor

recInheritance
    = "..." parent:specializeType => #INHERITANCE { parent }

recFieldType
    = modifier:recFieldTypeModifier? names:(_ i:identifiers => i) type:(_ t:type => t)?
    defaultValue:(_ ":" __ value:valueExpr => value)? {
        if (!type && !defaultValue)
            error("The type or the value of the member must be specified")
        return #REC_FIELD_TYPE {
            modifier,
            names,
            type: type ?? #ANY_TYPE {},
            defaultValue
        }
    }
    / modifier:"base" _ names:identifiers _ "=" __ defaultValue:valueExpr
        => #REC_FIELD_TYPE {
            modifier,
            names,
            defaultValue
        }
recFieldTypeModifier
    = "const" / "init" / "var"

propDef
    = p:propHeader getter:getterBody setter:setter? {
        p.getter = getter
        p.setter = setter
        return p
    }
propHeader
    = purity:(s:"state" _ => s)? "prop" _ name:id _ type:type?
        => #PROP_DEF {
            purity: purity ?? "pure",
            name,
            type
        }
setter
    = __ "set" _ body:lambda => body
getterBody
    = __ "=>" __ v:branch => v
    / __ "{" __ s:funStatements __ "}"
        => #CODE_BLOCK { statements: s, effects: [] }

lambda
    = isAsync:isAsync purity:purity kind:(m:"sub" _ => m)? "=>" __ value:optionNoPipe
        => #FUN_DEF {
            isAsync,
            purity,
            kind: kind ?? "fun",
            name: null,
            genericParams: [],
            params: tree.getLambdaVariables(value).toSorted().map(name =>
                #FUN_PARAM_DEF {
                    names: [#IDENTIFIER { name }],
                    type: #ANY_TYPE {},
                    mutable: false
                }),
            body: value,
            returnType: kind === "sub" ? #VOID_TYPE {} : #ANY_TYPE {}
        }
    / isAsync:isAsync purity:purity kind:("sub" / colon) _ genericParams:traitConstraints _
    params:params __ returnType:(colon _ t:type __ => t)? body:lambdaBody
        => #FUN_DEF {
             isAsync,
             purity,
             kind: kind === ":" ? "fun" : kind,
             name: null,
             genericParams,
             params: params,
             body: body,
             returnType: returnType ?? (kind === "sub" ? #VOID_TYPE {} : #ANY_TYPE {})
         }
     / isAsync:isAsync purity:purity "enum" __ returnType:(colon _ t:type __ => t)? body:lambdaBody
        => #INLINE_ENUM {
            isAsync,
            purity,
            body: body,
            returnType: returnType ?? #ANY_TYPE {}
        }

lambdaBody
    = "=>" __ v:optionNoPipe => v
    / "{" __ s:funStatements __ "}"
        => #CODE_BLOCK { statements: s, effects: [] }
    / case

constructor
    = isAsync:isAsync "new" returnType:(_ pipe _ t:type => t)? body:constructorBody
        => #FUN_DEF {
            isAsync,
            kind: "fun",
            name: "new",
            genericParams: [],
            params: [],
            body,
            returnType
        }
constructorBody
    = __ s:funStatements __ when:when? __ "end"
        => #CODE_BLOCK { statements: s, when }

voidType = void => #VOID_TYPE {}

anyType = "any" => #ANY_TYPE {}

/************* EXPRESSIONS *************/

pipedExpr
    = option:valueExpr pipeCalls:(__ "\\" __ c:appendCall => c)* {
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

valueExpr = default

default = hd:disjunction tl:(__ "??" __ d:disjunction => d)* {
        return tl.length > 0
            ? #DEFAULT_VALUE { values: hd::tl }
            : hd
    }

disjunction
    = hd:xor tl:(__ "or" __ o:xor => o)* {
        return tl.length > 0
            ? #DISJUNCTION { operands: hd::tl }
            : hd
        }
xor
    = hd:conjunction tl:(__ "xor" __ o:conjunction => o)* {
        return tl.length > 0
            ? #XOR { operands: hd::tl }
            : hd
        }
conjunction
    = hd:comparison tl:(__ "&" !"=" __ o:comparison => o)* {
        return tl.length > 0
            ? #CONJUNCTION { operands: hd::tl }
            : hd
        }

comparison
    = firstOperand:aboveComparison otherOperands:otherComparisonOperand* {
        return otherOperands.length > 0
            ? #COMPARISON {
                operands: [#COMPARISON_OPERAND { operator: null, value: firstOperand }].concat(otherOperands)
            }
            : firstOperand
        }
otherComparisonOperand
    = __ operator:comparisonOp __ operand:aboveComparison
        => #COMPARISON_OPERAND { operator, value: operand }

aboveComparison = setComparison

setComparison
    = firstOperand:aboveSetComparison otherOperands:otherSetComparisonOperand* {
        return otherOperands.length > 0
            ? #SET_COMPARISON {
                operands: [#SET_COMPARISON_OPERAND { operator: null, value: firstOperand }].concat(otherOperands)
            }
            : firstOperand
        }
otherSetComparisonOperand
    = _ operator:setComparisonOp __ operand:aboveSetComparison
        => #SET_COMPARISON_OPERAND { operator, value: operand }

aboveSetComparison = linkedList

linkedList
    = head:modifyRec tail:(__ "::" __ tl:linkedList => tl)? {
        if (tail)
            return #LINKED_LIST { head, tail }
        else
            return head
    }

modifyRec
    = record:concat changes:(__ "<|" _ hd:modifyMember tl:(__ "<|" c:modifyMember => c)* => hd::tl)? {
        if (changes)
            return #MODIFY_REC { record, changes }
        else
            return record
    }

modifyMember
    = name:id _ colon __ value:valueExpr
        => #REC_FIELD_VALUE { name, value }
    / "!" _ name:id
        => #REC_FIELD_VALUE { name, value: #VOID_VALUE {} } // remove

concat
    = hd:addition tl:(__ concatOp __ other:addition => other)* {
        return tl.length > 0
            ? #CONCAT { operands: hd::tl }
            : hd
        }

addition
    = hd:firstTerm tl:otherTerm* {
        return tl.length > 0 || hd.sign !== "+"
            ? #ADDITION { terms: hd::tl }
            : hd.value
        }
firstTerm
    = sign:additiveOp? _ term:term
        => #TERM { sign:sign ?? "+", value:term }

otherTerm
    = _ sign:additiveOp __ term:term
        => #TERM { sign, value:term }

term = call

call = nestedCall

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

effects // []
    = _ "with" _ hd:default tl:(_ "," __ d:default => d)*
        => hd::tl

appendCall
    = _ fun:multiplication defer:(_ "defer")? 
    effects:(_ e:effects => e)? params:rightParams?
        => #CALL { fun, defer: !!defer, params: params ?? [], effects: effects ?? [] }

rightParams
    = _ hd:nestedCall tl:(_ ";" __ c:nestedCall => c)*
        => hd::tl

callParam = setDiff

setDiff
    = hd:setUnion tl:(__ "{-}" __ t:setUnion => t)* {
        if (tl.length > 0)
            return #SET_DIFF { sets: hd::tl }
        else
            return hd
    }

setUnion
    = hd:setInter tl:(__ "{+}" __ t:setInter => t)* {
          if (tl.length > 0)
              return #SET_UNION { sets: hd::tl }
          else
              return hd
      }

setInter
    = hd:cartesianProd tl:(__ "{&}" __ t:cartesianProd => t)* {
          if (tl.length > 0)
              return #SET_INTER { sets: hd::tl }
          else
              return hd
      }

cartesianProd
    = hd:range tl:(__ "{*}" __ r:range => r)* {
        if (tl.length > 0)
            return #CARTESIAN_PROD { sets: hd::tl }
        else
            return hd
    }

cartesianPower
    = base:range power:(_ "{^}" __ r:range => r)* {
        if (tl.length > 0)
            return #CARTESIAN_POWER { base, power }
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
    = _ op:rangeOp __ lastOrCount:multiplication step:(_ "by" __ s:multiplication => s)?
        => #RANGE { lastOrCount, op, step }

multiplication
    = hd:factor tl:otherFactor* {
        return tl.length > 0
            ? #MULTIPLICATION {
                factors: [#FACTOR { operator:"*", value:hd }].concat(tl)
            }
            : hd
        }
otherFactor
    = __ operator:multiplicativeOp __ factor:factor
        => #FACTOR { operator, value:factor }
factor
    = not
    / compose;

not = notOp f:factor
    => #NOT { value:f }

compose
    = hd:exponentiation tl:(_ composeOp __ a:exponentiation => a)* {
        if (tl.length > 0)
            return #COMPOSE { functions: hd::tl }
        else
            return hd
    }

exponentiation
    = base:mutValue power:(_ powerOp __ p:mutValue => p)? {
        return power
            ? #EXPONENTIATION { base, power }
            : base
        }

mutValue
    = "mut" _ value:indexing
        => #MUTABLE_PARAM { value }
    / indexing

indexing
    = list:taggedAtom index:(_ "[" __ i:branch __ "]" => i)? {
        return index
            ? #INDEXING { list, index }
            : list
        }

/************* ATOMS *************/

taggedAtom
    = tag:tag value:(__ v:taggedAtom => v)?
        => #TAGGED_VALUE { tag, value: value ?? #VOID_VALUE {} }
    / atom

atom
    = tuple
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

voidValue
    = void => #VOID_VALUE {}
    / "<>" => #EMPTY_LINKED_LIST {}

implicitParam
    = "$" [a-z]
        => #IMPLICIT_PARAM { name: text() }
    / "$" => error("Expecting letter for implicit parameter after \"$\"")

tuple
    = "(" __ hd:branch tl:(_ "," __ v:branch => v)* close:")"? {
        if (!close) {
            if (tl.length > 0)
                error("Expecting \")\" to close the tuple")
            else
                error("Expecting matching \")\"")
        }
        return tl.length > 0
            ? #TUPLE { values: hd::tl }
            : hd
        }

recValue
    = "{|" __ members:recMembers? close:(__  "|}") {
        if (!close) error("Expecting \"|\x7d\" to close the record")
        return #REC_VALUE { members: members ?? [] }
    }
recMembers
    = hd:recMemberValue tl:(_ ("," _ / eol) m:recMemberValue => m)*
        => hd::tl
recMemberValue
    = recMemberFieldValue
    / propDef
    / fun
    / splat
recMemberFieldValue
    = modifier:(m:recMemberFieldValueModifier _ => m)? name:id _ type:type? _ colon __ value:branch
        => #REC_FIELD_VALUE { modifier, name, type, value }
recMemberFieldValueModifier
    = "const" / "var"

splat
    = "..." value:valueByName
        => #SPLAT { value }

list = "[" _ elements:listElements? close:(_ "]")? {
        if (!close) error("Expecting \"]\" to close the list")
        return #LIST { values: elements ?? [] }
    }
listElements
    = __ hd:(splat / branch) tl:(_ "," __ v:(splat / branch) => v)* __
        => hd::tl

set = "set" _ "{" _ elements:setElements? close:(_ "}")? {
        if (!close) error("Expecting \"\x7d\" to close the set")
        return #SET { values: elements ?? [] }
    }
setElements
    = __ hd:(splat / branch) tl:(_ "," __ v:(splat / branch) => v)* __
        => hd::tl

dictValue
    = "dict" _ "{" _ elements:dictElements? _ close:"}"? {
        if (!close) error("Expecting \"\x7d\" to close the dictionary")
        return #DICT_VALUE { elements: elements ?? [] }
    }
dictElements
    = __ hd:dictKeyValue tl:(_ "," __ m:dictKeyValue => m)* __
        => hd::tl
dictKeyValue
    = key:valueExpr _ "->" __ value:branch
        => #DICT_KEY_VALUE { key, value }
    / splat

newObject
    = "new" _ type:specializeType __ params:tuple
        => #CREATE_OBJECT { type, params }

getMember
    = container:(valueByName / implicitParam)
    path:("." id:id => id)* {
        if (path.length > 0)
            return #GET_MEMBER { container, path }
        else
            return container
    }
    / path:("." id:id => id)+ {
        return #GET_WISE_MEMBER { path }
    }

valueByName
    = namespace:(i:id "--" => i)* name:valueId
        => #VALUE_BY_NAME { name, namespace }
    / "me" => #VALUE_BY_NAME { name: "me", namespace: [] }

valueId
    = id
    / "(" op:overridableOp ")" => op

typeByName
    = namespace:(i:id "--" => i)* name:typeId
        => #TYPE_BY_NAME { name, namespace }

typeId
    = id
    / "enum"
    / traitAlias

traitAlias
    = "(" o:overridableOp ")" => o

boolean
    = value:("yes" / "no")
        => #BOOLEAN { value: value === "yes" }

tag
    = "#" s:specializeType => s

/************* TOKENS *************/

identifiers
    = hd:id tl:(_ "/" __ id:id => id)* {
        return (hd::tl).map(id => #IDENTIFIER { name: id }) 
    }

id = $(!keyword "_"? [A-Za-z] [A-Za-z0-9_]*)

integer = [0-9] [0-9_]* ([eE] "+"? [0-9]+)?
    => #INTEGER { value: BigInt(text()) }

float = beforePoint:$([0-9] [0-9_]*) afterPoint:$("." [0-9] [0-9_]*)? expPart:$([eE] [+-]? [0-9]+)? {
        if (afterPoint || expPart && expPart.includes("-") )
            return #FLOAT { value: parseFloat(text()) }
        else
        {
            if (expPart)
            {
                const expValue = expPart.substring(1)
                const factor = BigInt(10) ** BigInt(expValue)
                return #INTEGER { value: BigInt(beforePoint) * factor }
            }
            else
                return #INTEGER { value: BigInt(text()) }
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
    / "wise" / "else" / "while" / "repeat" / "each" / "next" / "break" / "when" / "resume"
    / "in" / "by" / "or" / "xor" / "export" / "async" / "defer" / "use") ![A-Za-z0-9_]

colon = $(":" ![=:])

pipe = $("|" !"}")
logicOp = "or" / "xor" / $("&" !"=")
comparisonOp = $("=" !">") / "!=" / rightAngleBracket / ">=" / leftAngleBracket / "<=" / "in" ![_a-zA-Z0-9]
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
    => #STRING_VALUE { parts }

literalPart = chars:literalChar+
    => #STRING_PART { value: chars.join("") }
formattedValue
    = "{" _ value:branch _ "}"
    => #FORMATTED_VALUE { value }
literalChar
    = [^"{}]
    / "{{" => "\x7b"
    / "}}" => "\x7d"
    / "\"\"" => "\""
