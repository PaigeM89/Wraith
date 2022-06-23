open Wraith

open System

// let colorTerm = Environment.GetEnvironmentVariable("COLORTERM")
// let term = Environment.GetEnvironmentVariable("TERM")

// printfn $"Term, colorterm: %A{term}, %A{colorTerm}"

// let name =
//     Prompts.textPrompter {
//         prompt "Enter name: "
//         on_empty_message "Please enter a valid name"
//         loop_on_empty
//         clear_on_loop
//         execute
//     }

// let name2 =
//     let config = Prompts.textPrompter {
//         prompt "Enter name again: "
//         on_empty_message "Please actually enter a name"
//         loop_on_empty
//         clear_on_loop
//     }
//     config |> Prompts.executeTextPrompt

// printfn $"Your name is either {Format.underline name} or {Format.underline name2}."


// let age =
//     Prompts.intPrompter {
//         prompt "Enter age: "
//         on_empty_message "Please enter your age"
//         loop_on_empty
//         loop_on_invalid_int
//         on_invalid_int "You did not enter an integer for your age"
//         execute
//     }

// let iq =
//     Prompts.intPrompter {
//         prompt "Please enter your IQ: "
//         default_value 10
//         execute
//     }

// clear()

// writeLine $"Your name is %s{(Format.underline >> Format.bold) name} and you are %s{Format.underline (string age)} years old with an IQ of %i{iq}"

let redName = Color.red "red"
let blueName = Color.blue "blue"
let greenName = Color.green "green"

let listTitle = "Pick a color"
let listOptions = [
    (Color.red "Red"), $"You picked %s{redName}! Nice pick!"
    (Color.blue "Blue"), $"You picked %s{blueName}!"
    (Color.green "Green"), $"You picked %s{greenName}!"
]

// let selected = ListPrompts.listPrompter<string>() {
//     title listTitle
//     options listOptions
//     execute
// }
// printfn "%s" selected

let numberedList : ListPrompts.NumberedListPromptConfig<string> = {
    Config = {
        Title = Some "Numbered list prompt"
        Options = listOptions
    }
    PromptText = Some "Please select an option: "
    IsZeroBased = false
}

let selected = ListPrompts.numberedListPrompter<string>() {
    title listTitle
    options listOptions
    prompt_text "Color: "
    is_one_based
    execute
}

printfn "%s" selected


// type ConsoleState = {
//     Name : string
//     Age : int
// } with
//     static member Default = {
//         Name = ""
//         Age = 0
//     }


// console {
//     let! name = Prompts.textPrompt "Enter name: "
//     let! text = "hello world"
//     let! text2 = $"Name is \"%s{Format.underline name}\"\n{text}"
//     //clear()
//     //display
//     display text2
// }

// type TestBuilder() =
//     member _.Yield _ = ""
//     [<CustomOperation("print")>]
//     member _.Print(text) = printfn "%s" text
//     member _.Bind(text, f) = text + " " + (f text)

// let test = TestBuilder()

// test {
//     let! innerVariable = "hello world"
//     print // the value 'variable' is not defined
// }
