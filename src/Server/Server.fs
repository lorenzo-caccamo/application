module Server

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open SAFE
open Saturn
open Shared.Monads
open Domain
open Infrastructure.Repository
open Application.UserService
open Infrastructure.DbConnectionHandler
open Shared.Types
open Mappers
open Microsoft.Extensions.DependencyInjection



type UserService = {
    userById: Guid -> TryA<User, string list>
    allUsers: unit -> TryA<User list, string list>
    addUser: UserDto -> TryA<int, string list>
    deleteUser: Guid -> TryA<int, string list>
    updateUser: UserDto -> TryA<int, string list>
}



let createService (dbHndl: DbConHandler) =
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
            let! tryUsers = us.allUsers () |> TryA.run

            match tryUsers with
            | Ok users ->
                return {
                    Result = users |> List.map toUserPrj
                    Error = []
                }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status400BadRequest // the error can be more detailed
                return { Result = []; Error = err }
        }

    getUserById =
        fun id -> async {
            let! tryUser = us.userById id |> TryA.run

            match tryUser with
            | Ok user ->
                return {
                    Result = user |> toUserPrj
                    Error = []
                }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                return { Result = emptyUser; Error = err }
        }

    createUser =
        fun user -> async {
            let! tryInsert = us.addUser user |> TryA.run

            match tryInsert with
            | Ok ins -> return { Result = ins; Error = [] }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                return {Result = 0; Error = err }
        }

    updateUser =
        fun user -> async{
            let! tryUpdate = us.updateUser user |> TryA.run

            match tryUpdate with
            | Ok res -> return { Result = res; Error = [] }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                return {Result = 0; Error = err }
        }

    deleteUserById =
        fun id -> async{
            let! tryDelete = us.deleteUser id |> TryA.run

            match tryDelete with
            | Ok res -> return { Result = res; Error = [] }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                return {Result = 0; Error = err }
        }
}


let private getAppConfig () =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("""Properties/appsettings.json""", optional = false, reloadOnChange = true)
        .Build()

let private setUp (config: IConfigurationRoot) =
    let connstrg = config.GetSection("dbconnection").Value

    match DbConHandler.Create(connstrg) with
    | None -> None
    | Some hndl -> Some hndl

let private webApp (hndl: DbConHandler) = Api.make (createService hndl |> appApi)

[<EntryPoint>]
let main _ =
    let config = getAppConfig ()

    match setUp config with
    | None ->
        printfn "Missing or invalid configuration. Shutting down..."
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
