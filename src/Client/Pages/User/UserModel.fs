module UserModel

open System
open Shared.Types

type Model = {
    request: UserDto
    result: Response<UserDto list>
}

type Msg =
    | Load
    | Loaded of User list
    | LoadFailed of string
    | SetNewName of string
    | SetNewEmail of string
    | Create
    | Created of User
    | CreateFailed of string
    | Delete of int
    | Deleted of int
    | DeleteFailed of string

let init: Model * Elmish.Cmd<Msg> =
    {
        request = {
            Email = ""
            Id = Guid.Empty
            Name = ""
            Role = Undefined
            Surname = ""
        }
        result = { Result = []; Error = [] }
    },
    Elmish.Cmd.ofMsg Load
