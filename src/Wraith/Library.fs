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

    [<RequireQualifiedAccess>]
    type Style =
    | Bold
    | Italics

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

    type Message =
    | Text of msg: string
    | ColoredMessage of msg: Message * textColor : StandardColor
    | StyledMessage of msg : Message * style : Style

    module Message =
        let text t = Text t

        let withColor color msg = ColoredMessage(msg, color)
        let withStyle style msg = StyledMessage(msg, style)

    module Operators =
        let (!!) t = Text t

module Console =
    open Ansi

    let private width = System.Console.BufferWidth
    let private height = System.Console.BufferHeight

    let s = Console.OpenStandardOutput()
    let tw = new IO.StringWriter()

    let writeLine (msg : Message) =
        let rec traverse m =
            match m with
            | Text t ->
                //Console.WriteLine t
                let sb = tw.GetStringBuilder()
                sb.Append t |> ignore
                tw.WriteLine()
                tw.Flush()
            | ColoredMessage (m, c) ->
                Console.ForegroundColor <- (ConsoleColor.Red)
                traverse m
            | StyledMessage (m, s) ->
                traverse m
        traverse msg

    let write (msg : Message) =
        let rec traverse m =
            match m with
            | Text t ->
                Console.Write t
            | ColoredMessage (m, c) ->
                Console.ForegroundColor <- (ConsoleColor.Red)
                traverse m
            | StyledMessage (m, s) ->
                traverse m
        traverse msg

    let private readLine() = Console.ReadLine()

    let prompt msg =
        write msg
        readLine()
