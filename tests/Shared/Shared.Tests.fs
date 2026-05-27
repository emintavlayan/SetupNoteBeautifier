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

    let keyWithoutImmediateValue =
        String.concat
            "\n"
            [ "Prescriptions:"
              ""
              ""
              "Comments:"
              "yes" ]

    let prescriptionWithDashAndGeneral =
        String.concat
            "\n"
            [ "Prescription(s):"
              "-"
              ""
              ""
              "General"
              ""
              "Deep inspiration breathhold: ..........................."
              "no" ]

type SetupNoteBeautifierRegressionTests() =
    [<Fact>]
    member _.``Default mode returns input unchanged when no options are enabled``() =
        let result = SetupNoteBeautifier.trim SetupNoteBeautifier.defaultOptions SetupNoteSamples.nextLineValuesOnly
        Assert.Equal(SetupNoteSamples.nextLineValuesOnly, result.Output)

    [<Fact>]
    member _.``Remove titles mode supports next-line values``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.nextLineValuesOnly
        Assert.Equal("Template=Breast\nPatient Orientation=Head First Supine\nComments=no", result.Output)

    [<Fact>]
    member _.``Default mode removes separator lines and ignores header lines without colons``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true
                RemoveSeparatorLines = true
                RemoveDotFillers = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.breastSetupNoteWithHeadersAndFillers

        Assert.DoesNotContain("General", result.Output)
        Assert.DoesNotContain("Breast board", result.Output)
        Assert.DoesNotContain("-----------------------------", result.Output)

    [<Fact>]
    member _.``Default mode removes dot fillers before parsing values``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true
                RemoveDotFillers = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.breastSetupNoteWithHeadersAndFillers

        Assert.DoesNotContain(".....", result.Output)
        Assert.Contains("Right arm cup=Vinge H\u00f8:", result.Output)

    [<Fact>]
    member _.``Default mode preserves colons inside parsed value text``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true
                RemoveDotFillers = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.colonInValue
        Assert.Equal("Right arm cup=Vinge H\u00f8: C, S: 2", result.Output)

    [<Fact>]
    member _.``Default mode renders key value pairs with newline separator``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.sameLineValuesOnly
        Assert.Equal("Template=Breast\nPatient Orientation=Head First Supine\nComments=yes", result.Output)

    [<Fact>]
    member _.``Default mode character count equals output length``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.sameLineValuesOnly
        Assert.Equal(result.Output.Length, result.CharacterCount)

    [<Fact>]
    member _.``When RemoveDotFillers is false filler dots remain if they are part of inline parsed values``() =
        let options = { SetupNoteBeautifier.defaultOptions with RemoveHeaderLines = true }

        let input = "Comments: ....................."
        let result = SetupNoteBeautifier.trim options input
        Assert.Equal("Comments=.....................", result.Output)

    [<Fact>]
    member _.``When ShortenKnownKeys is false full key names are preserved``() =
        let options = { SetupNoteBeautifier.defaultOptions with RemoveHeaderLines = true; ShortenKnownKeys = false }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Patient Orientation=Head First Supine", result.Output)

    [<Fact>]
    member _.``When ShortenKnownKeys is true known keys are shortened``() =
        let options = { SetupNoteBeautifier.defaultOptions with RemoveHeaderLines = true; ShortenKnownKeys = true }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Ori=Head First Supine", result.Output)

    [<Fact>]
    member _.``When ShortenKnownValues is false full values are preserved``() =
        let options = { SetupNoteBeautifier.defaultOptions with RemoveHeaderLines = true; ShortenKnownValues = false }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Patient Orientation=Head First Supine", result.Output)

    [<Fact>]
    member _.``When ShortenKnownValues is true known values are shortened``() =
        let options = { SetupNoteBeautifier.defaultOptions with RemoveHeaderLines = true; ShortenKnownValues = true }

        let result = SetupNoteBeautifier.trim options "Patient Orientation: Head First Supine"
        Assert.Equal("Patient Orientation=HFS", result.Output)

    [<Fact>]
    member _.``Missing key value does not consume later title as value``() =
        let options = { SetupNoteBeautifier.defaultOptions with RemoveHeaderLines = true }
        let result = SetupNoteBeautifier.trim options SetupNoteSamples.keyWithoutImmediateValue
        Assert.Equal("Prescriptions=\nComments=yes", result.Output)

    [<Fact>]
    member _.``When RemoveEmptyKeys is true entries with empty values are removed``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true
                RemoveEmptyKeys = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.keyWithoutImmediateValue
        Assert.Equal("Comments=yes", result.Output)

    [<Fact>]
    member _.``Prescription key does not consume General title as value``() =
        let options =
            { SetupNoteBeautifier.defaultOptions with
                RemoveHeaderLines = true
                RemoveDotFillers = true }

        let result = SetupNoteBeautifier.trim options SetupNoteSamples.prescriptionWithDashAndGeneral
        Assert.Equal("Prescription(s)=\nDeep inspiration breathhold=no", result.Output)

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
