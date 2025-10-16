namespace Infrastructure

open System
open DataAccess.DbConnectionHandler
open Domain
open Dapper.FSharp.MSSQL
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
        FirstName = firstName
        LastName =  surname
    }

    let private toDomain (user: UserEntity) =
        let maybeUserData =
            createUserData <*> Email.Create(user.Email)
            <!> FirstName.Create(user.Name)
            <!> LastName.Create(user.Surname)

        match maybeUserData with
        | Error err -> Error err
        | Ok data ->
            match user.Role with
            | Admin -> Ok(User.Admin { Data = data; Id = Id user.Id })
            | Normal -> Ok(User.Normal { Id = (Id user.Id); Data = data })
            | ReadOnly -> Ok(User.ReadOnly { Id = (Id user.Id); Data = data })


    let private createUserEntity (u: BaseUser) (role: Role) =
        let (Id id) = u.Id

        {
            Id = id
            Name = u.Data.FirstName.Value
            Surname = u.Data.LastName.Value
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

    let userById (userId: UserId) (hdr:DbConHandler) = tryM {
        return task {
            let (Id id) = userId

            let! usersEntity =
                select {
                    for u in userTable do
                        where (u.Id = id)
                }
                |> hdr.Conn.SelectAsync<UserEntity>

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

    let allUsers (hdr:DbConHandler) = tryM {
        return task {
            let! usersEntity =
                select {
                    for u in userTable do
                        selectAll
                }
                |> hdr.Conn.SelectAsync<UserEntity>

            let users = usersEntity |> Seq.map toDomain |> Seq.map toUserResult
            return users
        }
    }

    let createUser (user: User) (hdr:DbConHandler) = tryM {
        let usr = user |> toEntity

        return task {
            let! inserted =
                insert {
                    into userTable
                    value usr
                }
                |> hdr.Conn.InsertAsync<UserEntity>

            if inserted > 0 then
                return Successful(inserted)
            else
                return FailToCreate("user not inserted")
        }
    }

    let updateUser (user: User) (hdr:DbConHandler) = tryM {
        let usr = user |> toEntity

        return task {
            let! updated =
                update {
                    for u in userTable do
                        set usr
                        where (u.Id = usr.Id)
                }
                |> hdr.Conn.UpdateAsync<UserEntity>

            if updated > 0 then
                return Successful(updated)
            else
                return FailToUpdate("user not updated")
        }
    }

    let deleteUser (userId: UserId) (hdr:DbConHandler) = tryM {
        let (Id id) = userId

        return task {
            let! deleted =
                delete {
                    for u in userTable do
                        where (u.Id = id)
                }
                |> hdr.Conn.DeleteAsync

            if deleted > 0 then
                return Successful(deleted)
            else
                return FailToDelete("user not delete")
        }
    }