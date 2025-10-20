module Shared.Functions

let resTrv (resLst: Result<'a, 'b list> list) =
    let from (t1: Result<'a, 'b list>) (seed: Result<'a list, 'b list>) =
        match t1, seed with
        | Ok v, Ok va -> Ok(v :: va)
        | Ok _, Error sErr -> Error(sErr)
        | Error vErr, Ok _ -> Error(vErr)
        | Error vErr, Error sErr -> Error(vErr @ sErr)
    let into = id
    List.foldBack from resLst (into Ok [])

let (<!*>) lst = resTrv lst