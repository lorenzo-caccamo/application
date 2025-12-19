module UserProjection

open System

type Roles = Administrator | NormalUser | Readonly

type UserProjection = {
    Id : Guid
    Name: string
    Surname: string
    Email: string
    Role: Roles
}