namespace Infrastructure

open System
open System.Data
open Microsoft.Data.SqlClient
open Domain
open Dapper.FSharp.MSSQL

module Repository =
    type Role =
        | Admin
        | Normal
        | ReadOnly

    type UserEntity = {
        Id: Guid
        Name: string
        Surname: string
        Email: string
        Role: Role
    }

    type Entities = | UserEntity of UserEntity

    let private users = table<UserEntity>

    let private conn: IDbConnection =
        new SqlConnection("connectionString") :> IDbConnection

    let private toDomain (user: UserEntity) =
        King {
            Data = {
                PersonalData = {
                    FirstName = FirstName user.Name
                    LastName = LastName user.Surname
                    Email =
                        match Email.Create user.Email with
                        | Ok email -> email
                        | _ -> failwith ""
                }
                Id = Uuid user.Id
            }
        }

    let private toEntity (user: User) =
        match user with
        | King k ->
            let (UserId.Uuid id) = k.Data.Id
            let (FirstName name) = k.Data.PersonalData.FirstName
            let (LastName lstName) = k.Data.PersonalData.LastName

            {
                Id = id
                Name = name
                Surname = lstName
                Email = k.Data.PersonalData.Email.Value
                Role = Admin
            }
        | Civilian c ->
            let (UserId.Uuid id) = c.Data.Id
            let (FirstName name) = c.Data.PersonalData.FirstName
            let (LastName lstName) = c.Data.PersonalData.LastName

            {
                Id = id
                Name = name
                Surname = lstName
                Email = c.Data.PersonalData.Email.Value
                Role = Normal
            }
        | Slave s ->
            let (UserId.Uuid id) = s.Data.Id
            let (FirstName name) = s.Data.PersonalData.FirstName
            let (LastName lstName) = s.Data.PersonalData.LastName

            {
                Id = id
                Name = name
                Surname = lstName
                Email = s.Data.PersonalData.Email.Value
                Role = ReadOnly
            }

    let byId (entity: Entities) =
        match entity with
        | UserEntity en -> task{
            let (id: Guid) = en.Id
            let! user =
                select {
                    for u in users do
                        where (u.Id = id)
                        }
                        |> conn.SelectAsync<UserEntity>

            return user |> Seq.map toDomain
            }

    let all (entity: Entities) =
        match entity with
        | UserEntity _ -> task {
            let! users =
                select {
                    for u in users do
                        selectAll
                }
                |> conn.SelectAsync<UserEntity>

            return users |> Seq.map toDomain
          }