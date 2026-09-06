module Net.NowhereAtAll.Xfty.FSharpAsync.Test.RecordProviderAsyncTest

open System.Threading.Tasks
open Xunit
open Net.NowhereAtAll.Xfty.Core
open Net.NowhereAtAll.Xfty.Demo
open Net.NowhereAtAll.Xfty.FSharpAsync

let private lookup = DefaultProviderLookup()

[<Fact>]
let ``supply returns a generated record`` () : Task =
    async {
        // Arrange
        let provider = RecordProvider(typeof<Contact>, lookup)

        // Act
        let! result = RecordProviderAsync.supply provider

        // Assert
        Assert.NotNull(result)
    }
    |> Async.StartAsTask
    :> Task

[<Fact>]
let ``supplyList returns every requested record`` () : Task =
    async {
        // Arrange
        let provider = RecordProvider(typeof<Contact>, lookup).SetQuantityPerTemplate(3)

        // Act
        let! results = RecordProviderAsync.supplyList provider

        // Assert
        Assert.Equal(3, results.Count)
    }
    |> Async.StartAsTask
    :> Task

[<Fact>]
let ``supplyBundle returns a populated bundle`` () : Task =
    async {
        // Arrange
        let provider =
            RecordProvider(typeof<Contact>, lookup)
                .SetInsertMode(InsertMode.Mock)
                .SetInclusivity(InsertInclusivity.Required)

        // Act
        let! bundle = RecordProviderAsync.supplyBundle provider

        // Assert
        Assert.NotNull(bundle)
    }
    |> Async.StartAsTask
    :> Task
