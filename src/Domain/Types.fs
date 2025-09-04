namespace Domain

open System
open System.Text.RegularExpressions

type Uuid = Uuid of Guid
type UserId = Uuid
type FirstName = FirstName of string
type LastName = LastName of string

type Email =
    val Value: string
    private new(value: string) = { Value = value }

    static member Create(email: string) =
        match email with
        | value when Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") -> Error "not valid email"
        | _ -> Result.Ok(Email email)

type PersonalData = {
    FirstName: FirstName
    LastName: LastName
    Email: Email
}

type BaseUser = {
    Id: UserId
    PersonalData: PersonalData
}

type King = { Data: BaseUser }

type Civilian = { Data: BaseUser }

type Slave = { Data: BaseUser }

type User =
    | King of King
    | Civilian of Civilian
    | Slave of Slave

type DomainError<'a> =
    | ValidationError of 'a
    | NotFound of 'a
    | GenericError of 'a
    | RepositoryError of 'a
