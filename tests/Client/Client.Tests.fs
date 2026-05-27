namespace Client.Tests

open Index
open Shared
open Xunit

type UpdateTests() =
    [<Fact>]
    member _.``Raw text change recomputes output immediately using default trim options``() =
        let model, _ = init ()
        let updatedModel, _ = update (RawTextChanged "Template:\nBreast") model

        Assert.Equal("Template=Breast", updatedModel.Output)
        Assert.Equal(updatedModel.Output.Length, updatedModel.CharacterCount)

    [<Fact>]
    member _.``Toggling shorten known keys applies extreme key abbreviation in output``() =
        let model, _ = init ()
        let withText, _ = update (RawTextChanged "Patient Orientation: Head First Supine") model
        let updatedModel, _ = update (ToggleOption ShortenKnownKeys) withText

        Assert.Equal("Ori=Head First Supine", updatedModel.Output)

    [<Fact>]
    member _.``Loading sample fills raw text and produces parsed output``() =
        let model, _ = init ()
        let updatedModel, _ = update LoadSample model

        Assert.True(updatedModel.RawText.Contains("Template:"))
        Assert.True(updatedModel.Output.Contains("Template=Breast"))
        Assert.True(updatedModel.CharacterCount > 0)
