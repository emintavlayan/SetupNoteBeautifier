namespace Shared.Tests

open System
open Shared
open Xunit

type TodoTests() =
    [<Fact>]
    member _.``Todo validation rejects an empty description to prevent creating blank tasks``() =
        let actual = Todo.isValid ""
        Assert.False(actual)

type SetupNoteBeautifierTests() =
    [<Fact>]
    member _.``Empty lines are removed so note output only contains meaningful data``() =
        let input = "Template:\n\nBreast\n\nPatient Orientation: Head First Supine"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal("Template=Breast | Patient Orientation=Head First Supine", result.Output)

    [<Fact>]
    member _.``Dot fillers are removed before key value rendering``() =
        let input = "Template: .....................\nBreast"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal("Template=Breast", result.Output)

    [<Fact>]
    member _.``When a key has no inline value the next valid line is used as value``() =
        let input = "Template:\nBreast"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal("Template=Breast", result.Output)

    [<Fact>]
    member _.``Value lines may contain colons and are preserved as part of the value``() =
        let input = "Right arm cup : .....................\nVinge Hø:  C, S:  2"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal("Right arm cup=Vinge Hø: C, S: 2", result.Output)

    [<Fact>]
    member _.``Header lines without colon are ignored unless used as carry-over value``() =
        let input = "General\nBreast board\nTemplate: Breast"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal("Template=Breast", result.Output)

    [<Fact>]
    member _.``Default render joins key value pairs with pipe separators``() =
        let input = "Template: Breast\nPatient Orientation: Head First Supine"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal("Template=Breast | Patient Orientation=Head First Supine", result.Output)

    [<Fact>]
    member _.``CharacterCount always matches the exact output string length``() =
        let input = "Template: Breast"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.Equal(result.Output.Length, result.CharacterCount)

    [<Fact>]
    member _.``Warning flag becomes true when output exceeds 230 characters``() =
        let longValue = String.replicate 232 "a"
        let input = $"Comments: {longValue}"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.True(result.IsAboveWarningLimit)

    [<Fact>]
    member _.``Hard limit flag becomes true when output exceeds 250 characters``() =
        let longValue = String.replicate 252 "a"
        let input = $"Comments: {longValue}"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        Assert.True(result.IsAboveHardLimit)

    [<Fact>]
    member _.``Known keys are shortened only when key shortening is enabled``() =
        let input = "Patient Orientation: Head First Supine"
        let disabled = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        let enabled =
            { SetupNoteBeautifier.defaultOptions with
                ShortenKnownKeys = true }
            |> fun options -> SetupNoteBeautifier.trim options input

        Assert.Equal("Patient Orientation=Head First Supine", disabled.Output)
        Assert.Equal("Ori=Head First Supine", enabled.Output)

    [<Fact>]
    member _.``Known values are shortened only when value shortening is enabled``() =
        let input = "Patient Orientation: Head First Supine"
        let disabled = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions input
        let enabled =
            { SetupNoteBeautifier.defaultOptions with
                ShortenKnownValues = true }
            |> fun options -> SetupNoteBeautifier.trim options input

        Assert.Equal("Patient Orientation=Head First Supine", disabled.Output)
        Assert.Equal("Patient Orientation=HFS", enabled.Output)
