module Shared.Monads

type TryS<'r, 'err> = TryS of predicate: (unit -> Result<'r, 'err>)

module TryS =
    let run (TryS f) =
        try
            f ()
        with ex ->
            Error ex

    let bind (f: 't -> TryS<'r, exn>) (a: TryS<'t, exn>) =
        TryS(fun () ->
            match run a with
            | Ok t -> run (f t)
            | Error err -> Error err)

    let liftOk (t: 't) = TryS(fun () -> Ok t)

    let liftErr (t: 't) = TryS(fun () -> Error t)

    let map (f: 't -> 'r) (a: TryS<'t, exn>) = a |> bind (fun t -> liftOk (f t))

    let (>>=) (a: TryS<'b, exn>) (f: 'b -> TryS<'c, exn>) = a |> bind f

type TryBuilder() =
    member _.Bind(a, f) = TryS.bind f a
    member _.Return(a) = TryS(fun () -> a)
    member _.ReturnFrom(a) = a
    member _.Zero() = TryS.liftOk ()
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
    let run (TryA f) =async{
        try
            let! res = f()
            return res
        with ex -> return (Error [ex.Message])
    }

    let bind f a =
        TryA(fun () ->
            async{
                let! res = run a
                match res with
                | Ok ok -> return f ok
                | Error err -> return Error err
                }
        )

    let liftOk a = TryA(fun () -> async { return Ok a })

type TryABuilder() =
    member _.Return(x) = TryA(fun () -> x)
    member _.ReturnFrom(x) = x
    member _.Bind(x, f) = TryA.bind f x

let tryA = TryABuilder()
