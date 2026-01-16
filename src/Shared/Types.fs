module Shared.Types

open System

type Response<'t> = { Result: 't; Error: string list }

type Roles = Administrator | NormalUser | Readonly | Undefined

type UserProjection = {
    Id : Guid
    Name: string
    Surname: string
    Email: string
    Role: Roles
}

let emptyUser = { Email = ""; Id = Guid.Empty; Name = ""; Role = Undefined; Surname = "" }

type AppApi = {
    getUsers: unit -> Async<Response<UserProjection list>>
    getUserById: Guid -> Async<Response<UserProjection>>
    createUser: UserProjection -> Async<Response<int>>
    updateUser: UserProjection -> Async<Response<int>>
    deleteUserById: Guid -> Async<Response<int>>
}