open Wraith
open Wraith.Ansi
open Wraith.Ansi.Operators

open System
open System.Threading
open System.Threading.Tasks

let runTaskU (t: Task) = t |> Async.AwaitTask |> Async.RunSynchronously
let runTask (t : Task<'a>) = t |> Async.AwaitTask |> Async.RunSynchronously

// [<Literal>]
// let escape = '\u001B'

let startUnderline = $"%c{escape}[04m"

let endUnderline = $"%c{escape}[24m"

// have to escape, then unescape.
let underline text = $"%s{startUnderline}%s{text}%s{endUnderline}"

let windowHeight = System.Console.WindowHeight
let windowWidth = System.Console.WindowWidth

let reader = System.Console.In
let writer = System.Console.Out

let textPrompt (text : string) =
    writer.WriteAsync text
    |> runTaskU
    reader.ReadLine()

let name = textPrompt "Enter name: "

Console.Clear()

System.Console.WriteLine $"Name is \"%s{underline name}\""

let colorTerm = Environment.GetEnvironmentVariable("COLORTERM")
let term = Environment.GetEnvironmentVariable("TERM")

printfn $"Term, colorterm: %A{term}, %A{colorTerm}"

open Wraith.Console

type ConsoleState = {
    Name : string
    Age : int
} with
    static member Default = {
        Name = ""
        Age = 0
    }


console {
    let name = Prompts.textPrompt "Enter name: "
    clear()
    display $"Name is \"%s{underline name}\""
}
