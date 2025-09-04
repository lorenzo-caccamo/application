module Shared.Monads

type Exception<'r, 'err> =
    | Ok of 'r
    | Fail of 'err

type Try<'r, 'err> = Try of predicate: (unit -> Exception<'r, 'err>)

type TryT<'r, 'err> = TryT of Try<Async<'r>, Async<'err>>

module TryT =

    let inline run (TryT f) = f

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
            Fail ex

    let bind (f: 't -> Try<'r, exn>) (a: Try<'t, exn>) =
        Try(fun () ->
            match run a with
            | Ok t -> run (f t)
            | Fail err -> Fail err)

    let liftOk (t: 't) = Try(fun () -> Ok t)

    let liftErr (t: 't) = Try(fun () -> Fail t)

    let map (f: 't -> 'r) (a: Try<'t, exn>) = a |> bind (fun t -> liftOk (f t))

    let (>>=) (a: Try<'b, exn>) (f: 'b -> Try<'c, exn>) = a |> bind f

type TryBuilder() =
    member _.Bind(a, f) = Try.bind f a
    member _.Return(a) = Try.liftOk a

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