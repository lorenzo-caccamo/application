namespace Infrastructure

open System
open System.Data
open Domain
open Dapper.FSharp.MSSQL
open Microsoft.Data.SqlClient
open Shared.Monads

module Repository =
    type private Role =
        | Admin
        | Normal
        | ReadOnly

    type private UserEntity = {
        Id: Guid
        Email: string
        Name: string
        Surname: string
        Role: Role
    }

    let private userTable = table<UserEntity>

    let private createUserData email firstName surname : UserData = {
        Email = email
        FirstName = FirstName firstName
        LastName = LastName surname
    }

    let private toDomain (user: UserEntity) =
        let maybeUserData =
            createUserData <*> Email.Create(user.Email)
            <!> BaseName.Create(user.Name)
            <!> BaseName.Create(user.Surname)

        match maybeUserData with
        | Error err -> Error err
        | Ok data ->
            match user.Role with
            | Admin -> Ok(User.Admin { Data = data; Id = Id user.Id })
            | Normal -> Ok(User.Normal { Id = (Id user.Id); Data = data })
            | ReadOnly -> Ok(User.ReadOnly { Id = (Id user.Id); Data = data })


    let private createUserEntity (u: BaseUser) (role: Role) =
        let (Id id) = u.Id
        let (FirstName name) = u.Data.FirstName
        let (LastName lstName) = u.Data.LastName

        {
            Id = id
            Name = name.Value
            Surname = lstName.Value
            Email = u.Data.Email.Value
            Role = role
        }

    let private toEntity (user: User) =
        match user with
        | User.Admin user -> createUserEntity user Admin
        | User.Normal user -> createUserEntity user Normal
        | User.ReadOnly user -> createUserEntity user ReadOnly

    let private toUserResult (res: Result<User, string list>) =
        match res with
        | Error err -> InvalidUser(err)
        | Ok v -> Successful(v)


    let userById (userId: UserId) (conn:IDbConnection) = tryM {
        return task {
            let (Id id) = userId

            let! usersEntity =
                select {
                    for u in userTable do
                        where (u.Id = id)
                }
                |> conn.SelectAsync<UserEntity>

            let users = usersEntity |> Seq.map toDomain

            return
                match Seq.length users with
                | 0 -> NotFound([ $"user with id {id} not found" ])
                | 1 ->
                    match Seq.head users with
                    | Ok user -> Successful(user)
                    | Error err -> InvalidUser(err)
                | _ -> InvalidUser([ $"too many users with id {id}" ])
        }
    }

    let allUsers (conn:IDbConnection) = tryM {
        return task {
            let! usersEntity =
                select {
                    for u in userTable do
                        selectAll
                }
                |> conn.SelectAsync<UserEntity>

            let users = usersEntity |> Seq.map toDomain |> Seq.map toUserResult
            return users
        }
    }

    let createUser (user: User) (conn:IDbConnection) = tryM {
        let usr = user |> toEntity

        return task {
            let! inserted =
                insert {
                    into userTable
                    value usr
                }
                |> conn.InsertAsync<UserEntity>

            if inserted > 0 then
                return Successful(inserted)
            else
                return FailToCreate("User not inserted")
        }
    }

    let updateUser (user: User) (conn:IDbConnection) = tryM {
        let usr = user |> toEntity

        return task {
            let! updated =
                update {
                    for u in userTable do
                        set usr
                        where (u.Id = usr.Id)
                }
                |> conn.UpdateAsync<UserEntity>

            if updated > 0 then
                return Successful(updated)
            else
                return FailToUpdate("User not updated")
        }
    }

    let deleteUser (userId: UserId) (conn: IDbConnection) = tryM {
        let (Id id) = userId

        return task {
            let! deleted =
                delete {
                    for u in userTable do
                        where (u.Id = id)
                }
                |> conn.DeleteAsync

            if deleted > 0 then
                return Successful(deleted)
            else
                return FailToDelete("User not delete")
        }
    }
