module Application.UserService

open System
open Domain
open Domain.DomainResult
open Shared.Monads

type IUserRepository = {
    byId: UserId -> Try<Async<DomainResult<User, string list>>, exn>
    all: unit -> Try<Async<DomainResult<User, string list> seq>, exn>
    add: User -> Try<Async<DomainResult<int, string>>, exn>
    delete: UserId -> Try<Async<DomainResult<int, string>>, exn>
    update: User -> Try<Async<DomainResult<int, string>>, exn>
}

let getUserById (id: UserId) =
    reader{
        let! (r: IUserRepository) = Reader.ask
        return r.byId id
    }

let getAllUsers () =
    reader{
        let! (r: IUserRepository) = Reader.ask
        return r.all ()
    }

let addUser (user: User) =
     reader{
        let! (r:IUserRepository) = Reader.ask
        return r.add user
    }

let deleteUser (id: UserId) =
    reader{
        let! (r:IUserRepository) = Reader.ask
        return r.delete id
    }

let updateUser (user: User) =
    reader{
        let! (r:IUserRepository) = Reader.ask
        return r.update user
    }