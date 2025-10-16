module Application.UserService

open System
open Domain
open Shared.Monads

type UserRepository = {
    byId: UserId -> Try<UserResult<User, string list>, exn>
    all: unit -> Try<UserResult<User, string list> seq, exn>
    add: User -> Try<UserResult<int, string list>, exn>
    delete: UserId -> Try<UserResult<unit, string list>, exn>
    update: User -> Try<UserResult<unit, string list>, exn>
}

let getUserById (id: UserId) =
    reader{
        let! (r: UserRepository) = Reader.ask
        return match r.byId id |> Try.run  with
                | Ok user -> Result.Ok user
                | Fail err when (err :? InvalidOperationException) -> NotFound err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }

let getAllUsers () =
    reader{
        let! (r: UserRepository) = Reader.ask
        return match r.all () |> Try.run with
                | Ok user -> user |> Result.Ok
                | Fail err when (err :? InvalidOperationException) -> NotFound err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }

let addUser (user: User) =
    reader{
        let! (r:UserRepository) = Reader.ask
        return match r.add user |> Try.run with
                | Ok user -> user |> Result.Ok
                | Fail err when (err :? InvalidOperationException) -> RepositoryError err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }

let deleteUser (id: UserId) =
    reader{
        let! (r:UserRepository) = Reader.ask
        return match r.delete id |> Try.run with
                | Ok user -> user |> Result.Ok
                | Fail err when (err :? InvalidOperationException) -> RepositoryError err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }

let updateUser (user: User) =
    reader{
        let! (r:UserRepository) = Reader.ask
        return match r.update user |> Try.run with
                | Ok user -> user |> Result.Ok
                | Fail err when (err :? InvalidOperationException) -> RepositoryError err.Message |> Result.Error
                | Fail err -> GenericError err.Message |> Result.Error
    }