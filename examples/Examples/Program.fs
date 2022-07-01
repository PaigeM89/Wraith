open Wraith

let nameStr = Format.underline "name"
let namePrompt() =
    Prompts.textPrompter {
        prompt $"Enter %s{nameStr}: "
        // on empty input, re-ask for the name
        loop_on_empty
        // display this message when the input is empty
        on_empty_message "Please enter a valid name"
        // clear display (so the prompt doesn't appear multiple times) on loop
        clear_on_loop
        // execute this builder and return a string
        execute
    }

let ageStr = Format.bold "age"

let agePrompt() =
    Prompts.intPrompter {
        prompt "Enter age: "
        loop_on_empty
        on_empty_message $"Please enter your %s{ageStr}: "
        // rerun the prompt if the input is not a number
        loop_on_invalid_int
        // Message to print if the input is not a number
        on_invalid_int "You did not enter an integer for your age"
        // execute this builder and return an int
        // If the "loop" values are not set, the default value is 0
        // or `default_value` can be used to set the default value for the prompt
        execute
    }

let red = Color.red "red"
let blue = Color.blue "blue"
let green = Color.green "green"

let colorListTitle = "Pick a " + (Format.bold "color")
let colorListOptions = [
    red, $"You picked %s{red}! Nice pick!"
    blue, $"You picked %s{blue}!"
    green, $"You picked %s{green}!"
]

let selectedColorPrompt() =
    // note that, because these are generics, they need to be constructed via `()`
    ListPrompts.listPrompter<string>() {
        title colorListTitle
        options colorListOptions
        execute
    }

type State = {
    Name : string
    Age : int
    Color : string
    Exit : bool
} with
    static member Default = {
        Name = ""
        Age = 0
        Color = ""
        Exit = false
    }

let mainMenuTitle = (Format.bold >> Format.underline) "Main menu"
let mainMenuOptions = [
    "Enter name", (fun (state : State) -> { state with Name = namePrompt() } )
    "Enter age", (fun (state : State) -> { state with Age = agePrompt() } )
    "Pick a color", (fun (state : State) -> { state with Color = selectedColorPrompt() } )
    "Exit", (fun state -> { state with Exit = true } )
]

let mainMenu = ListPrompts.listPrompter<State -> State>() {
    title mainMenuTitle
    options mainMenuOptions
    execute
}

let rec menu state =
    printfn "state before menu is %A" state
    let state = mainMenu state
    printfn "State is %A" state
    match state.Exit with
    | true -> state
    | false -> menu state

let finalState = menu (State.Default)
printfn "Final state is %A" finalState
