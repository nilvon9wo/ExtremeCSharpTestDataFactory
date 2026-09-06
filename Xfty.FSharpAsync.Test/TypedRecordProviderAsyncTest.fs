module Net.Nowhereatall.Xfty.FSharpAsync.Test.TypedRecordProviderAsyncTest

open System.Threading.Tasks
open Xunit
open Net.Nowhereatall.Xfty.Core
open Net.Nowhereatall.Xfty.Demo
open Net.Nowhereatall.Xfty.FSharpAsync

let private lookup = DefaultProviderLookup()

[<Fact>]
let ``supply returns a typed record with no cast needed`` () : Task =
    async {
        // Arrange
        let provider = RecordProvider<Contact>(lookup)

        // Act
        let! result = TypedRecordProviderAsync.supply provider

        // Assert
        Assert.NotNull(box result)
    }
    |> Async.StartAsTask
    :> Task

[<Fact>]
let ``supplyList returns a typed list with no cast needed`` () : Task =
    async {
        // Arrange
        let provider = RecordProvider<Contact>(lookup).SetQuantityPerTemplate(2)

        // Act
        let! results = TypedRecordProviderAsync.supplyList provider

        // Assert
        Assert.Equal(2, results.Count)
    }
    |> Async.StartAsTask
    :> Task

[<Fact>]
let ``supplyBundle returns a populated bundle`` () : Task =
    async {
        // Arrange
        let provider =
            RecordProvider<Contact>(lookup)
                .SetInsertMode(InsertMode.Mock)
                .SetInclusivity(InsertInclusivity.Required)

        // Act
        let! bundle = TypedRecordProviderAsync.supplyBundle provider

        // Assert
        Assert.NotNull(bundle)
    }
    |> Async.StartAsTask
    :> Task
