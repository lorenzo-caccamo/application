module Shared.Types

open System

type Response<'t> = { Result: 't; Error: string list }

type Roles = Administrator | NormalUser | Readonly | Undefined

type UserDto = {
    Id : Guid
    Name: string
    Surname: string
    Email: string
    Role: Roles
}

let emptyUser = { Email = ""; Id = Guid.Empty; Name = ""; Role = Undefined; Surname = "" }

type AppApi = {
    getUsers: unit -> Async<Response<UserDto list>>
    getUserById: Guid -> Async<Response<UserDto>>
    createUser: UserDto -> Async<Response<int>>
    updateUser: UserDto -> Async<Response<int>>
    deleteUserById: Guid -> Async<Response<int>>
}