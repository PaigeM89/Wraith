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
        empty_message "Please enter a valid name"
        loop_on_empty
        execute
    }
    //|> Prompts.execute

clear()

writeLine $"Your name is %s{Format.underline name}"

// type ConsoleState = {
//     Name : string
//     Age : int
// } with
//     static member Default = {
//         Name = ""
//         Age = 0
//     }


// console {
//     let name = Prompts.textPrompt "Enter name: "
//     let test = "hello world"
//     //clear()
//     display test
//     display $"Name is \"%s{Format.underline name}\""
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
