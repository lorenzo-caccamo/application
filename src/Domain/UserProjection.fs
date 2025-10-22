module Domain.UserProjection

open System

type Roles = Admin | Normal | Readonly

type UserProjection = {
    Id : Guid
    Name: string
    Surname: string
    Email: string
    Role: Roles
}