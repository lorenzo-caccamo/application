module Tests

open System
open Xunit
open FsUnit
open Shared.Monads
open Shared.Monads.TryS
open Shared.Helper

[<Fact>]
let ``should return Ok`` () =
    //Arrange
    let str = "123"
    let tryParse s = TryS(fun () -> Ok(s |> int))
    let tryDivide (num: int) = TryS(fun () -> Ok(1 / num))

    //Act
    let res = (tryParse str) |> bind tryDivide

    //Assert
    (res |> run) |> getTaggedTypeName |> should equal "Ok"

[<Fact>]
let ``should return Fail`` () =
    //Arrange
    let str = ""
    let tryParse s = TryS(fun () -> Ok(s |> int))
    let tryDivide (num: int) = TryS(fun () -> Ok(1 / num))

    //Act
    let res = (tryParse str) |> bind tryDivide

    //Assert
    (res |> run) |> getTaggedTypeName |> should equal "Error"

[<Fact>]
let ``should return Fail when string "0"`` () =
    //Arrange
    let str = "0"
    let tryParse s = TryS(fun () -> Ok(s |> int))
    let tryDivide (num: int) = TryS(fun () -> Ok(1 / num))

    //Act
    let res = (tryParse str) >>= tryDivide

    //Assert
    (res |> run) |> getTaggedTypeName |> should equal "Error"

[<Fact>]
let ``should return Fail when string "0" computational expression`` () =
    //Arrange
    let str = "0"
    let tryParse s = TryS(fun () -> Ok(s |> int))
    let tryDivide (num: int) = TryS(fun () -> Ok(1 / num))

    //Act
    let res = tryS {
        let! strn = tryParse str
        let r = tryDivide strn
        return! r
    }

    //Assert
    (res |> run) |> Result.isError |> should equal true

[<Fact>]
let ``should return error when async fails`` () =
    //Arrange
    let doSome =
        TryA(fun () -> async {
            do! Async.Sleep(2000)
            failwith "unexpected error"
            return Ok("job completed successfully")
        })

    //Act
    let res = doSome |> TryA.run

    //Assert
    async {
        let! r = res
        r |> Result.isError |> should equal true
    }

[<Fact>]
let ``should return ok when async ok`` () =
    //Arrange
    let doSome =
        TryA(fun () -> async {
            do! Async.Sleep(2000)
            return Ok("job completed successfully")
        })

    //Act
    let res = doSome |> TryA.run

    //Assert
    async {
        let! r = res
        r |> Result.isOk |> should equal true
    }

[<Fact>]
let ``should return error when async fails_computational express`` () =
    //Arrange
    let doSome = tryA {
        return
            (async {
                do! Async.Sleep(2000)
                failwith "unexpected error"
                return Ok("job completed successfully")
            })
    }

    //Act
    let res = doSome |> TryA.run

    //Assert
    async {
        let! r = res
        r |> Result.isError |> should equal true
    }

[<Fact>]
let ``should return result when async ok_computational express`` () =
    //Arrange
    let doSome = tryA {
        return
            (async {
                do! Async.Sleep(2000)
                return Error(["job completed successfully"])
            })
    }

    //Act
    let res = doSome |> TryA.run

    //Assert
    async {
        let! r = res
        r |> Result.isError |> should equal true
    }

[<Fact>]
let ``should return fail when running sync async fail`` () =
    //Arrange
    let f =  fun() -> async {
                do! Async.Sleep(2000)
                failwith ""
                return Ok("job completed successfully")} |> Async.RunSynchronously
    let doSome = TryS(f)

    //Act
    let res = doSome |> run

    //Assert
    res |> Result.isError |> should equal true
