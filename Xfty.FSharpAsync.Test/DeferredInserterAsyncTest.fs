module Net.NowhereAtAll.Xfty.FSharpAsync.Test.DeferredInserterAsyncTest

open System.Threading.Tasks
open Xunit
open Net.NowhereAtAll.Xfty.FSharpAsync

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
