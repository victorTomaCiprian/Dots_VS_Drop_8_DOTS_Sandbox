Jan 30/2020 - This is a very tiny guideline for creating nodes

# LABELING
Trigger output ports must be labeled *Output*
Single Data output ports is labeled *Result*
Input & Output data, which range from 0..1 is called *Progress* (ex: Lerp)
For nodes that are executing on many frames, use the trigger ports *Start* for Input, *Done* for output when completed.
If you are doing a FlowNode, you must have an input trigger in order to update the data & you must have an *Output* that trigger every time it is updated

# NODES TYPES

## CONSTANT NODES
Constant node are executed only at initalzation, and never after.
Node example: ConstantFloat
Inherit from : **IConstantNode<T>**, where T is the type of the constant.
Members to implement: void Execute(GraphInstance ctx), called only once at init.

## DATA NODES
Data nodes are executed only when an output value is read by another node (Pull model).
They do not have any internal state & are deterministics (result is only determined by inputs)
Node example: Multiplication, Addition, Substraction, etc. (MathBinaryNumber)
Inherit from : **IDataNode**
Members to implement: void Execute(GraphInstance ctx), called every time output values must be recomputed.

## ENTRY POINTS NODES (TRIGGERS)
These nodes have only outputs & are triggered by external events
Node example: OnUpdate, OnTriggerEnter, OnCollisionEnter, etc.
Inherit from : **IEntryPointNode**
Members to implement: void Execute(GraphInstance ctx), called every time the event occur.

## FLOW NODES
Flow nodes always have triggers in input. They usually perform logical decisions or operations/actions.
They execute in a single frame, when triggered.
Node example: If, Switch, SetPosition, etc.
Inherit from : **IFlowNode**
Members to implement: void Execute(GraphInstance ctx, InputTriggerPort port)
They receive a port identifier, to identify which pin was triggered when there is more than one input trigger.

## STATEFULL FLOW NODES
Statefull flow noded act like flow nodes, but they laso have he capacity to store data, which can be used for execution over multiple frames.
Inside the node, call GraphInstance.GetState() to retreive your own state data.
Node example: StopWatch, Wait, All, etc.
Inherit from : **IFlowNode<T>**, where T is the type of your persistent data
Members to implement:
Execution Execute(GraphInstance ctx, InputTriggerPort port)
Like Flow nodes, this is called with a trigger port, identifying, which pin was triggered.
An Excecution status must be returned; Execution.Done, if the execution is completed on that call, or Execution.Running if the execution must
be continued on a subsequent frame. When Execution.Running is returned, this second member will then be called every frame:
Execution Update(GraphInstance ctx)
Update will be called every frame, as long as it returns Execution.Running.
If Execution.Done is returned, Update will never be called anymore, until a pin re-trigger it trough Execute()
IMPORTANT NOTE: it is possible that in a same frame, many pins are triggered, and the node is also updated.
Update from last frame will always be the first thing executed, then triggers.
If 2 pins are triggered in the last frame, returning different values (Running & Done), the last pin executed will decide if Update execute next frame.
Ex: StopWatch *Start* input trigger pin is triggerd; Execution.Running is returned. Then *Stop* is called right after. Execution.Done is returned.
The node is then removed from update queue for next frame.

# GraphInstance usefull functions
GetState- Retreive your persistent Data
ReadData - Read from a data port
WriteData - Write a data port
Trigger - Start execution on a trigger output port

# Dynamic ports numbers
All ports types, also have a Multiport version, which enable the dynamic creation of more entries.
One good example of this type of port is for the switch case node.
All MultiPort node have a SelectPort member, which will create a port based on a runtime index number.
This new port can be used to call ReadData/WriteData/Trigger/etc.

# Edge connections
It is possible to connect an output trigger node, to many input trigger node. The order of execution is undetermined for now.
It is possible to connect 1 data output, to multiple data input. The data will be stored in the type selected by the node having the
output data node. All reading nodes will cast their data when reading if necessary. What des it mean? If you connect your output data
node to a node that requires an int as an input, that would not affect if you create an edge from the same output to a second input node.
