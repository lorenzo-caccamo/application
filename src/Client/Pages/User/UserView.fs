module UserView

open Feliz
open Shared.Types
open UserModel
open Fable.I18Next

let private t = I18n.Translate

let view (model: UserDto) (dispatch: Msg -> unit) =
    Html.div [
        prop.style [ style.marginTop 5 ]
        prop.children [
            Html.label [ prop.text (t "user-name") ]
            Html.input [
                prop.value model.Name
                prop.defaultValue ""
                prop.placeholder (t "user-name-placeholder")
            ]
        ]
    ]
