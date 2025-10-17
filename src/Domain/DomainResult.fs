module Domain.DomainResult

type DomainResult<'a, 'b> =
    | Successful of 'a
    | NotFound of 'b
    | Invalid of 'b
    | FailToCreate of 'b
    | FailToUpdate of 'b
    | FailToDelete of 'b