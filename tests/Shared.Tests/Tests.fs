module Tests

open System
open Xunit
open FsUnit
open Shared.Monads
open Shared.Monads.Try
open Shared.Helper

[<Fact>]
let ``should return Ok`` () =
    //Arrange
    let str = "123"
    let tryParse s = Try(fun () -> Ok(s |> int))
    let tryDivide (num: int) = Try(fun () -> Ok(1 / num))

    //Act
    let res = (tryParse str) |> bind tryDivide

    //Assert
    (res |> run) |> getTaggedTypeName |> should equal "Ok"

[<Fact>]
let ``should return Fail`` () =
    //Arrange
    let str = ""
    let tryParse s = Try(fun () -> Ok(s |> int))
    let tryDivide (num: int) = Try(fun () -> Ok(1 / num))

    //Act
    let res = (tryParse str) |> bind tryDivide

    //Assert
    (res |> run) |> getTaggedTypeName |> should equal "Fail"

[<Fact>]
let ``should return Fail when string "0"`` () =
    //Arrange
    let str = "0"
    let tryParse s = Try(fun () -> Ok(s |> int))
    let tryDivide (num: int) = Try(fun () -> Ok(1 / num))

    //Act
    let res = (tryParse str) >>= tryDivide

    //Assert
    (res |> run) |> getTaggedTypeName |> should equal "Fail"
