module Shared.Helper

open Microsoft.FSharp.Reflection

let getTaggedTypeName (x: 'a) =
    let case, _ = FSharpValue.GetUnionFields(x, typeof<'a>)
    case.Name