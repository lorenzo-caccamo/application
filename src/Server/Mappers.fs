module Mappers

open Domain
open Shared.Types

let toUserPrj (u: User) : UserProjection =
    match u with
    | Admin a ->
        let (Id id) = a.Id
        {
        Id = id
        Name = a.Data.FirstName.Value
        Surname = a.Data.LastName.Value
        Email = a.Data.Email.Value
        Role = Administrator
      }
    | Normal n ->
        let (Id id) = n.Id
        {
        Id = id
        Name = n.Data.FirstName.Value
        Surname = n.Data.LastName.Value
        Email = n.Data.Email.Value
        Role = NormalUser
      }
    | ReadOnly r ->
        let (Id id) = r.Id
        {
        Id = id
        Name = r.Data.FirstName.Value
        Surname = r.Data.LastName.Value
        Email = r.Data.Email.Value
        Role = Readonly
      }