module DataAccess.DbConnectionHandler

open System.Data
open Microsoft.Data.SqlClient

type DbConHandler =
    val private conn : IDbConnection
    private new(conn:IDbConnection) = {conn = conn}
    member this.Conn = this.conn
    static member Create(conn:string) =
        let dbconn = new SqlConnection (conn)
        match dbconn with
        | null -> None
        | v -> Some(DbConHandler v)