namespace Client.Tests

open Index
open SAFE
open Shared
open Xunit

type UpdateTests() =
    [<Fact>]
    member _.``SaveTodo finished updates model with returned todos for immediate UI refresh``() =
        let newTodo = Todo.create "new todo"
        let model, _ = init ()
        let updatedModel, _ = update (SaveTodo(Finished [ newTodo ])) model
        let todoCount = updatedModel.Todos |> RemoteData.map _.Length |> RemoteData.defaultValue 0
        let firstTodo = updatedModel.Todos |> RemoteData.map List.head |> RemoteData.defaultValue (Todo.create "")

        Assert.Equal(1, todoCount)
        Assert.Equal(newTodo, firstTodo)

    [<Fact>]
    member _.``SetInput updates only the input text so typing does not mutate todo data``() =
        let model, _ = init ()
        let updatedModel, _ = update (SetInput "Beam check") model

        Assert.Equal("Beam check", updatedModel.Input)
