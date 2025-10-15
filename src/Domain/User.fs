namespace Domain

open System
open System.Text.RegularExpressions

type UserId = Id of Guid

type BaseName =
    val private value: string
    private new(value: string) = { value = value }
    member this.Value = this.value

    static member Create(name: string) =
        match name with
        | v when (v |> Seq.exists (fun c -> not (Char.IsLetter c))) -> Error ["not a valid name"]
        | _ -> Ok(BaseName name)

type FirstName = FirstName of BaseName

type LastName = LastName of BaseName

type Email =
    val private value: string
    private new(value: string) = { value = value }
    member this.Value = this.value

    static member Create(email: string) =
        match email with
        | value when Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") -> Error ["not a valid email"]
        | _ -> Result.Ok(Email email)

type UserData = {
    FirstName: FirstName
    LastName: LastName
    Email: Email
}

type BaseUser = { Id: UserId; Data: UserData }

type User =
    | Admin of BaseUser
    | Normal of BaseUser
    | ReadOnly of BaseUser

type UserResult<'a, 'b> =
    | Successful of 'a
    | NotFound of 'b
    | InvalidUser of 'b
    | FailToCreate of 'b
    | FailToUpdate of 'b
    | FailToDelete of 'b
