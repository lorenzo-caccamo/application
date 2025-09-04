module Monad.Tests.Monad_Tests

open FsUnit
open Xunit
open Shared.Monads
open Shared.Helper

[<Fact>]
let rec ``should return Ok``()=
    //Arrange
    let str = "123"
    let tryParse s = Try(fun () -> Ok(s |> int))
    let tryDivide (num: int) = Try(fun () -> Ok(1/num))

    //Act
    let res = (tryParse str) |> Try.bind tryDivide

    //Assert
    (( Try.run res) |> getTaggedTypeName) |> should equal "Ok"
    // (res |> Try.run) |> should equal Ok









