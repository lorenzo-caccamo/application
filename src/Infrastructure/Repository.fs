namespace Infrastructure

open System
open Domain
open Dapper.FSharp.MSSQL
open Infrastructure.DbConnectionHandler
open Shared.Monads
open Shared.Functions

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
        LastName = surname
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

    let userById (hdr: DbConHandler) (userId: UserId) = async {
        let (Id id) = userId

        use conn = hdr.GetConnection()

        return tryS {
            let usersEntity =
                select {
                    for u in userTable do
                        where (u.Id = id)
                        orderBy u.Surname
                        thenBy u.Name
                }
                |> conn.SelectAsync<UserEntity>
                |> Async.AwaitTask
                |> Async.RunSynchronously

            let users = usersEntity |> Seq.map toDomain |> Seq.tryExactlyOne

            return
                match users with
                | None -> Error([ $"too many users with id {id}" ])
                | Some value ->
                    match value with
                    | Ok user -> Ok(user)
                    | Error err -> Error(err)
        }
    }


    let userById_ (hdr: DbConHandler) (userId: UserId) =
        let (Id id) = userId
        use conn = hdr.GetConnection()

        tryA {
            let res =
                select {
                    for u in userTable do
                        where (u.Id = id)
                        orderBy u.Surname
                        thenBy u.Name
                }
                |> conn.SelectAsync<UserEntity>
                |> Async.AwaitTask

            let c = async {
                let! usersEntity = res
                let users = usersEntity |> Seq.map toDomain |> Seq.tryExactlyOne

                return
                    match users with
                    | None -> Error([ $"too many users with id {id}" ])
                    | Some value ->
                        match value with
                        | Ok user -> Ok(user)
                        | Error err -> Error(err)
            }

            return c
        }





    let allUsers (hdr: DbConHandler) () = async {
        use conn = hdr.GetConnection()

        return tryS {
            let usersEntity =
                select {
                    for u in userTable do
                        selectAll
                }
                |> conn.SelectAsync<UserEntity>
                |> Async.AwaitTask
                |> Async.RunSynchronously

            return (<!*>) (usersEntity |> Seq.map toDomain |> Seq.toList)
        }
    }

    let createUser (hdr: DbConHandler) (user: User) = async {
        let usr = user |> toEntity

        use conn = hdr.GetConnection()

        return tryS {
            let inserted =
                insert {
                    into userTable
                    value usr
                }
                |> conn.InsertAsync<UserEntity>
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if inserted > 0 then
                return Ok(inserted)
            else
                return Error([ "user not inserted" ])
        }
    }

    let modifyUser (hdr: DbConHandler) (user: User) = async {
        let usr = user |> toEntity

        use conn = hdr.GetConnection()

        return tryS {
            let updated =
                update {
                    for u in userTable do
                        set usr
                        where (u.Id = usr.Id)
                }
                |> conn.UpdateAsync<UserEntity>
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if updated > 0 then
                return Ok(updated)
            else
                return Error([ "user not updated" ])
        }
    }

    let removeUser (hdr: DbConHandler) (userId: UserId) = async {
        let (Id id) = userId

        use conn = hdr.GetConnection()

        return tryS {
            let deleted =
                delete {
                    for u in userTable do
                        where (u.Id = id)
                }
                |> conn.DeleteAsync
                |> Async.AwaitTask
                |> Async.RunSynchronously

            if deleted > 0 then
                return Ok(deleted)
            else
                return Error([ "user not delete" ])
        }
    }
