namespace Server.Tests

open Server
open Shared
open Xunit

type StorageTests() =
    [<Fact>]
    member _.``Adding a valid todo returns Ok and stores the item for later retrieval``() =
        let todo = Todo.create "Review setup note formatting rules"
        let beforeCount = Storage.todos.Count

        let result = Storage.addTodo todo

        Assert.Equal(Ok(), result)
        Assert.Equal(beforeCount + 1, Storage.todos.Count)
        Assert.Equal(todo, Storage.todos[Storage.todos.Count - 1])

    [<Fact>]
    member _.``Adding an invalid todo returns an error so empty tasks are never persisted``() =
        let todo = Todo.create ""
        let beforeCount = Storage.todos.Count

        let result = Storage.addTodo todo

        Assert.Equal(Error "Invalid todo", result)
        Assert.Equal(beforeCount, Storage.todos.Count)
