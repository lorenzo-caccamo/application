module Infrastructure.DbConnectionHandler

open System.Data
open FSharpPlus.Control
open Microsoft.Data.SqlClient
open Shared.Monads

type DbConHandler =
    val private conn : string
    private new(conn:string) = {conn = conn}
    member this.Conn = this.conn
    static member Create(conn:string) =
        try
            use dbconn = new SqlConnection (conn)
            dbconn.Open()
            Some(DbConHandler conn)
        with
        | _ -> None


    member this.GetConnection() = new SqlConnection (this.conn)