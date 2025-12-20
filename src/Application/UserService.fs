module Application.UserService

open System
open Domain
open Shared.Monads
open UserProjection

type IUserRepository = {
    byId: UserId -> Async<TryS<Result<User, string list>, exn>>
    all: unit -> Async<TryS<Result<User list, string list>, exn>>
    add: User -> Async<TryS<Result<int, string list>, exn>>
    delete: UserId -> Async<TryS<Result<int, string list>, exn>>
    update: User -> Async<TryS<Result<int, string list>, exn>>
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
        | Error err -> async { return TryS.liftOk (Error err) }
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
        | Error err -> async { return TryS.liftOk (Error err) }
}
