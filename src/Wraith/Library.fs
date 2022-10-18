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

[<RequireQualifiedAccess>]
module Align =
  let private diff (text: string) = System.Console.BufferWidth - (text.Length)

  let left text =
    let padding = String.replicate (diff text) " "
    text + padding
  
  let right text =
    let padding = String.replicate (diff text) " "
    padding + text

  let center text =
    let d = diff text
    let leftPad = String.replicate (d/2) " "
    let rightPad = String.replicate (d/2) " "
    leftPad + text + rightPad

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

        member this.Execute() =
          if this.LoopOnEmpty then
            let rec loop input =
                match input with
                | Some input ->
                    if String.IsNullOrWhiteSpace input then
                        if this.ClearOnLoop then clear()
                        match this.OnError with
                        | Some errMsg ->
                            Write.writeLine $"{errMsg}\n"
                            Write.writeLine this.Prompt
                            Read.readLine() |> Some |> loop
                        | None ->
                            Write.writeLine this.Prompt
                            Read.readLine() |> Some |> loop
                    else
                        input
                | None ->
                    Write.write this.Prompt
                    Read.readLine() |> Some |> loop
            loop None
          else
              Write.write this.Prompt
              Read.readLine()

    let private execute (config: PromptConfig) = config.Execute()

    type TextPromptConfig = {
        PromptConfig : PromptConfig
    } with
        static member Default = {
            PromptConfig = PromptConfig.Default
        }

        static member FromPrompt prompt = {
            TextPromptConfig.Default with PromptConfig = PromptConfig.FromPrompt prompt
        }

        member this.Execute() = this.PromptConfig.Execute()

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

    type PagingType =
    | SmoothScroll of pageSize: int * minIndex : int * maxIndex : int
    | JumpScroll of pageSize : int * pageNumber : int
    with
      member this.MaxPages optionCount =
        match this with
        | SmoothScroll(pageSize, _, _) -> Math.Ceiling((float) optionCount / (float) pageSize) |> (int)
        | JumpScroll(pageSize, _) -> Math.Ceiling((float) optionCount / (float) pageSize)  |> (int)

      member this.Previous() =
        match this with
        | SmoothScroll(size, min, max) ->
          SmoothScroll(size, Math.Max(min, 0), Math.Max(max - 1, 0))
        | JumpScroll(size, pageNumber) ->
          JumpScroll(size, Math.Max(pageNumber - 1, 0))

      member this.Next() =
        match this with
        | SmoothScroll(size, min, max) ->
          SmoothScroll(size, min + 1, max + 1)
        | JumpScroll(size, pageNumber) ->
          JumpScroll(size, pageNumber + 1)

      member this.HasPageBefore() =
        match this with
        | SmoothScroll(size, min, _) -> min > 0
        | JumpScroll(_, pageNumber) -> pageNumber > 0

      member this.HasPageAfter optionCount =
        match this with
        | SmoothScroll(_, _, max) -> max < optionCount
        | JumpScroll(size, pageNumber) ->
          (pageNumber + 1) < (this.MaxPages optionCount)

    // subtract 2 to leave room for the "[...]" if needed
    let defaultJumpScroll = JumpScroll(System.Console.WindowHeight - 2, 0)
    let defaultSmoothScroll = SmoothScroll(System.Console.WindowHeight - 2, 0, System.Console.WindowHeight - 2)

    let createJumpScroll pageSize = JumpScroll(pageSize, 0)
    let createSmoothScroll pageSize = SmoothScroll(pageSize, 0, pageSize)

    let rec executeListPrompt (paging: PagingType) title (options: (string * 'a) list) hoverIndex =
      Console.Clear()

      let  min, max =
        match paging with
        | SmoothScroll(size, min, max) -> 
          min, max
        | JumpScroll(size, pageNumber) ->
          pageNumber * size, (pageNumber+1) * size
      
      match title with
        | Some title ->
            Write.writeLine title
        | None -> ()
      if paging.HasPageBefore() then Write.writeLine "[...]"
      options
      |> List.iteri (fun index (o, _) ->
          if index >= min && index < max then
              if index = hoverIndex then
                  Write.writeLine (" >" + o)
              else
                  Write.writeLine ("  " + o)
      )
      if paging.HasPageAfter (List.length options) then Write.writeLine "[...]"
      let c = Console.ReadKey(true).Key
      match c with
      | ConsoleKey.UpArrow ->
          let nextIndex = if hoverIndex - 1 < 0 then 0 else hoverIndex - 1
          let pageConfig = if nextIndex < min then paging.Previous() else paging
          executeListPrompt pageConfig title options nextIndex
      | ConsoleKey.DownArrow ->
          let nextIndex = if hoverIndex + 1 >= (List.length options) then hoverIndex else hoverIndex + 1
          let pageConfig = if nextIndex >= max then paging.Next() else paging
          executeListPrompt pageConfig title options nextIndex
      | ConsoleKey.Enter ->
          options
          |> List.item hoverIndex
          |> snd
      | _ -> executeListPrompt paging title options hoverIndex

    type ListPromptConfig<'a>  = {
        Title : string option
        Options : (string * 'a) list
        PagingConfig : PagingType
    } with
        static member Default = {
            Title = None
            Options = List.empty<string * 'a>
            PagingConfig = defaultJumpScroll
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
        [<CustomOperation("paging")>]
        member this.PageSize(config: ListPromptConfig<'a>, pagingConfig) = { config with PagingConfig = pagingConfig }

        member this.Run(config : ListPromptConfig<'a>) = config.Execute()

    let listPrompter<'a>() = ListPromptBuilder<'a>()

    type NumberedListSelectionError =
    | InvalidIndex of index: int
    | UnparsableIndex of input : string

    let rec executeNumberedListPrompt (paging : PagingType) title (options: (string * 'a) list) currentInput  =
        Console.Clear()
        match title with
        | Some title ->
            Write.writeLine title
        | None -> ()
        //if scrollingConfig.MinIndex > 0 then Write.writeLine "[...]"
        if paging.HasPageBefore() then Write.writeLine "[...]"
        
        let min, max, isPastFirstPage, isBeforeLastPage =
          match paging with
          | SmoothScroll(size, min, max) -> 
            let isPastFirstPage = min > size
            let isBeforeLastPage = max < (List.length options - size)
            min, max, isPastFirstPage, isBeforeLastPage
          | JumpScroll(size, pageNumber) ->
            pageNumber * size, pageNumber * size + 1, pageNumber > 0, pageNumber < (List.length options) / size

        options
        |> List.iteri (fun index (o, _) ->
            if index >= min && index <= max then
                Write.writeLine ($" %i{index}. %s{o}")
        )
        if paging.HasPageAfter (List.length options) then Write.writeLine "[...]"
        Write.write $"Select an option: %s{currentInput}"
        let c = Console.ReadKey(true).Key
        
        let padInput =
          executeNumberedListPrompt paging title options

        match c with
        | ConsoleKey.UpArrow ->
            let paging = paging.Previous()//scrollingConfig.Backtrack()
            executeNumberedListPrompt paging title options currentInput
        | ConsoleKey.DownArrow ->
            //let scrollingConfig = scrollingConfig.Advance()
            let paging = paging.Next()
            executeNumberedListPrompt paging title options currentInput
        | ConsoleKey.Enter ->
            if currentInput = "" then
                executeNumberedListPrompt paging title options currentInput
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
        | ConsoleKey.NumPad0 -> padInput (currentInput + "0")
        | ConsoleKey.D1
        | ConsoleKey.NumPad1 -> padInput (currentInput + "1")
        | ConsoleKey.D2
        | ConsoleKey.NumPad2 -> padInput (currentInput + "2")
        | ConsoleKey.D3
        | ConsoleKey.NumPad3 -> padInput (currentInput + "3")
        | ConsoleKey.D4
        | ConsoleKey.NumPad4 -> padInput (currentInput + "4")
        | ConsoleKey.D5
        | ConsoleKey.NumPad5 -> padInput (currentInput + "5")
        | ConsoleKey.D6
        | ConsoleKey.NumPad6 -> padInput (currentInput + "6")
        | ConsoleKey.D7
        | ConsoleKey.NumPad7 -> padInput (currentInput + "7")
        | ConsoleKey.D8
        | ConsoleKey.NumPad8 -> padInput (currentInput + "8")
        | ConsoleKey.D9
        | ConsoleKey.NumPad9 -> padInput (currentInput + "9")
        | ConsoleKey.Backspace -> padInput (currentInput.Substring(0, currentInput.Length - 2))
        | _ -> padInput currentInput

    type NumberedListPromptConfig<'a> = {
        Config : ListPromptConfig<'a>
        PromptText : string option
        IsZeroBased : bool
        Paging : PagingType
    } with
        static member Default = {
            Config = ListPromptConfig<'a>.Default
            PromptText = None
            IsZeroBased = true
            Paging = defaultJumpScroll
        }

        member this.Execute() =
            let isZeroBased i = if this.IsZeroBased then i else i + 1
            executeNumberedListPrompt this.Paging this.Config.Title this.Config.Options ""

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
        [<CustomOperation("paging")>]
        member this.PageSize(config, paging) = { config with Paging = paging }

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


