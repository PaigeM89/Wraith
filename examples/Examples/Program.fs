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
        // If the "loop" values are not set, the default value is 0
        // or `default_value` can be used to set the default value for the prompt
    }

module FavoriteColorPrompt =

  let red = Color.red "red"
  let blue = Color.blue "blue"
  let green = Color.green "green"

  // Note that string interpolation gets weird here, since you can't do
  // $"text {Format.bold "text"}" - the quotes will confuse it, and you can't escape them.
  // This is a valid argument in favor of using a markup parser. Contributions welcome!
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
      }

module PagingListPrompt =
  let numberListOptions = [1..30] |> List.map (fun i -> $"Option #{i}", $"Option #{i}")
  let numberListTitle = "Pick a number"

  let selectNumberPromptJumpScroll() =
    ListPrompts.listPrompter<string>() {
      title numberListTitle
      options numberListOptions
      paging (ListPrompts.createJumpScroll 10)
    }

  let selectNumberPromptSmoothScroll() =
    ListPrompts.listPrompter<string>() {
      title numberListTitle
      options numberListOptions
      paging (ListPrompts.createSmoothScroll 10)
    }

  let selectScrollMode() =
    ListPrompts.listPrompter<bool>() {
      title "Select scroll mode"
      options ["Jump", false; "Smooth", true]
    }


// state to store user inputs
type State = {
    Name : string
    Age : int
    Color : string
    UseSmoothScroll : bool
    NumberSelected : string
    Exit : bool
} with
    static member Default = {
        Name = ""
        Age = 0
        Color = ""
        UseSmoothScroll = false
        NumberSelected = ""
        Exit = false
    }

let printState (state : State) =
    clear()
    Write.writeLine <| sprintf "%A" state
    Write.writeLine "Press Enter to continue"
    // wait for the user to hit "enter"
    let _ = Read.read()
    state // return the unmodified state

let mainMenuTitle = (Format.bold >> Format.underline >> Align.center) "Main menu"
let mainMenuOptions = [
    "Enter name", (fun (state : State) -> { state with Name = namePrompt() } )
    "Enter age", (fun (state : State) -> { state with Age = agePrompt() } )
    "Pick a color", (fun (state : State) -> { state with Color = FavoriteColorPrompt.selectedColorPrompt() } )
    "Pick a number", (fun (state : State) ->
      if state.UseSmoothScroll then
        { state with NumberSelected = PagingListPrompt.selectNumberPromptSmoothScroll() } 
      else
        { state with NumberSelected = PagingListPrompt.selectNumberPromptJumpScroll() } 
    )
    "Select scroll mode", (fun (state: State) -> { state with UseSmoothScroll = PagingListPrompt.selectScrollMode()})
    "Print state", (fun (state : State) -> printState state)
    "Exit", (fun state -> { state with Exit = true } )
]

// takes the current state, lets the user select a function, applies that function, and returns the new state
let mainMenu state =
    let f = // select a function from the menu
        ListPrompts.listPrompter<State -> State>() {
            title mainMenuTitle
            options mainMenuOptions
        }
    f state // execute that function

// recurse through the menu until the user exits
let rec menu state =
    let state = mainMenu state
    match state.Exit with
    | true -> state
    | false -> menu state

let finalState = menu State.Default
printfn "Final state is %A" finalState
