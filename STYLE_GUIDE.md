# PoE-Bot Style Guide

## Code Patterns

These patterns are the most consistent and relevant within our code.

### Tasks and Async/Await

This particular project relies a lot on network communication to function. A vast majority of the code utilizes Tasks 
to maintain asynchronous functionality as a result. Tasks are a fairly simple construct, but can result in weird 
behaviors when not in specific manners. The largest reason for this is due to the nature of how `Tasks` are run. Unlike 
`Thread` invocation, the runtime uses `Tasks` to agnostically handle `async` ops. If a `Thread` is optimal *(and 
available)*, the runtime will start a `Thread`. If, however, the runtime decides that it is better to utilize an event 
loop/messaging channel, it will do that instead. This allows us to simply define `async` operations and `await` those 
`Tasks`.

#### Task Return Types

Proper returning and notification is key to utilizing `Tasks` appropriately. 

* Use `Task<Type>` over `Task` for `async` functions, whenever possible. This allows code to return data rather than
utilizing properties for in-process values.
* Use `Task` over `void` for `async` functions.
* The only valid `async` function with a `void` return type is an `EventHandler`.

#### Task-based Interoperations

* Use `Task.Run()` to asyncify synchronous functions.
* Use `TaskCompletionSource` over callbacks. [This article](https://www.pluralsight.com/guides/task-taskcompletion-source-csharp)
demonstrates several simple uses for `TaskCompletionSource`.
* Otherwise, default to simply awaiting or running the function with a `Task<Type>` return.

### Factories and Builders

Factories and Builders are one of the most useful patterns in code, and we use them frequently to build complex 
structures.

* All Factory methods should be synchronous.
* If a Factory's completion depends on `async` operations: `await` the `Task(s)` first, then initialize and `Build()` the 
object.
* Factory methods should be chained in one continuous call chain.
* 
* When possible, `Build()` method should be added at the end of the call chain.

**Good Factory**
```csharp
var asyncValue = await asyncTaskResult();
var builtObject = new ObjectFactory()
	.property1("Value")
	.reference(new otherObject())
	.asyncProperty(asyncValue)
	.Build();
```

## Naming Conventions

Naming is one of the most important aspects of code, because it allows multiple collaborators to understand what is 
going on with the code without actually running it. Proper naming lowers the barrier of entry for new coders and 
contributors, and can remind veterans of the project what is happening in functions and procedures that have not been 
recently maintained or modified by the individual. 

### Namespaces

* `PascalCase` **Always**

### Constants

* `UPPER_CASE`

### Local Variables

* `camelCase` 
* No `txtHungarianNotation`. *(Example: `fieldId` **not** `lFieldId`)*
* Be descriptive, yet concise: Should not only indicate the data type, but *general* usage.
* Names should start with a noun, not a descriptor. *(Example: `fieldId` **not** `idField`)*
* Use `var` when ***both*** the name and the initializer are sufficiently descriptive.
* Use `var` when initialized with a Factory/Builder. *(See Method Chaining)*
* Use a specific type when either the name or context is vague, or when the initializer is not straight-forward.

**Example: Clear name and Context**
```csharp
var games = (await twitch.Helix.Games.GetGamesAsync(null, new List<string>(new string[] { _stream.Game })));
```

**Example: Clear name, but vague context**
```csharp
var rssData = await GetRssAsync(feed.FeedUrl);
// ExcludeRecentPosts doesn't elucidate a return type
List<RssItem> rssPosts = await ExcludeRecentPosts(feed, rssData);
```

**Example: Clear name, but chained methods obscure type**
```csharp

```


### Local Functions/Method Declarations

* `PascalCase`
* Names should start with an action verb, except in a Factory class.

### Asynchronous/Synchronous Functions

The vast majority of functions and methods within PoE-Bot interact with a remote server or service and are, therfore, 
asynchronous in nature. It is preferred that, for the purposes of this project, all synchronous functions be suffixed 
with `Sync`. While any sync function may use any return value, `async` functions should whenever possible return `Task<T>`

**Asynchronous Function Example**
```csharp
public async Task<bool> DoStuff() 
{

}
```

**Synchronous Function Example**
```csharp
public bool DoStuffSync()
{

}
```

### Events

* `PascalCase`
* Should end in an action verb
* Should be immediately recognizable from code and coding assistants.

**Single Events**
```csharp
public event EventHandler ButtonClick;
```

**Pre/Post Events**
```csharp
public event EventHandler BeforeTextChange;
public event EventHandler AfterTextChanged;
```

### Event Handlers

* Follow `OnEventName()` format, if handler is a predefined `Method()` of a Class.
* Follow `EventNameHandler()` format, if handler is a local function, parameter or return value.
* Should always return `void`, unless an external event requires otherwise.

**Object Event Handler**
```csharp
class MyClass {
	public event EventHandler MyEvent;

	public void OnMyEvent(object sender, EventArgs data) 
	{
		// Handle event here...
	}
}
```

**Passed Event Handler**
```csharp
public MyClass {
	public event EventHandler MyEvent;

	public Func<void> GetMyEventHandler() 
	{
		var MyEventHandler = (object sender, EventArgs data) => 
		{
			// Handle event here...
		};
		return MyEventHandler;
	}
}
```
