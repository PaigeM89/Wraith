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

        let startBold = $"%c{escape}[01m"
        let endBold = $"%c{escape}[22m"
        let bold text = $"%s{startBold}%s{text}%s{endBold}"

    module Format =
        let underline = EscapedWrites.underline
        let bold = EscapedWrites.bold

    let private runTaskU (t: Task) = t |> Async.AwaitTask |> Async.RunSynchronously
    let private runTask (t : Task<'a>) = t |> Async.AwaitTask |> Async.RunSynchronously

    module Write =
        let private writer = System.Console.Out
        let write (text : string) = writer.WriteAsync text |> runTaskU
        let writeLine (text : string) = writer.WriteAsync text |> runTaskU

    module Read =
        let private reader = System.Console.In
        let readLine() = reader.ReadLine()
        let read() = reader.Read()

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
        type PromptConfig = {
            Prompt: string
            OnError: string option
            LoopOnEmpty: bool
            ClearOnLoop: bool
        } with
            static member Default = {
                Prompt = ""
                OnError = None
                LoopOnEmpty = false
                ClearOnLoop = false
            }

            static member FromPrompt prompt = {
                PromptConfig.Default with Prompt = prompt
            }
            member this.SetPrompt str = { this with Prompt = str }
            member this.SetOnError onErr = { this with OnError = onErr }
            member this.SetLoopOnEmpty b = { this with LoopOnEmpty = b }
            member this.SetClearOnLoop b = { this with ClearOnLoop = b }

        type TextPromptConfig = {
            PromptConfig : PromptConfig
        } with
            static member Default = {
                PromptConfig = PromptConfig.Default
            }

            static member FromPrompt prompt = {
                TextPromptConfig.Default with PromptConfig = PromptConfig.FromPrompt prompt
            }

        let execute (config: PromptConfig) =
            if config.LoopOnEmpty then
                let rec loop input =
                    match input with
                    | Some input ->
                        if String.IsNullOrWhiteSpace input then
                            if config.ClearOnLoop then clear()
                            match config.OnError with
                            | Some errMsg ->
                                Write.writeLine $"{errMsg}\n"
                                Write.writeLine config.Prompt
                                Read.readLine() |> Some |> loop
                            | None ->
                                Write.writeLine config.Prompt
                                Read.readLine() |> Some |> loop
                        else
                            input
                    | None ->
                        Write.write config.Prompt
                        Read.readLine() |> Some |> loop
                loop None
            else
                Write.write config.Prompt
                Read.readLine()

        type TextPromptBuilder() =
            member _.Yield _ = TextPromptConfig.Default
            [<CustomOperation("prompt")>]
            member this.Prompt(state, prompt) = { state with PromptConfig = state.PromptConfig.SetPrompt prompt }
            [<CustomOperation("loop_on_empty")>]
            member this.LoopOnEmpty(state) = { state with PromptConfig = state.PromptConfig.SetLoopOnEmpty true }
            [<CustomOperation("clear_on_loop")>]
            member this.ClearOnLoop(state) = { state with PromptConfig = state.PromptConfig.SetClearOnLoop true }
            [<CustomOperation("on_empty_message")>]
            member this.EmptyMessage(state, errMsg) = { state with PromptConfig = state.PromptConfig.SetOnError (Some errMsg) }
            [<CustomOperation("execute")>]
            member this.Execute (config) = execute config.PromptConfig

        let textPrompter = TextPromptBuilder()

        let textPrompt prompt =
            let conf = TextPromptConfig.FromPrompt prompt
            execute conf.PromptConfig

        type IntPromptConfig = {
            PromptConfig: PromptConfig
            LoopOnInvalidParse : bool
            OnInvalidParse: string option
            DefaultValue: int
        } with
            static member Default = {
                PromptConfig = PromptConfig.Default
                LoopOnInvalidParse = false
                OnInvalidParse = None
                DefaultValue = 0
            }
            static member FromPrompt prompt = {
                PromptConfig = PromptConfig.FromPrompt prompt
                LoopOnInvalidParse = false
                OnInvalidParse = None
                DefaultValue = 0
            }
            member this.Execute() =
                let rec loop() =
                    let str = execute this.PromptConfig
                    match Int32.TryParse str with
                    | true, x -> x
                    | false, _ ->
                        if this.LoopOnInvalidParse then
                            match this.OnInvalidParse with
                            | Some errMsg -> Write.writeLine errMsg
                            | None -> ()
                            loop()
                        else
                            this.DefaultValue
                loop()

        let rec executeIntPrompt (ipc : IntPromptConfig) =
            ipc.Execute()

        type IntPromptBuilder() =
            member _.Yield _ = IntPromptConfig.Default
            [<CustomOperation("prompt")>]
            member this.Prompt(state: IntPromptConfig, prompt) = { state with PromptConfig = state.PromptConfig.SetPrompt prompt }
            [<CustomOperation("loop_on_empty")>]
            member this.LoopOnEmpty(state: IntPromptConfig) = { state with PromptConfig = state.PromptConfig.SetLoopOnEmpty true }
            [<CustomOperation("clear_on_loop")>]
            member this.ClearOnLoop(state: IntPromptConfig) = { state with PromptConfig = state.PromptConfig.SetClearOnLoop true }
            [<CustomOperation("on_empty_message")>]
            member this.EmptyMessage(state: IntPromptConfig, errMsg) = { state with PromptConfig = state.PromptConfig.SetOnError (Some errMsg) }
            [<CustomOperation("loop_on_invalid_int")>]
            member this.LoopOnInvalidInt(state: IntPromptConfig) = { state with LoopOnInvalidParse = true }
            [<CustomOperation("on_invalid_int")>]
            member this.InvalidInt(state: IntPromptConfig, errMsg) = { state with OnInvalidParse = Some errMsg }
            [<CustomOperation("default_value")>]
            member this.DefaultValue(state: IntPromptConfig, dv) = { state with DefaultValue = dv }
            [<CustomOperation("execute")>]
            member this.Execute (config) = executeIntPrompt config

        let intPrompter = IntPromptBuilder()

    type ConsoleBuilder() =
        member _.Yield _ = ""

        member this.Bind(text, func) = text + " " + (func text)

        [<CustomOperation("clear")>]
        member _.Clear (_, ()) =
            System.Console.Clear()

        [<CustomOperation("display")>]
        member _.Display(_, text) =
            writeLine text

    let console = ConsoleBuilder()


