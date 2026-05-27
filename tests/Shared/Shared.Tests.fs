namespace Shared.Tests

open System
open Shared
open Xunit

type TodoTests() =
    [<Fact>]
    member _.``Todo validation rejects an empty description to prevent creating blank tasks``() =
        let actual = Todo.isValid ""
        Assert.False(actual)

module SetupNoteSamples =
    let breastSetupNoteWithHeadersAndFillers =
        String.concat
            "\n"
            [ "General"
              "Template:"
              "Breast"
              ""
              "Deep inspiration breathhold: yes"
              "Head turned: right"
              "Breast board"
              "Right arm cup : ....................."
              "Vinge H\u00f8:  C, S:  2"
              "Left arm cup: Vinge Ve: C, S: 3"
              "Bas: 15"
              "Opstilling long:"
              "Pinde: 4"
              "Kn\u00e6pude: yes"
              "Comments: no"
              "-----------------------------" ]

    let sameLineValuesOnly =
        String.concat
            "\n"
            [ "Template: Breast"
              "Patient Orientation: Head First Supine"
              "Comments: yes" ]

    let nextLineValuesOnly =
        String.concat
            "\n"
            [ "Template:"
              "Breast"
              "Patient Orientation:"
              "Head First Supine"
              "Comments:"
              "no" ]

    let colonInValue =
        String.concat
            "\n"
            [ "Right arm cup : ....................."
              "Vinge H\u00f8: C, S: 2" ]

type SetupNoteBeautifierRegressionTests() =
    [<Fact>]
    member _.``Default mode removes empty lines and supports next-line values``() =
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.nextLineValuesOnly
        Assert.Equal("Template=Breast\nPatient Orientation=Head First Supine\nComments=no", result.Output)

    [<Fact>]
    member _.``Default mode removes separator lines and ignores header lines without colons``() =
        let result =
            SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.breastSetupNoteWithHeadersAndFillers

        Assert.DoesNotContain("General", result.Output)
        Assert.DoesNotContain("Breast board", result.Output)
        Assert.DoesNotContain("-----------------------------", result.Output)

    [<Fact>]
    member _.``Default mode removes dot fillers before parsing values``() =
        let result =
            SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.breastSetupNoteWithHeadersAndFillers

        Assert.DoesNotContain(".....", result.Output)
        Assert.Contains("Right arm cup=Vinge H\u00f8: C, S: 2", result.Output)

    [<Fact>]
    member _.``Default mode preserves colons inside parsed value text``() =
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.colonInValue
        Assert.Equal("Right arm cup=Vinge H\u00f8: C, S: 2", result.Output)

    [<Fact>]
    member _.``Default mode renders key value pairs with newline separator``() =
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.sameLineValuesOnly
        Assert.Equal("Template=Breast\nPatient Orientation=Head First Supine\nComments=yes", result.Output)

    [<Fact>]
    member _.``Default mode character count equals output length``() =
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.sameLineValuesOnly
        Assert.Equal(result.Output.Length, result.CharacterCount)

    [<Fact>]
    member _.``When RemoveDotFillers is false filler dots remain if they are part of inline parsed values``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveDotFillers = false }

        let input = "Comments: ....................."
        let result = SetupNoteBeautifier.trim options input
        Assert.Equal("Comments=.....................", result.Output)

    [<Fact>]
    member _.``When ShortenKnownKeys is false full key names are preserved``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                ShortenKnownKeys = false }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Patient Orientation=Head First Supine", result.Output)

    [<Fact>]
    member _.``When ShortenKnownKeys is true known keys are shortened``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                ShortenKnownKeys = true }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Ori=Head First Supine", result.Output)

    [<Fact>]
    member _.``When ShortenKnownValues is false full values are preserved``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                ShortenKnownValues = false }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Patient Orientation=Head First Supine", result.Output)

    [<Fact>]
    member _.``When ShortenKnownValues is true known values are shortened``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                ShortenKnownValues = true }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Patient Orientation=HFS", result.Output)

    [<Fact>]
    member _.``Output length at or below 230 has no warning and no hard limit``() =
        let value = String.replicate 220 "a"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions $"Comments: {value}"
        Assert.False(result.IsAboveWarningLimit)
        Assert.False(result.IsAboveHardLimit)

    [<Fact>]
    member _.``Output length between 231 and 250 has warning only``() =
        let value = String.replicate 240 "a"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions $"Comments: {value}"
        Assert.True(result.IsAboveWarningLimit)
        Assert.False(result.IsAboveHardLimit)

    [<Fact>]
    member _.``Output length above 250 has warning and hard limit``() =
        let value = String.replicate 260 "a"
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions $"Comments: {value}"
        Assert.True(result.IsAboveWarningLimit)
        Assert.True(result.IsAboveHardLimit)
