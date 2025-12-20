module Application.UserService

open System
open Domain
open Shared.Monads
open UserProjection

type IUserRepository = {
    byId: UserId -> TryA<User, string list>
    all: unit -> TryA<User list, string list>
    add: User -> TryA<int, string list>
    delete: UserId -> TryA<int, string list>
    update: User -> TryA<int, string list>
}

let private createUserData email firstName surname : UserData = {
    Email = email
    FirstName = firstName
    LastName = surname
}

let private map (user: UserProjection) : Result<User, string list> =
    let maybeUsrData =
        createUserData <*> Email.Create user.Email
        <!> FirstName.Create user.Name
        <!> LastName.Create user.Surname

    match maybeUsrData with
    | Ok u ->
        match user.Role with
        | Administrator -> Ok(User.Admin { Id = Id user.Id; Data = u })
        | NormalUser -> Ok(User.Normal { Id = Id user.Id; Data = u })
        | Readonly -> Ok(User.ReadOnly { Id = Id user.Id; Data = u })
    | Error err -> Error err

let getUserById (id: Guid) = reader {
    let! (r: IUserRepository) = Reader.ask
    return r.byId (Id id)
}

let getAllUsers () = reader {
    let! (r: IUserRepository) = Reader.ask
    return r.all ()
}

let addUser (user: UserProjection) = reader { // TODO Id must not be mapped
    let! (r: IUserRepository) = Reader.ask

    return
        match map user with
        | Ok u -> r.add u
        | Error err -> TryA(fun () -> async { return Error err })
}

let deleteUser (id: Guid) = reader {
    let! (r: IUserRepository) = Reader.ask
    return r.delete (Id id)
}

let updateUser (user: UserProjection) = reader {
    let! (r: IUserRepository) = Reader.ask

    return
        match map user with
        | Ok u -> r.update u
        | Error err -> TryA(fun () -> async { return Error err })
}
