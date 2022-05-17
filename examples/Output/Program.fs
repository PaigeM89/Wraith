open Wraith
open Wraith.Ansi
open Wraith.Ansi.Operators

//let name = Console.prompt (!! "Enter name: ")

// let msg =
//     !! $"Hello world from F#"
//     |> Message.withColor StandardColor.Red

// Console.writeLine msg

open System
let private width = System.Console.BufferWidth
let private height = System.Console.BufferHeight

printfn $"height x width: %i{height} x %i{width}"
