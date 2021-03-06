module Wraith

open System
open System.Threading.Tasks

[<Literal>]
let private escape = '\u001B'

[<RequireQualifiedAccess>]
module Format =
    let private startUnderline = $"%c{escape}[04m"
    let private endUnderline = $"%c{escape}[24m"
    let underline text = $"%s{startUnderline}%s{text}%s{endUnderline}"

    let private startBold = $"%c{escape}[01m"
    let private endBold = $"%c{escape}[22m"
    let bold text = $"%s{startBold}%s{text}%s{endBold}"

module Color =
    let private startFGBlack = $"%c{escape}[30m"
    let private startFGRed = $"%c{escape}[31m"
    let private startFGGreen = $"%c{escape}[32m"
    let private startFGYellow = $"%c{escape}[33m"
    let private startFGBlue = $"%c{escape}[34m"
    let private startFGMagenta = $"%c{escape}[35m"
    let private startFGCyan = $"%c{escape}[36m"
    let private startFGWhite = $"%c{escape}[37m"
    // 38 is extended - todo
    let private startFGDefault = $"%c{escape}[39m"

    let private wrap color text = $"%s{color}%s{text}%s{startFGDefault}"

    let black text = wrap startFGBlack text
    let red text = wrap startFGRed text
    let green text = wrap startFGGreen text
    let yellow text = wrap startFGYellow text
    let blue text = wrap startFGBlue text
    let magenta text = wrap startFGMagenta text
    let cyan text = wrap startFGCyan text
    let white text = wrap startFGWhite text

    // todo: backgrounds

let private runTaskU (t: Task) = t |> Async.AwaitTask |> Async.RunSynchronously
let private runTask (t : Task<'a>) = t |> Async.AwaitTask |> Async.RunSynchronously

module Write =
    let private writer = System.Console.Out
    let write (text : string) = writer.WriteAsync text |> runTaskU
    let writeLine (text : string) = writer.WriteLineAsync text |> runTaskU

module Read =
    let private reader = System.Console.In
    let readLine() = reader.ReadLine()
    let read() = reader.Read()

let clear() = Console.Clear()

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

        member this.Execute() =
            if this.PromptConfig.LoopOnEmpty then
                let rec loop input =
                    match input with
                    | Some input ->
                        if String.IsNullOrWhiteSpace input then
                            if this.PromptConfig.ClearOnLoop then clear()
                            match this.PromptConfig.OnError with
                            | Some errMsg ->
                                Write.writeLine $"{errMsg}\n"
                                Write.writeLine this.PromptConfig.Prompt
                                Read.readLine() |> Some |> loop
                            | None ->
                                Write.writeLine this.PromptConfig.Prompt
                                Read.readLine() |> Some |> loop
                        else
                            input
                    | None ->
                        Write.write this.PromptConfig.Prompt
                        Read.readLine() |> Some |> loop
                loop None
            else
                Write.write this.PromptConfig.Prompt
                Read.readLine()

    let private execute (config: PromptConfig) =
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

        member this.Run (config : TextPromptConfig) = config.Execute()

    let textPrompter = TextPromptBuilder()

    let executeTextPrompt (tpc : TextPromptConfig) = tpc.Execute()

    let basicTextPrompt prompt =
        let conf = TextPromptConfig.FromPrompt prompt
        conf.Execute()

    type IntPromptConfig = {
        PromptConfig: PromptConfig
        LoopOnInvalidParse : bool
        OnInvalidParse: string option
        LoopOnOutsideRange : (int * int) option
        DefaultValue: int
    } with
        static member Default = {
            PromptConfig = PromptConfig.Default
            LoopOnInvalidParse = false
            OnInvalidParse = None
            LoopOnOutsideRange = None
            DefaultValue = 0
        }
        static member FromPrompt prompt = {
            PromptConfig = PromptConfig.FromPrompt prompt
            LoopOnInvalidParse = false
            OnInvalidParse = None
            LoopOnOutsideRange = None
            DefaultValue = 0
        }
        member this.Execute() =
            let rec loop() =
                let str = execute this.PromptConfig
                match Int32.TryParse str with
                | true, x ->
                    match this.LoopOnOutsideRange with
                    | Some (min, max) ->
                        if x < min || x > max then loop() else x
                    | None -> x
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
        [<CustomOperation("loop_on_outside_range")>]
        member this.LoopOnOutsideRange(state: IntPromptConfig, (min, max) : (int * int)) = { state with LoopOnOutsideRange = Some (min, max) }
        [<CustomOperation("on_invalid_int")>]
        member this.InvalidInt(state: IntPromptConfig, errMsg) = { state with OnInvalidParse = Some errMsg }
        [<CustomOperation("default_value")>]
        member this.DefaultValue(state: IntPromptConfig, dv) = { state with DefaultValue = dv }

        member this.Run (config) = executeIntPrompt config

    let intPrompter = IntPromptBuilder()

module ListPrompts =

    type PagingConfig = {
        PageSize : int
        Page : int
    } with
        static member Default = {
            PageSize = System.Console.WindowHeight
            Page = 0
        }
        member this.NextPage() = { this with Page = this.Page + 1 }
        member this.PreviousPage() = { this with Page = Math.Min(this.Page - 1, 0) }

    let rec executeListPrompt (pageConfig : PagingConfig) title (options: (string * 'a) list) currentIndex =
        Console.Clear()
        let min = pageConfig.Page * pageConfig.PageSize
        let max = (pageConfig.Page + 1) * pageConfig.PageSize
        match title with
        | Some title ->
            Write.writeLine title
        | None -> ()
        if pageConfig.Page > 0 then Write.writeLine "[...]"
        options
        |> List.iteri (fun index (o, _) ->
            if index >= min && index <= max then
                if index = currentIndex then
                    Write.writeLine (" >" + o)
                else
                    Write.writeLine ("  " + o)
        )
        if pageConfig.Page < (List.length options / pageConfig.PageSize) then Write.writeLine "[...]"
        let c = Console.ReadKey(true).Key
        match c with
        | ConsoleKey.UpArrow ->
            let nextIndex = if currentIndex - 1 < 0 then 0 else currentIndex - 1
            let pageConfig = if nextIndex < min then pageConfig.PreviousPage() else pageConfig
            executeListPrompt pageConfig title options nextIndex
        | ConsoleKey.DownArrow ->
            let nextIndex = if currentIndex + 1 >= (List.length options) then currentIndex else currentIndex + 1
            let pageConfig = if nextIndex > max then pageConfig.NextPage() else pageConfig
            executeListPrompt pageConfig title options nextIndex
        // page up & page down don't seem to work? seems to be captured by the terminal instead
        | ConsoleKey.PageDown ->
            let nextIndex = Math.Max(currentIndex + pageConfig.PageSize, List.length options)
            let pageConfig = pageConfig.NextPage()
            executeListPrompt pageConfig title options nextIndex
        | ConsoleKey.PageUp ->
            let nextIndex = Math.Min(currentIndex - pageConfig.PageSize, 0)
            let pageConfig = pageConfig.PreviousPage()
            executeListPrompt pageConfig title options nextIndex
        | ConsoleKey.Enter ->
            options
            |> List.item currentIndex
            |> snd
        | _ -> executeListPrompt pageConfig title options currentIndex

    type ListPromptConfig<'a>  = {
        Title : string option
        Options : (string * 'a) list
        PagingConfig : PagingConfig
    } with
        static member Default = {
            Title = None
            Options = List.empty<string * 'a>
            PagingConfig = PagingConfig.Default
        }

        member this.Execute() =
            let rec loop() = executeListPrompt this.PagingConfig this.Title this.Options 0
            loop()


    type ListPromptBuilder<'a>() =
        member _.Yield _ = ListPromptConfig<'a>.Default
        [<CustomOperation("title")>]
        member this.Title(config : ListPromptConfig<'a>, title) = { config with Title = Some title }
        [<CustomOperation("options")>]
        member this.Options(config : ListPromptConfig<'a>, options) = { config with Options = options }
        [<CustomOperation("page_size")>]
        member this.PageSize(config: ListPromptConfig<'a>, size) = { config with PagingConfig = { config.PagingConfig with PageSize = size } }

        member this.Run(config : ListPromptConfig<'a>) = config.Execute()

    let listPrompter<'a>() = ListPromptBuilder<'a>()

    type NumberedListSelectionError =
    | InvalidIndex of index: int
    | UnparsableIndex of input : string

    type ScrollingConfig = {
        PageSize : int
        MinIndex : int
        MaxIndex : int
    } with
        static member Default = {
            PageSize = System.Console.WindowHeight
            MinIndex = 0
            MaxIndex = System.Console.WindowHeight
        }
        static member FromPageSize ps = {
            PageSize = ps
            MinIndex = 0
            MaxIndex = ps
        }
        member this.Advance() = { this with MinIndex = this.MinIndex + 1; MaxIndex = this.MaxIndex + 1 }
        member this.Backtrack() = { this with MinIndex = Math.Max(0, this.MinIndex - 1); MaxIndex = Math.Max(this.MaxIndex - 1, System.Console.WindowHeight) }

    let rec executeNumberedListPrompt (scrollingConfig : ScrollingConfig) title (options: (string * 'a) list) currentInput  =
        Console.Clear()
        match title with
        | Some title ->
            Write.writeLine title
        | None -> ()
        if scrollingConfig.MinIndex > 0 then Write.writeLine "[...]"
        options
        |> List.iteri (fun index (o, _) ->
            if index >= scrollingConfig.MinIndex && index <= scrollingConfig.MaxIndex then
                Write.writeLine ($" %i{index}. %s{o}")
        )
        if scrollingConfig.MaxIndex < (List.length options) then Write.writeLine "[...]"
        Write.write $"Select an option: %s{currentInput}"
        let c = Console.ReadKey(true).Key
        match c with
        | ConsoleKey.UpArrow ->
            let scrollingConfig = scrollingConfig.Backtrack()
            executeNumberedListPrompt scrollingConfig title options currentInput
        | ConsoleKey.DownArrow ->
            let scrollingConfig = scrollingConfig.Advance()
            executeNumberedListPrompt scrollingConfig title options currentInput
        | ConsoleKey.Enter ->
            if currentInput = "" then
                executeNumberedListPrompt scrollingConfig title options currentInput
            else
                let targetIndex =
                    match Int32.TryParse currentInput with
                    | true, x -> Ok x
                    | false, _ -> UnparsableIndex currentInput |> Error
                match targetIndex with
                | Ok i ->
                    match options |> List.tryItem i with
                    | Some x -> Ok (snd x)
                    | None -> InvalidIndex i |> Error
                | Error e -> Error e
        | ConsoleKey.D0
        | ConsoleKey.NumPad0 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "0")
        | ConsoleKey.D1
        | ConsoleKey.NumPad1 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "1")
        | ConsoleKey.D2
        | ConsoleKey.NumPad2 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "2")
        | ConsoleKey.D3
        | ConsoleKey.NumPad3 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "3")
        | ConsoleKey.D4
        | ConsoleKey.NumPad4 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "4")
        | ConsoleKey.D5
        | ConsoleKey.NumPad5 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "5")
        | ConsoleKey.D6
        | ConsoleKey.NumPad6 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "6")
        | ConsoleKey.D7
        | ConsoleKey.NumPad7 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "7")
        | ConsoleKey.D8
        | ConsoleKey.NumPad8 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "8")
        | ConsoleKey.D9
        | ConsoleKey.NumPad9 ->
            executeNumberedListPrompt scrollingConfig title options (currentInput + "9")
        | _ -> executeNumberedListPrompt scrollingConfig title options currentInput

    type NumberedListPromptConfig<'a> = {
        Config : ListPromptConfig<'a>
        PromptText : string option
        IsZeroBased : bool
        ScrollingConfig : ScrollingConfig
    } with
        static member Default = {
            Config = ListPromptConfig<'a>.Default
            PromptText = None
            IsZeroBased = true
            ScrollingConfig = ScrollingConfig.Default
        }

        member this.Execute() =
            let isZeroBased i = if this.IsZeroBased then i else i + 1
            executeNumberedListPrompt this.ScrollingConfig this.Config.Title this.Config.Options ""

    type NumberedListPromptBuilder<'a>() =
        member _.Yield _ = NumberedListPromptConfig<'a>.Default
        [<CustomOperation("title")>]
        member this.Title(config : NumberedListPromptConfig<'a>, title) = { config with Config = { config.Config with Title = Some title } }
        [<CustomOperation("options")>]
        member this.Options(config : NumberedListPromptConfig<'a>, options) = { config with Config = { config.Config with Options = options } }
        [<CustomOperation("prompt_text")>]
        member this.PromptText(config, prompt) = { config with PromptText = Some prompt }
        [<CustomOperation("is_zero_based")>]
        member this.IsZeroBased(config) = { config with IsZeroBased = true }
        [<CustomOperation("is_one_based")>]
        member this.IsOneBased(config) = { config with IsZeroBased = false }
        [<CustomOperation("page_size")>]
        member this.PageSize(config, pageSize) = { config with ScrollingConfig = ScrollingConfig.FromPageSize pageSize }

        member this.Run(config: NumberedListPromptConfig<'a>) = config.Execute()

    let numberedListPrompter<'a>() = NumberedListPromptBuilder<'a>()

// todo: can this even be done? I want to make a single CE that can wrap a series of console interactions
// type ConsoleBuilder() =
//     member _.Yield _ = ""

//     member this.Bind(text, func) = text + " " + (func text)

//     [<CustomOperation("clear")>]
//     member _.Clear (_, ()) =
//         System.Console.Clear()

//     [<CustomOperation("display")>]
//     member _.Display(_, text) =
//         writeLine text

// let console = ConsoleBuilder()


