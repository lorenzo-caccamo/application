module Validator


type IValidator<'t, 'err> ={
    validate: 't -> Result<'t, 'err>
}