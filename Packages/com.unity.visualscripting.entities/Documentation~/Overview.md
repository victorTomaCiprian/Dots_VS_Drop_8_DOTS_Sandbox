# Visual Scripting Technical Overview

A scripting graph lifecycle is the product of two separations: asset and scene objects, gameobjects and entities.

|Editor Asset|Scene Object|Runtime Asset|Runtime Object|
|------------|------------|-------------|--------------|
|VSGraphModel|ScriptingGraphAuthoring,ScriptingGraphAsset|ScriptingGraph,GraphDefinition|ScriptingGraphInstance|
|VariableDeclarationModel|InputBindingAuthoring|InputBinding|ValueInputs|

## Graphs

During translation, the VSGraphModel creates an embedded ScriptableObject, the ScriptingGraphAsset, containing a GraphDefinition. This asset is referenced on a GameObject by a ScriptingGraphAuthoring conversion component.

During conversion, the ScriptingGraphAuthoring adds two components to the entity: a ScriptingGraph shared component that references the definition asset and a ScriptingGraphInstance components holding instance specific values.

## Inputs/outputs:

Each VariableDeclarationModel in the graph model is translated to an InputBinding in the graph definition - a BindingId ( the variable model guid) and a reserved data index.

The ScriptingGraphAuthoring has a list of InputBindingAuthoring mapping a BindingId to a value, which can be scene GameObjects.

During the conversion, a BlobAsset of type ValueInputs is created, and a reference to that blob asset is stored in the ScriptingGraphInstance component.

The ScriptingGraphRuntime fills the data buckets described in the bindings with the values in the blob asset
Bound object
