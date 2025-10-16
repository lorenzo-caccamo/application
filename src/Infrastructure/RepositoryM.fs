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

 type Repository =
    val private conn : IDbConnection
    private new(conn:IDbConnection) = {conn = conn}
    static member Create(conn:string) =
        let dbconn = new SqlConnection (conn)
        match dbconn with
        | null -> None
        | v -> Some(Repository v )

    member this.userById (userId: UserId) = tryM {
        return task {
            let (Id id) = userId

            let! usersEntity =
                select {
                    for u in Repository.userTable do
                        where (u.Id = id)
                }
                |> this.conn.SelectAsync<Repository.UserEntity>

            let users = usersEntity |> Seq.map Repository.toDomain

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

    member this.allUsers = tryM {
        return task {
            let! usersEntity =
                select {
                    for u in Repository.userTable do
                        selectAll
                }
                |> this.conn.SelectAsync<Repository.UserEntity>

            let users = usersEntity |> Seq.map Repository.toDomain |> Seq.map Repository.toUserResult
            return users
        }
    }

    member this.createUser (user: User) = tryM {
        let usr = user |> Repository.toEntity

        return task {
            let! inserted =
                insert {
                    into Repository.userTable
                    value usr
                }
                |> this.conn.InsertAsync<Repository.UserEntity>

            if inserted > 0 then
                return Successful(inserted)
            else
                return FailToCreate("User not inserted")
        }
    }

    member this.updateUser (user: User) = tryM {
        let usr = user |> Repository.toEntity

        return task {
            let! updated =
                update {
                    for u in Repository.userTable do
                        set usr
                        where (u.Id = usr.Id)
                }
                |> this.conn.UpdateAsync<Repository.UserEntity>

            if updated > 0 then
                return Successful(updated)
            else
                return FailToUpdate("User not updated")
        }
    }

    member this.deleteUser (userId: UserId) = tryM {
        let (Id id) = userId

        return task {
            let! deleted =
                delete {
                    for u in Repository.userTable do
                        where (u.Id = id)
                }
                |> this.conn.DeleteAsync

            if deleted > 0 then
                return Successful(deleted)
            else
                return FailToDelete("User not delete")
        }
    }
