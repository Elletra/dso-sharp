### BreakStmtNode

```
OP_JMP
breakPoint
```


### ContinueStmtNode

```
OP_JMP
continuePoint
```


### ReturnStmtNode

```
if we're returning an expression/value:
	expression
	OP_RETURN
else:
	OP_RETURN
```


### IfStmtNode

```
test expression

if there's an else block:
	<OP_JMPIFNOT (integer)/OP_JMPIFFNOT (float)>
	else address
	if block
	OP_JMP
	else end address
	else block
else:
	<OP_JMPIFNOT (integer)/OP_JMPIFFNOT (float)>
	if end address
	if block
```


### ConditionalExprNode (ternary operator)

```
test expression
<OP_JMPIFNOT/OP_JMPIFFNOT>
else address
true expression
OP_JMP
end address
// else addr
false expression
// end addr
```


### LoopStmtNode

```
if there's an initial expression:
	initial expression

test expression
<OP_JMPIFNOT/OP_JMPIFFNOT>
breakpoint addr

loop block

if there's an end loop expression:
	end loop expression

test expression

<OP_JMPIF/OP_JMPIFF>
loop block start addr
```


### FloatBinaryExprNode

```
right side expression
left side expression
<OP_ADD/OP_SUB/OP_DIV/OP_MUL>
if type != float:
	conversion to float operation
```


### IntBinaryExprNode

```
if operator == OP_OR || operator == OP_AND:
	left side expression

	if operator == OP_OR:
		OP_JMPIF_NP
	else:
		OP_JMPIFNOT_NP

	Jump address

	right side expression
	// jump address
else:
	right side expression
	left side expression
	operator

if not UInt type:
	conversion to UInt operation
```


### StreqExprNode

```
left side expression
OP_ADVANCE_STR_NUL
right side expression
OP_COMPARE_STR

if not equal:
	OP_NOT

if not UInt type:
	conversion to UInt operation
```


### StrcatExprNode

```
left side expression

if we're appending a character:
	OP_ADVANCE_STR_APPENDCHAR
	char to append to string
else:
	OP_ADVANCE_STR

right side expression

OP_REWIND_STR

if type is UInt:
	OP_STR_TO_UINT
else if type is float:
	OP_STR_TO_FLT
```


### CommaCatExprNode

```
left side expression
OP_ADVANCE_STR_COMMA
right side expression
OP_REWIND_STR

if type is UInt:
	OP_STR_TO_UINT
else if type is float:
	OP_STR_TO_FLT
```


### IntUnaryExprNode

```
expression

if operator is '!':
	<OP_NOT/OP_NOTF>
else if operator is '~':
	OP_ONESCOMPLEMENT

if type is not UInt:
	convert operation to UInt
```


### FloatUnaryExprNode

```
expression
OP_NEG

if type is not float:
	convert operation to float
```


### VarNode

```
if we have an array index:
	OP_LOADIMMED_IDENT
	variable name index
	OP_ADVANCE_STR
	array index expression
	OP_REWIND_STR
	OP_SETCURVAR_ARRAY
else:
	OP_SETCURVAR
	variable name index

<OP_LOADVAR_UINT/OP_LOADVAR_FLT/OP_LOADVAR_STR>
```


### IntNode

```
if type is UInt:
	OP_LOADIMMED_UINT
	integer value
else if type is string:
	OP_LOADIMMED_STR
	string index
else if type is float:
	OP_LOADIMMED_FLT
	float index
```


### FloatNode

```
if type is UInt:
	OP_LOADIMMED_UINT
	integer value
else type is string:
	OP_LOADIMMED_STR
	string index
else type is float:
	OP_LOADIMMED_FLT
	float index
```


### StrConstNode

```
if type is string:
	if is tagged string:
		OP_TAG_TO_STR
	else:
		OP_LOADIMMED_STR
	string index
else if type is UInt:
	OP_LOADIMMED_UINT
	integer value
else if type is float:
	OP_LOADIMMED_FLT
	float index
```


### ConstantNode

```
if type is string:
	OP_LOADIMMED_IDENT
	string index
else if type is UInt:
	OP_LOADIMMED_UINT
	integer value
else if type is float:
	OP_LOADIMMED_FLT
	float index
```


### AssignExprNode

```
expression

if has array index:
	if subtype is string:
		OP_ADVANCE_STR

	OP_LOADIMMED_IDENT
	variable name index
	OP_ADVANCE_STR
	array index expression
	OP_REWIND_STR
	OP_SETCURVAR_ARRAY_CREATE

	if subtype is string:
		OP_TERMINATE_REWIND_STR
else:
	OP_SETCURVAR_CREATE
	variable name index

if subtype is string:
	OP_SAVEVAR_STR
else if type is UInt:
	OP_SAVEVAR_UINT
else if type is float:
	OP_SAVEVAR_FLT

if type is not subtype:
	convert operation to subtype
```


### AssignOpExprNode

```
expression

if has array index:
	OP_LOADIMMED_IDENT
	variable name index
	OP_ADVANCE_STR
	array index expression
	OP_REWIND_STR
	OP_SETCURVAR_ARRAY_CREATE
else:
	OP_SETCURVAR_CREATE
	variable name index

if subtype is float:
	OP_LOADVAR_FLT
else:
	OP_LOADVAR_UINT

operator

if subtype is float:
	OP_SAVEVAR_FLT
else:
	OP_SAVEVAR_UINT

if subtype is not type:
	convert operation to subtype
```


### SlotAccessNode

```
if has array expression:
	array expression
	OP_ADVANCE_STR

object expression
OP_SETCUROBJECT
OP_SETCURFIELD
slot name index

if has array expression:
	OP_TERMINATE_REWIND_STR
	OP_SETCURFIELD_ARRAY

if type is UInt:
	OP_LOADFIELD_UINT
else if type is float:
	OP_LOADFIELD_FLT
else if type is string:
	OP_LOADFIELD_STR
```


### SlotAssignNode

```
value expression
OP_ADVANCE_STR

if has array expression:
	array expression
	OP_ADVANCE_STR

if has object expression:
	object expression
	OP_SETCUROBJECT
else:
	OP_SETCUROBJECT_NEW

OP_SETCURFIELD
slot name index

if has array expression:
	OP_TERMINATE_REWIND_STR
	OP_SETCURFIELD_ARRAY

OP_TERMINATE_REWIND_STR
OP_SAVEFIELD_STR

if type is not string:
	convert operation to string
```


### SlotAssignOpNode

```
value expression

if has array expression:
	array expression
	OP_ADVANCE_STR

object expression
OP_SETCUROBJECT
OP_SETCURFIELD
slot name index

if has array expression:
	OP_TERMINATE_REWIND_STR
	OP_SETCURFIELD_ARRAY

if subtype is float:
	OP_LOADFIELD_FLT
else:
	OP_LOADFIELD_UINT

operator

if subtype is float:
	OP_SAVEFIELD_FLT
else:
	OP_SAVEFIELD_UINT

if subtype is not type:
	convert operation to subtype
```


### ObjectDeclNode

```
if object is a root object (not a subobject of another object):
	OP_LOADIMMED_UINT
	0

OP_PUSH_FRAME
class name expression
OP_PUSH

object name expression
OP_PUSH

for each argument:
	expression
	OP_PUSH

OP_CREATE_OBJECT
parent object index
isDataBlock
fail jump addr

for each slot assign node:
	slot expression

OP_ADD_OBJECT
placeAtRoot

for each subobject:
	subobject

OP_END_OBJECT

if placeAtRoot:
	placeAtRoot
else:
	isDataBlock

if type is not UInt:
	convert operation to UInt
```


### FuncCallExprNode

```
OP_PUSH_FRAME

for each argument:
	expression
	OP_PUSH

if call type is MethodCall or call type is ParentCall:
	OP_CALLFUNC
else:
	OP_CALLFUNC_RESOLVE

function name index
namespace index
call type

if type is not string:
	convert operation to string
```


### FunctionDeclStmtNode

```
OP_FUNC_DECL
function name index
namespace index
package name index
has body
func end addr
number of arguments

for each argument:
	variable name index

statements
OP_RETURN
```
