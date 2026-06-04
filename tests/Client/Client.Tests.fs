namespace Client.Tests

open Index
open Shared
open Xunit

type UpdateTests() =
    [<Fact>]
    member _.``Raw text change recomputes output immediately at raw level``() =
        let model, _ = init ()
        let updatedModel, _ = update (RawTextChanged "Template:\nBreast") model

        Assert.Equal("Template:\nBreast", updatedModel.Output)
        Assert.Equal(updatedModel.Output.Length, updatedModel.CharacterCount)

    [<Fact>]
    member _.``Changing processing level updates output through the deterministic pipeline``() =
        let model, _ = init ()
        let withText, _ = update (RawTextChanged "Patient Orientation: Head First Supine") model
        let updatedModel, _ = update (SetProcessingLevel 5) withText

        Assert.Equal("Ori=HFS", updatedModel.Output)

    [<Fact>]
    member _.``Loading sample fills raw text and keeps output raw at default level``() =
        let model, _ = init ()
        let updatedModel, _ = update LoadSample model

        Assert.True(updatedModel.RawText.Contains("Template:"))
        Assert.Equal(updatedModel.RawText, updatedModel.Output)
        Assert.True(updatedModel.CharacterCount > 0)

    [<Fact>]
    member _.``Generated output can be edited manually and character count follows edited text``() =
        let model, _ = init ()
        let withText, _ = update (RawTextChanged "Template:\nBreast") model
        let edited, _ = update (OutputTextChanged "Tpl=Breast | Ori=manual") withText

        Assert.Equal("Tpl=Breast | Ori=manual", edited.Output)
        Assert.Equal(23, edited.CharacterCount)

    [<Fact>]
    member _.``Processing level input is clamped to the supported range``() =
        Assert.Equal(ProcessingLevel.Raw, processingLevelFromInt -10)
        Assert.Equal(ProcessingLevel.CompactFinal, processingLevelFromInt 99)
