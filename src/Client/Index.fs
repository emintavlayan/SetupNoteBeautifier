module Index

open System
open Elmish
open Feliz
open Fable.Core
open Shared

type CopyStatus =
    | NotCopied
    | CopySucceeded
    | CopyFailed

type CounterState =
    | CounterNormal
    | CounterWarning
    | CounterError

type Model =
    { RawText: string
      ProcessingLevel: ProcessingLevel
      Output: string
      CharacterCount: int
      CopyStatus: CopyStatus }

type Msg =
    | RawTextChanged of string
    | OutputTextChanged of string
    | SetProcessingLevel of int
    | LoadSample
    | CopyOutput
    | OutputCopied
    | OutputCopyFailed
    | ClearInput

/// Writes text to browser clipboard via JavaScript navigator API.
[<Emit("globalThis.navigator.clipboard.writeText($0)")>]
let writeClipboard (text: string) : JS.Promise<unit> = jsNative

/// Provides representative sample setup note text for the load sample action.
let sampleSetupNote =
    String.concat
        "\n"
        [ "Template:"
          "Breast"
          ""
          "Patient Orientation:"
          "Head First Supine"
          ""
          "General"
          "Deep inspiration breathhold: ..........................."
          "Yes"
          ""
          "Breast board"
          "Kile: L2"
          "Bas: B+20"
          "Opstilling long:"
          "Mellem"
          ""
          "Knæpude"
          "Knæpude: På bryst"
          ""
          "Gatingboks"
          "Gatingboks: Yes"
          "Unknown note: should be ignored"
          "//////" ]

let processingLevels =
    [ ProcessingLevel.Raw, "Raw"
      ProcessingLevel.CleanLines, "Clean lines"
      ProcessingLevel.RemoveVisualNoise, "Remove visual noise"
      ProcessingLevel.ParseKnownFields, "Parse known setup fields only"
      ProcessingLevel.ShortenSafeValues, "Shorten safe values"
      ProcessingLevel.ShortenSafeKeys, "Shorten safe keys"
      ProcessingLevel.CompactFinal, "Compact final output" ]

let processingLevelFromInt (value: int) =
    let clamped = max 0 (min 6 value)
    enum<ProcessingLevel> clamped

let processingLevelLabel (level: ProcessingLevel) =
    processingLevels
    |> List.find (fun (candidate, _) -> candidate = level)
    |> snd

/// Applies the selected processing level and returns output text with character count.
let applyProcessing (level: ProcessingLevel) (rawText: string) =
    let result = SetupNoteBeautifier.trim level rawText
    result.Output, result.CharacterCount

/// Recomputes output and character count after model changes.
let evaluateModel (model: Model) =
    let output, characterCount = applyProcessing model.ProcessingLevel model.RawText

    { model with
        Output = output
        CharacterCount = characterCount }

/// Returns counter visual state from output character count.
let counterState (count: int) =
    if count > SetupNoteBeautifier.hardLimit then
        CounterError
    elif count > SetupNoteBeautifier.warningLimit then
        CounterWarning
    else
        CounterNormal

/// Returns copy status text shown under action buttons.
let copyStatusText (status: CopyStatus) =
    match status with
    | NotCopied -> ""
    | CopySucceeded -> "Copied to clipboard."
    | CopyFailed -> "Copy failed. Please copy manually."

/// Builds counter class name based on warning and hard-limit state.
let counterClassName (count: int) =
    match counterState count with
    | CounterNormal -> "counter counter-normal"
    | CounterWarning -> "counter counter-warning"
    | CounterError -> "counter counter-error"

/// Creates a command that writes output text to the clipboard.
let copyOutputCmd (text: string) =
    Cmd.OfPromise.either
        writeClipboard
        text
        (fun _ -> OutputCopied)
        (fun _ -> OutputCopyFailed)

/// Creates the initial model and command for the trimmer page.
let init () =
    let baseModel =
        { RawText = ""
          ProcessingLevel = SetupNoteBeautifier.defaultProcessingLevel
          Output = ""
          CharacterCount = 0
          CopyStatus = NotCopied }

    evaluateModel baseModel, Cmd.none

/// Handles all Elmish messages for local trimmer state updates.
let update msg model =
    match msg with
    | RawTextChanged value ->
        { model with
            RawText = value
            CopyStatus = NotCopied }
        |> evaluateModel,
        Cmd.none
    | OutputTextChanged value ->
        { model with
            Output = value
            CharacterCount = value.Length
            CopyStatus = NotCopied },
        Cmd.none
    | SetProcessingLevel value ->
        { model with
            ProcessingLevel = processingLevelFromInt value
            CopyStatus = NotCopied }
        |> evaluateModel,
        Cmd.none
    | LoadSample ->
        { model with
            RawText = sampleSetupNote
            CopyStatus = NotCopied }
        |> evaluateModel,
        Cmd.none
    | CopyOutput -> model, copyOutputCmd model.Output
    | OutputCopied -> { model with CopyStatus = CopySucceeded }, Cmd.none
    | OutputCopyFailed -> { model with CopyStatus = CopyFailed }, Cmd.none
    | ClearInput ->
        { model with
            RawText = ""
            CopyStatus = NotCopied }
        |> evaluateModel,
        Cmd.none

/// Renders contextual output length warning messages.
let lengthMessages (model: Model) =
    let messages =
        [ if model.CharacterCount > SetupNoteBeautifier.warningLimit
             && int model.ProcessingLevel < int ProcessingLevel.ShortenSafeKeys then
              Html.p [
                  prop.className "message message-warning"
                  prop.text "Output is long. Increase processing level if the extra shortening is clinically safe."
              ]
          if model.CharacterCount > SetupNoteBeautifier.hardLimit then
              Html.p [
                  prop.className "message message-error"
                  prop.text "Output is above system limit."
              ] ]

    Html.div [ prop.children messages ]

let processingSlider model dispatch =
    Html.section [
        prop.className "processing-panel"
        prop.children [
            Html.div [
                prop.className "processing-header"
                prop.children [
                    Html.h2 [
                        prop.className "pane-title"
                        prop.text "Processing level"
                    ]
                    Html.p [
                        prop.className "processing-current"
                        prop.text (processingLevelLabel model.ProcessingLevel)
                    ]
                ]
            ]
            Html.input [
                prop.className "processing-slider"
                prop.type'.range
                prop.min 0
                prop.max 6
                prop.step 1
                prop.value (int model.ProcessingLevel)
                prop.onChange (fun (value: string) -> value |> int |> SetProcessingLevel |> dispatch)
            ]
            Html.div [
                prop.className "processing-scale"
                prop.children (
                    processingLevels
                    |> List.map (fun (level, label) ->
                        Html.div [
                            prop.className (
                                if level = model.ProcessingLevel then
                                    "processing-step processing-step-active"
                                else
                                    "processing-step"
                            )
                            prop.children [
                                Html.span [ prop.className "processing-step-index"; prop.text (string (int level)) ]
                                Html.span [ prop.className "processing-step-label"; prop.text label ]
                            ]
                        ])
                )
            ]
        ]
    ]

/// Renders the setup note trimmer page.
let view model dispatch =
    Html.main [
        prop.className "page"
        prop.children [
            Html.h1 [
                prop.className "title"
                prop.text "Setup Note Beautifier"
            ]
            Html.div [
                prop.className "actions"
                prop.children [
                    Html.button [
                        prop.className "button"
                        prop.onClick (fun _ -> dispatch LoadSample)
                        prop.text "Load sample"
                    ]
                    Html.button [
                        prop.className "button button-secondary"
                        prop.onClick (fun _ -> dispatch ClearInput)
                        prop.text "Clear"
                    ]
                    Html.button [
                        prop.className "button"
                        prop.onClick (fun _ -> dispatch CopyOutput)
                        prop.disabled (model.Output = "")
                        prop.text "Copy output"
                    ]
                ]
            ]
            Html.p [
                prop.className "copy-status"
                prop.text (copyStatusText model.CopyStatus)
            ]
            processingSlider model dispatch
            Html.section [
                prop.className "columns"
                prop.children [
                    Html.div [
                        prop.className "pane"
                        prop.children [
                            Html.h2 [
                                prop.className "pane-title"
                                prop.text "Raw setup note"
                            ]
                            Html.textarea [
                                prop.className "text-area"
                                prop.value model.RawText
                                prop.placeholder "Paste setup note text here..."
                                prop.onChange (RawTextChanged >> dispatch)
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "pane"
                        prop.children [
                            Html.h2 [
                                prop.className "pane-title"
                                prop.text "Generated output"
                            ]
                            Html.textarea [
                                prop.className "text-area"
                                prop.value model.Output
                                prop.onChange (OutputTextChanged >> dispatch)
                            ]
                            Html.p [
                                prop.className (counterClassName model.CharacterCount)
                                prop.text $"Characters: {model.CharacterCount}"
                            ]
                            lengthMessages model
                        ]
                    ]
                ]
            ]
        ]
    ]
