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
    let pelvisSetupNote =
        String.concat
            "\n"
            [ "Course number"
              "Course Number: 12"
              "Benfiksation"
              "Arme"
              "Arme: Oppe"
              "Madras"
              "Madras: Kort madras"
              "Unknown line: should be ignored"
              "//////" ]

    let headNeckSetupNote =
        String.concat
            "\n"
            [ "Template:"
              "Head Neck"
              "Patient Orientation:"
              "Head First Supine"
              "Nakkestøtte"
              "Nakkestøtter: ..........................."
              "Mellem"
              "Maske"
              "Knæpude"
              "Knæpude: På bryst"
              "Comments"
              "Photos" ]

    let breastSetupNote =
        String.concat
            "\n"
            [ "Template:"
              "Breast"
              "Patient Orientation: Head First Supine"
              "General"
              "Deep inspiration breathhold:"
              "Yes"
              "Breast board"
              "Kile: L2"
              "Knæpude"
              "Knæpude: På bryst"
              "Gatingboks"
              "Gatingboks: Yes"
              "Comments:"
              ""
              "Photos"
              "------------------------" ]

type SetupNoteBeautifierRegressionTests() =
    [<Fact>]
    member _.``Raw level returns input unchanged``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.Raw SetupNoteSamples.headNeckSetupNote
        Assert.Equal(SetupNoteSamples.headNeckSetupNote, result.Output)

    [<Fact>]
    member _.``Clean lines removes blank lines and trims each line``() =
        let input = " Template: \n \n Breast \n"
        let result = SetupNoteBeautifier.trim ProcessingLevel.CleanLines input
        Assert.Equal("Template:\nBreast", result.Output)

    [<Fact>]
    member _.``Remove visual noise strips separator and slash lines``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.RemoveVisualNoise SetupNoteSamples.pelvisSetupNote
        Assert.DoesNotContain("//////", result.Output)

    [<Fact>]
    member _.``Parse known fields only ignores section titles and unknown colon lines``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.ParseKnownFields SetupNoteSamples.pelvisSetupNote

        Assert.Equal("Course Number=12\nArme=Oppe\nMadras=Kort madras", result.Output)
        Assert.DoesNotContain("Course number", result.Output)
        Assert.DoesNotContain("Benfiksation", result.Output)
        Assert.DoesNotContain("Unknown line", result.Output)

    [<Fact>]
    member _.``Next-line parsing preserves Danish values while dot fillers do not block key recognition``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.ParseKnownFields SetupNoteSamples.headNeckSetupNote

        Assert.Equal(
            "Template=Head Neck\nPatient Orientation=Head First Supine\nNakkestøtter=Mellem\nKnæpude=På bryst",
            result.Output
        )

    [<Fact>]
    member _.``Known English values are shortened only at shorten-safe-values level``() =
        let parsed = SetupNoteBeautifier.trim ProcessingLevel.ParseKnownFields SetupNoteSamples.breastSetupNote
        let shortened = SetupNoteBeautifier.trim ProcessingLevel.ShortenSafeValues SetupNoteSamples.breastSetupNote

        Assert.Contains("Patient Orientation=Head First Supine", parsed.Output)
        Assert.Contains("Patient Orientation=HFS", shortened.Output)
        Assert.Contains("Deep inspiration breathhold=yes", shortened.Output)
        Assert.Contains("Knæpude=På bryst", shortened.Output)
        Assert.Contains("Kile=L2", shortened.Output)

    [<Fact>]
    member _.``Shorten safe keys uses the compact key map without translating Danish values``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.ShortenSafeKeys SetupNoteSamples.breastSetupNote

        Assert.Equal(
            "Tpl=Breast\nOri=HFS\nDIBH=yes\nKile=L2\nKnæpude=På bryst\nGatingboks=yes",
            result.Output
        )

    [<Fact>]
    member _.``Compact final renders pipe separated output``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.CompactFinal SetupNoteSamples.breastSetupNote
        Assert.Equal("Tpl=Breast | Ori=HFS | DIBH=yes | Kile=L2 | Knæpude=På bryst | Gatingboks=yes", result.Output)

    [<Fact>]
    member _.``Output character count equals output length``() =
        let result = SetupNoteBeautifier.trim ProcessingLevel.CompactFinal SetupNoteSamples.breastSetupNote
        Assert.Equal(result.Output.Length, result.CharacterCount)

    [<Fact>]
    member _.``Output length at or below 230 has no warning and no hard limit``() =
        let value = String.replicate 220 "a"
        let result = SetupNoteBeautifier.trim ProcessingLevel.ParseKnownFields $"Template: {value}"
        Assert.False(result.IsAboveWarningLimit)
        Assert.False(result.IsAboveHardLimit)

    [<Fact>]
    member _.``Output length between 231 and 250 has warning only``() =
        let value = String.replicate 240 "a"
        let result = SetupNoteBeautifier.trim ProcessingLevel.ParseKnownFields $"Template: {value}"
        Assert.True(result.IsAboveWarningLimit)
        Assert.False(result.IsAboveHardLimit)

    [<Fact>]
    member _.``Output length above 250 has warning and hard limit``() =
        let value = String.replicate 260 "a"
        let result = SetupNoteBeautifier.trim ProcessingLevel.ParseKnownFields $"Template: {value}"
        Assert.True(result.IsAboveWarningLimit)
        Assert.True(result.IsAboveHardLimit)
