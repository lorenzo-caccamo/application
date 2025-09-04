module Application.UserService

open System
open Domain
open Shared.Monads
open Validator

let getUserById (id: UserId) =
    reader{
        let! (r: IRepository<User>) = Reader.ask
        return match r.byId id |> Try.run with
                | Ok user -> Result.Ok user
                | Fail err when (err :? InvalidOperationException) -> NotFound err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }

let getAllUsers () =
    reader{
        let! (r: IRepository<User>) = Reader.ask
        return match r.all () |> Try.run with
                | Ok user -> user |> Result.Ok
                | Fail err when (err :? InvalidOperationException) -> NotFound err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }

let addUser (user: User) =
    reader{
        let! (r:IRepository<User>) = Reader.ask
        return match r.create user |> Try.run with
                | Ok user -> user |> Result.Ok
                | Fail err when (err :? InvalidOperationException) -> RepositoryError err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }