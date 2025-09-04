namespace Application

open System
open Shared.Monads
open Domain
open Validator

type IRepository<'r> = {
    byId: Uuid -> Try<'r, exn>
    all: unit -> Try<'r list, exn>
    create: 'r -> Try<'r, exn>
    delete: Uuid -> Try<unit, exn>
    update: 'r -> Try<'r, exn>
}

type IUserService = {
    Repository: IRepository<User>
    Validator: IValidator<User, DomainError<string>>
}
