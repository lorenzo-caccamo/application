module Server

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open SAFE
open Saturn
open Shared.Monads
open Domain
open Domain.UserProjection
open Infrastructure.Repository
open Application.UserService
open Infrastructure.DbConnectionHandler

type Response<'t> = { Result: 't; Error: string list }

type UserService = {
    userById: Guid -> Async<Try<Result<User, string list>, exn>>
    allUsers: unit -> Async<Try<Result<User list, string list>, exn>>
    addUser: UserProjection -> Async<Try<Result<int, string list>, exn>>
    deleteUser: Guid -> Async<Try<Result<int, string list>, exn>>
    updateUser: UserProjection -> Async<Try<Result<int, string list>, exn>>
}

type IAppApi = {
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
            let! tryUsers = us.allUsers ()
            match tryUsers |> Try.run with
            | Ok res ->
                match res with
                | Ok usrLst ->
                    ctx.Response.StatusCode <- StatusCodes.Status200OK
                    return {
                        Result = usrLst //map to userprojection
                        Error = []
                    }
                | Error err ->
                    ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                    return { Result = []; Error = err }
            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status500InternalServerError
                return {
                    Result = []
                    Error = [ $"Failed to retrieve all users. Internal messase: {err.InnerException}" ]
                }
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
    let maybeDbHndl = DbConHandler.Validate(connstrg)

    match maybeDbHndl with
    | None -> None
    | Some v -> Some v

let webApp = Api.make (createUserService (getAppConfig >> setConfig) |> appApi)

let app = application {
    service_config (fun services ->
        services.AddSingleton<IHostedService>(fun sp ->
            let lifetime = sp.GetRequiredService<IHostApplicationLifetime>()
            let conf = (getAppConfig >> setConfig) ()

            match conf with
            | None ->
                printfn "Missing or invalid connection string. Shutting down..."
                lifetime.StopApplication()
            | Some v -> v |> ignore

            null // no hosted service needed
        ))

    use_router webApp
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0
