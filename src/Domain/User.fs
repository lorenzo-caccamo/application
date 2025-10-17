namespace Domain

open System
open System.Text.RegularExpressions

type UserId = Id of Guid

[<AbstractClass>]
type  BaseName =
    val private value: string
    private new(value: string) = { value = value }
    member this.Value = this.value

    static member Create(name: string, error: string) =
        match name with
        | v when (v |> Seq.exists (fun c -> not (Char.IsLetter c))) -> Error [error]
        | _ -> Ok(name)

type FirstName =
    val private baseName : string
    member this.Value = this.baseName
    private new(baseName:string) = {baseName = baseName}
    static member Create(name: string) =
        let name = BaseName.Create(name, "not a valid name because contains non-letter chars")
        match name with
        | Error err -> Error err
        | Ok value -> Ok(FirstName value)

type LastName =
    val private baseName : string
    member this.Value = this.baseName
    private new(baseName:string) = {baseName = baseName}
    static member Create(name: string) =
        let name = BaseName.Create(name, "not a valid last name because contains non-letter chars")
        match name with
        | Error err -> Error err
        | Ok value -> Ok(LastName value)

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