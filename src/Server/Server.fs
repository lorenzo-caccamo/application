module Server

open System
open System.IO
open Application
open Domain.DomainResult
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open SAFE
open Saturn
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Shared.Monads
open Domain
open Infrastructure.Repository
open Application.UserService
open Infrastructure.DbConnectionHandler

type UserProjection = {
    Name: string
    Surname: string
    Email: string
    Role: string
}

type Response<'t> = { Result: 't; Error: string list }

type IUserService = {
    userById: UserId -> Try<DomainResult<User, string list>, exn>
    allUsers: unit -> Try<DomainResult<User list, string list>, exn>
    addUser: User -> Try<DomainResult<int, string>, exn>
    deleteUser: UserId -> Try<DomainResult<int, string>, exn>
    updateUser: User -> Try<DomainResult<int, string>, exn>
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
        delete = Infrastructure.Repository.deleteUser dbHndl
        update = Infrastructure.Repository.updateUser dbHndl
    }

    {
        userById = fun userId -> (getUserById userId) |> Reader.run userRepo
        allUsers = fun () -> (getAllUsers ()) |> Reader.run userRepo
        addUser = fun user -> (addUser user) |> Reader.run userRepo
        deleteUser = fun userId -> (deleteUser userId) |> Reader.run userRepo
        updateUser = fun user -> (updateUser user) |> Reader.run userRepo
    }

let rec private toResponse (prj: UserProjection list) (err: string list) (lst: DomainResult<User, string list> list)   =
    match lst with
    | [] -> { Result = []; Error = [] }
    | t1 :: tl when match t1 with
                    | Successful v -> {
                        Result = v :: toResponse prj err tl
                        Error = err
                        }
                    | Invalid
                    | NotFound err ->{
                        Result = []
                        Error = err
                    }

//  | NotFound err -> {Result = Seq.empty; Error = err}
//                 | Invalid err ->
//                 {Result = Seq.empty; Error = err}
//                 )


let appApi (s: IUserService) (ctx: HttpContext) = {
    getUsers =
        fun () -> async {
            match s.allUsers () |> Try.run with
            | Ok tryUsers ->
                   return match tryUsers with
                          | Successful _ -> { Result = Seq.empty; Error = [] }
                          | NotFound err ->
                            ctx.Response.StatusCode <- StatusCodes.Status404NotFound
                            { Result = Seq.empty; Error = err }
                          | Invalid err ->
                            ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                            { Result = Seq.empty; Error = err }
                          | _ ->
                            ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                            { Result = Seq.empty; Error = ["Unexpected error"] }

            | Error err ->
                ctx.Response.StatusCode <- StatusCodes.Status500InternalServerError
                return {
                    Result = Seq.empty
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
    let maybeDbHndl = DbConHandler.Create(connstrg)

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
