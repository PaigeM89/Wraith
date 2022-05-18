namespace Wraith

open System

/// <summary> Initial module </summary>
module Say =

    /// <summary> Finite list of Colors </summary>
    type FavoriteColor =
        | Red
        | Yellow
        | Blue

    /// <summary> A person with many different field types </summary>
    type Person =
        { Name: string
          FavoriteNumber: int
          FavoriteColor: FavoriteColor
          DateOfBirth: DateTimeOffset }

    /// <summary>Says hello to a specific person</summary>
    let helloPerson (person: Person) =
        sprintf
            "Hello %s. You were born on %s and your favorite number is %d. You like %A."
            person.Name
            (person.DateOfBirth.ToString("yyyy/MM/dd"))
            person.FavoriteNumber
            person.FavoriteColor

    /// <summary>
    /// Adds two integers <paramref name="a"/> and <paramref name="b"/> and returns the result.
    /// </summary>
    ///
    /// <remarks>
    /// This usually contains some really important information that you'll miss if you don't read the docs.
    /// </remarks>
    ///
    /// <param name="a">An integer.</param>
    /// <param name="b">An integer.</param>
    ///
    /// <returns>
    /// The sum of two integers.
    /// </returns>
    ///
    /// <exceptions cref="M:System.OverflowException">Thrown when one parameter is max
    /// and the other is greater than 0.</exceptions>
    let add a b = a + b


    /// I do nothing
    let nothing name = name |> ignore

module Ansi =

    [<Literal>]
    let escape = '\u001B'

    [<RequireQualifiedAccess>]
    type Style =
    | Bold
    | Italics
    | Underline
    | Centered

    [<RequireQualifiedAccess>]
    type StandardColor =
    | Black
    | DarkRed
    | DarkGreen
    | DarkYellow
    | DarkBlue
    | DarkMagenta
    | DarkCyan
    | Gray
    | DarkGray
    | Red
    | Green
    | Yellow
    | Blue
    | Magenta
    | Cyan
    | White

    // this actually isn't useful because it's hard to capture a concept where only part of the text
    // has special formatting
    // this would require each layer be a list, which gets unwieldy, both to implement and to actually use
    type Message =
    | Text of msg: string
    | ColoredMessage of msg: Message * textColor : StandardColor
    | StyledMessage of msg : Message * style : Style

    module Message =
        let text t = Text t

        let withColor color msg = ColoredMessage(msg, color)
        let withStyle style msg = StyledMessage(msg, style)

        let underline msg = StyledMessage(msg, Style.Underline)

    module Operators =
        let (!!) t = Text t

module Console =
    open Ansi
    open System.Threading.Tasks

    [<RequireQualifiedAccess>]
    module private EscapedWrites =
        let startUnderline = $"%c{escape}[04m"
        let endUnderline = $"%c{escape}[24m"

        let underline text = $"%s{startUnderline}%s{text}%s{endUnderline}"

    module Format =
        let underline = EscapedWrites.underline

    let runTaskU (t: Task) = t |> Async.AwaitTask |> Async.RunSynchronously
    let runTask (t : Task<'a>) = t |> Async.AwaitTask |> Async.RunSynchronously

    let reader = System.Console.In
    let writer = System.Console.Out

    let textPrompt (text : string) =
        writer.WriteAsync text
        |> runTaskU
        reader.ReadLine()

    let clear() = Console.Clear()
    let writeMessage (msg : Message) =
        let rec traverse rem prefix postfix =
            match rem with
            | Text t -> $"%s{prefix}%s{t}%s{postfix}"
            // ignore colors for now
            | ColoredMessage (msg, color) -> traverse msg prefix postfix
            | StyledMessage(msg, style) ->
                match style with
                | Style.Underline ->
                    traverse msg (prefix + EscapedWrites.startUnderline) (EscapedWrites.endUnderline + postfix)
                | _ -> traverse msg prefix postfix
        let escapedString = traverse msg "" ""
        Console.WriteLine escapedString
    let writeLine (text: string) = Console.WriteLine text

    module Prompts =
        let textPrompt (text : string) =
            writer.WriteAsync text
            |> runTaskU
            reader.ReadLine()

    type ConsoleBuilder() =
        member _.Yield _ = ""

        member this.Bind(state, func) = func state

        [<CustomOperation("clear")>]
        member _.Clear (_, ()) =
            System.Console.Clear()

        [<CustomOperation("display")>]
        member _.Display((), text) =
            writeLine text
            ()

    let console = ConsoleBuilder()
