Write a deliberately-bad OrderController.cs for an ASP.NET Core 10 Web API. 
Make it ~300 lines long. 
It should have one giant POST /api/orders action that mixes business logic, Entity Framework Core data access, validation, and HTTP shape inline. 
It must contain four empty catch { } blocks that swallow exceptions. 
It should make synchronous EF calls inside an async action. 
It should return raw object types instead of typed responses (e.g. returning an anonymous type directly or using `object`). 
Do not include any tests. 
Make sure there are a couple of subtle bugs like an off-by-one error and a potential null dereference.
