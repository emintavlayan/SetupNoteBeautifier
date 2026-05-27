module Index

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

type OptionField =
    | RemoveSeparatorLines
    | RemoveDotFillers
    | NormalizeSpaces
    | RemoveHeaderLines
    | RemoveEmptyKeys
    | ShortenKnownValues
    | ShortenKnownKeys

type Model =
    { RawText: string
      Options: TrimOptions
      Output: string
      CharacterCount: int
      CopyStatus: CopyStatus }

type Msg =
    | RawTextChanged of string
    | OutputTextChanged of string
    | ToggleOption of OptionField
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
        [ "General"
          "Template:"
          "Breast"
          ""
          "Patient Orientation: Head First Supine"
          "Right arm cup : ....................."
          "Vinge H\u00f8:  C, S:  2"
          "Breast board"
          "Comments: yes" ]

/// Applies trim options to raw input and returns output text with character count.
let applyTrim (options: TrimOptions) (rawText: string) =
    let result = SetupNoteBeautifier.trim options rawText
    result.Output, result.CharacterCount

/// Recomputes output and character count after model changes.
let evaluateModel (model: Model) =
    let output, characterCount = applyTrim model.Options model.RawText
    { model with
        Output = output
        CharacterCount = characterCount }

/// Toggles one trimming option in the current options record.
let toggleOption (field: OptionField) (options: TrimOptions) =
    match field with
    | RemoveSeparatorLines -> { options with RemoveSeparatorLines = not options.RemoveSeparatorLines }
    | RemoveDotFillers -> { options with RemoveDotFillers = not options.RemoveDotFillers }
    | NormalizeSpaces -> { options with NormalizeSpaces = not options.NormalizeSpaces }
    | RemoveHeaderLines -> { options with RemoveHeaderLines = not options.RemoveHeaderLines }
    | RemoveEmptyKeys -> { options with RemoveEmptyKeys = not options.RemoveEmptyKeys }
    | ShortenKnownValues -> { options with ShortenKnownValues = not options.ShortenKnownValues }
    | ShortenKnownKeys -> { options with ShortenKnownKeys = not options.ShortenKnownKeys }

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
          Options = SetupNoteBeautifier.defaultOptions
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
    | ToggleOption field ->
        { model with
            Options = toggleOption field model.Options
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

/// Renders one checkbox option bound to a toggle message.
let optionCheckbox (label: string) field value dispatch =
    Html.label [
        prop.className "option-item"
        prop.children [
            Html.input [
                prop.type'.checkbox
                prop.isChecked value
                prop.onChange (fun (_: bool) -> dispatch (ToggleOption field))
            ]
            Html.span [ prop.text label ]
        ]
    ]

/// Renders contextual output length warning messages.
let lengthMessages (model: Model) =
    let messages =
        [ if model.CharacterCount > SetupNoteBeautifier.warningLimit && not model.Options.ShortenKnownKeys then
              Html.p [
                  prop.className "message message-warning"
                  prop.text "Output is long. Enable extreme key shortening if needed."
              ]
          if model.CharacterCount > SetupNoteBeautifier.hardLimit then
              Html.p [
                  prop.className "message message-error"
                  prop.text "Output is above system limit."
              ] ]

    Html.div [ prop.children messages ]

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
            Html.section [
                prop.className "options"
                prop.children [
                    optionCheckbox
                        "Remove separator lines"
                        RemoveSeparatorLines
                        model.Options.RemoveSeparatorLines
                        dispatch
                    optionCheckbox "Remove dot fillers" RemoveDotFillers model.Options.RemoveDotFillers dispatch
                    optionCheckbox "Normalize spaces" NormalizeSpaces model.Options.NormalizeSpaces dispatch
                    optionCheckbox
                        "Remove header lines (titles)"
                        RemoveHeaderLines
                        model.Options.RemoveHeaderLines
                        dispatch
                    optionCheckbox
                        "Remove empty keys"
                        RemoveEmptyKeys
                        model.Options.RemoveEmptyKeys
                        dispatch
                    optionCheckbox "Shorten known values" ShortenKnownValues model.Options.ShortenKnownValues dispatch
                    optionCheckbox
                        "Shorten known keys / extreme"
                        ShortenKnownKeys
                        model.Options.ShortenKnownKeys
                        dispatch
                ]
            ]
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
