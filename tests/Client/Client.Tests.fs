namespace Client.Tests

open Index
open Shared
open Xunit

type UpdateTests() =
    [<Fact>]
    member _.``Raw text change recomputes output immediately using default trim options``() =
        let model, _ = init ()
        let updatedModel, _ = update (RawTextChanged "Template:\nBreast") model

        Assert.Equal("Template:\nBreast", updatedModel.Output)
        Assert.Equal(updatedModel.Output.Length, updatedModel.CharacterCount)

    [<Fact>]
    member _.``Shorten known keys applies abbreviation when remove titles is enabled``() =
        let model, _ = init ()
        let withText, _ = update (RawTextChanged "Patient Orientation: Head First Supine") model
        let withTitlesRemoved, _ = update (ToggleOption RemoveHeaderLines) withText
        let updatedModel, _ = update (ToggleOption ShortenKnownKeys) withTitlesRemoved

        Assert.Equal("Ori=Head First Supine", updatedModel.Output)

    [<Fact>]
    member _.``Loading sample fills raw text and keeps output unformatted by default``() =
        let model, _ = init ()
        let updatedModel, _ = update LoadSample model

        Assert.True(updatedModel.RawText.Contains("Template:"))
        Assert.Equal(updatedModel.RawText, updatedModel.Output)
        Assert.True(updatedModel.CharacterCount > 0)

    [<Fact>]
    member _.``Generated output can be edited manually and character count follows edited text``() =
        let model, _ = init ()
        let withText, _ = update (RawTextChanged "Template:\nBreast") model
        let edited, _ = update (OutputTextChanged "Template=Breast\nComment=manual") withText

        Assert.Equal("Template=Breast\nComment=manual", edited.Output)
        Assert.Equal(30, edited.CharacterCount)
