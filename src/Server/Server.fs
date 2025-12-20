module Server

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open SAFE
open Saturn
open Shared.Monads
open Domain
open UserProjection
open Infrastructure.Repository
open Application.UserService
open Infrastructure.DbConnectionHandler
open Mappers
open Microsoft.Extensions.DependencyInjection

type Response<'t> = { Result: 't; Error: string list }

type UserService = {
    userById: Guid -> TryA<User, string list>
    allUsers: unit -> TryA<User list, string list>
    addUser: UserProjection -> TryA<int, string list>
    deleteUser: Guid -> TryA<int, string list>
    updateUser: UserProjection -> TryA<int, string list>
}

type AppApi = {
    getUsers: unit -> Async<Response<UserProjection list>>
    getUserById: Guid -> Async<Response<UserProjection>>
    createUser: UserProjection -> Async<Response<int>>
    updateUser: UserProjection -> Async<Response<int>>
    deleteUserById: Guid -> Async<Response<int>>
}

let createUserService (dbHndl: DbConHandler) =
    let userRepo = {
        byId = userById dbHndl
        all = allUsers dbHndl
        add = createUser dbHndl
        delete = removeUser dbHndl
        update = modifyUser dbHndl
    }

    {
        userById = fun userId -> (getUserById userId) |> Reader.run userRepo
        allUsers = fun () -> (getAllUsers ()) |> Reader.run userRepo
        addUser = fun user -> (addUser user) |> Reader.run userRepo
        deleteUser = fun userId -> (deleteUser userId) |> Reader.run userRepo
        updateUser = fun user -> (updateUser user) |> Reader.run userRepo
    }

let appApi (us: UserService) (ctx: HttpContext) = {
    getUsers =
        fun () -> async {
            let! users = us.allUsers () |> TryA.run

            match users with
            | Ok res ->
                return {
                    Result = res |> List.map toUserPrj
                    Error = []
                }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                return { Result = []; Error = err }
        }
    getUserById = failwith "todo"
    createUser = failwith "todo"
    updateUser = failwith "todo"
    deleteUserById = failwith "todo"
}


let private getAppConfig () =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("Properties/appsettings.json", optional = false, reloadOnChange = true)
        .Build()

let private setConfig (config: IConfigurationRoot) =
    let connstrg = config.GetSection("dbconnection").Value

    match DbConHandler.Create(connstrg) with
    | None -> None
    | Some hndl -> Some hndl

let private webApp (hndl: DbConHandler) =
    Api.make (createUserService hndl |> appApi)

[<EntryPoint>]
let main _ =
    let config = getAppConfig ()

    match setConfig config with
    | None ->
        printfn "Missing or invalid connection string. Shutting down..."
        1
    | Some hndl ->
        let app = application {
            service_config (fun services -> services.AddEndpointsApiExplorer().AddSwaggerGen())
            use_router (webApp hndl)
            memory_cache
            use_gzip
        }

        run app
        0