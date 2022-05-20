open Wraith
open Wraith.Ansi
open Wraith.Ansi.Operators

open System

let colorTerm = Environment.GetEnvironmentVariable("COLORTERM")
let term = Environment.GetEnvironmentVariable("TERM")

printfn $"Term, colorterm: %A{term}, %A{colorTerm}"

open Wraith.Console

let name =
    Prompts.textPrompter {
        prompt "Enter name: "
        on_empty_message "Please enter a valid name"
        loop_on_empty
        clear_on_loop
        execute
    }

let age =
    Prompts.intPrompter {
        prompt "Enter age: "
        on_empty_message "Please enter your age"
        loop_on_empty
        loop_on_invalid_int
        on_invalid_int "You did not enter an integer for your age"
        execute
    }

let iq =
    Prompts.intPrompter {
        prompt "Please enter your IQ: "
        default_value 10
        execute
    }

clear()

writeLine $"Your name is %s{(Format.underline >> Format.bold) name} and you are %s{Format.underline (string age)} years old with an IQ of %i{iq}"

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
