module Shared.Monads

type Try<'r, 'err> = Try of predicate: (unit -> Result<'r, 'err>)

// type TryT<'r, 'err> = TryT of Try<Async<'r>, Async<'err>>

// module TryT =
//
//     let inline run (TryT f) = f

// let inline bind (f: 't -> '``OutM<Try<'r, 'err>>>``) (a: TryT<'``OutM<Try<'t, 'err>>``>) =
//     f (run a) |> TryT

(*Examples
let tryParse (str: string) = Try(fun () -> Ok(str |> int))
let tryDivide (num: int) = Try(fun () -> Ok(1/num))
let res (str: string) = (tryParse str) |> Try.bind tryDivide
let ``go&back`` (str:string) = (tryParse str) |> Try.map (fun num -> (num |> string))*)

module Try =
    let run (Try f) =
        try
            f ()
        with ex ->
            Error ex

    let bind (f: 't -> Try<'r, exn>) (a: Try<'t, exn>) =
        Try(fun () ->
            match run a with
            | Ok t -> run (f t)
            | Error err -> Error err)

    let liftOk (t: 't) = Try(fun () -> Ok t)

    let liftErr (t: 't) = Try(fun () -> Error t)

    let map (f: 't -> 'r) (a: Try<'t, exn>) = a |> bind (fun t -> liftOk (f t))

    let (>>=) (a: Try<'b, exn>) (f: 'b -> Try<'c, exn>) = a |> bind f

type TryBuilder() =
    member _.Bind(a, f) = Try.bind f a
    member _.Return(a) = Try.liftOk a
    member _.ReturnFrom(a) = a
    member _.Zero() = Try.liftOk ()
    member _.Delay(f) = f ()

let tryS = TryBuilder()

type Reader<'env, 'a> = Reader of action: ('env -> 'a)

module Reader =
    /// Run a Reader with a given environment
    let run env (Reader action) = action env // simply call the inner function

    /// Create a Reader which returns the environment itself
    let ask = Reader id // id is identity function (fun env -> env)

    /// Map a function over a Reader
    let map f reader = Reader(fun env -> f (run env reader))

    /// flatMap a function over a Reader
    let bind f reader =
        Reader(fun env ->
            let x = run env reader
            run env (f x)) // è la stessa cosa di fare: let action env = let x run.. Reader action (action è una func che prende env e restituisce 'c, per cui e' necessario run env fx)

type ReaderBuilder() =
    member _.Return(x) = Reader(fun _ -> x)
    member _.Bind(x, f) = Reader.bind f x
    member _.Zero() = Reader(fun _ -> ())

// the builder instance
let reader = ReaderBuilder()

let (<*>) = Result.map

let (<!>) t1 t2 =
    match (t1, t2) with
    | Ok f, Ok value -> Ok(f value)
    | Ok _, Error err -> Error(err)
    | Error err, _ ->
        match t2 with
        | Ok _ -> Error err
        | Error err2 -> Error(err2 @ err)

type TryA<'r, 'err> = TryA of predicate: (unit -> Async<Result<'r, 'err>>)

module TryA =
    let run (TryA f) = async {
        try
            return! f ()
        with ex ->
            return Error ex
    }

    let bind f a =
        TryA(fun () -> async {
            let! res = run a

            return!
                match res with
                | Ok ok -> run (f ok)
                | Error err -> async { return Error err }
        })

    let liftOk a = TryA(fun () -> async { return Ok a })

type TryABuilder() =
    member _.Return(x) = TryA.liftOk (x)
    member _.ReturnFrom(x) = x
    member _.Bind(x, f) = TryA.bind f x

let tryA = TryABuilder()



// type TryAsync<'a> = TryAsync of Try<Async<'a>, exn>
//
// module TryAsync =
//     let run (TryAsync m) = m
//     let bind (f: Async<'a> -> Try<Async<'b>, exn>) (a: TryAsync<'a>) =
//         TryAsync(
//             let b = run a
//             match b |> Try.run with
//             | Ok ok -> f  ok
//             | Error err -> Try.liftErr err
//         )
//
//     // let inline hoist (x: 'a option) : OptionT<'``Monad<option<'a>>``> = x |> result |> OptionT
//     let hoist (a: Async<'a>) = a |> Try.liftOk
//
// type TryAsyncBuilder() =
//     member _.Return(x) = TryAsync x
//     member _.ReturnFrom(x) =  x
//     member _.Bind(x, f) = TryAsync.bind f x
//     member _.Zero() =
//         TryAsync(Try(fun () -> Result.Ok(async.Return())))
//
// // the builder instance
// let tryAsync = TryAsyncBuilder()
