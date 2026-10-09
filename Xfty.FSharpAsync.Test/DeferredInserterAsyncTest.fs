module Net.NowhereAtAll.Xfty.FSharpAsync.Test.DeferredInserterAsyncTest

open System.Threading.Tasks
open Xunit
open Net.NowhereAtAll.Xfty.FSharpAsync
open Net.NowhereAtAll.Xfty.Persistence

[<Fact>]
let ``flush with nothing registered completes without a gateway`` () : Task =
    async {
        // Arrange
        // (nothing registered with DeferredInserter - the common "empty" case)

        // Act
        do! DeferredInserterAsync.flush None

        // Assert - completed without throwing; nothing pending needs no gateway
        Assert.True(true)
    }
    |> Async.StartAsTask
    :> Task

[<Fact>]
let ``flush with a gateway and nothing registered never calls the gateway`` () : Task =
    async {
        // Arrange
        let mutable insertCalls = 0
        let gateway =
            { new IPersisting with
                member _.Insert(_records, _idField) =
                    insertCalls <- insertCalls + 1
                    Task.CompletedTask }

        // Act
        do! DeferredInserterAsync.flush (Some gateway)

        // Assert
        Assert.Equal(0, insertCalls)
    }
    |> Async.StartAsTask
    :> Task
