namespace Shared

open System
open System.Collections.Generic
open System.Text.RegularExpressions

type ProcessingLevel =
    | Raw = 0
    | CleanLines = 1
    | RemoveVisualNoise = 2
    | ParseKnownFields = 3
    | ShortenSafeValues = 4
    | ShortenSafeKeys = 5
    | CompactFinal = 6

type KeyValue = { Key: string; Value: string }

type TrimResult =
    { Output: string
      CharacterCount: int
      IsAboveWarningLimit: bool
      IsAboveHardLimit: bool
      KeyValues: KeyValue list }

module SetupNoteBeautifier =
    type KnownKey =
        { FullKey: string
          CompactKey: string }

    /// Default processing level for first render.
    let defaultProcessingLevel = ProcessingLevel.Raw

    /// Warning character limit for output length.
    let warningLimit = 230

    /// Hard character limit for output length.
    let hardLimit = 250

    let knownKeyDefinitions =
        [ "Template", "Tpl"
          "Patient Orientation", "Ori"
          "Prescription(s)", "Rx"
          "Course Number", "Course"
          "Fodstøtte", "Fodstøtte"
          "Vinkel", "Vinkel"
          "Arme", "Arme"
          "Madras", "Madras"
          "Nakkestøtter", "Nakkestøtte"
          "Net", "Net"
          "Knæpude", "Knæpude"
          "Contrast media used", "Contrast"
          "Iodine allergy", "IodineAllergy"
          "Hearing aids removed for CT sim?", "HearingAidsRemoved"
          "Dentures removed for CT sim", "DenturesRemoved"
          "Deep inspiration breathhold", "DIBH"
          "Kile", "Kile"
          "Bas", "Bas"
          "Opstilling long", "Long"
          "Right arm cup", "RArm"
          "Left arm cup", "LArm"
          "Pinde", "Pinde"
          "Gatingboks", "Gatingboks" ]

    let reservedSectionTitles =
        [ "Course number"
          "Benfiksation"
          "Arme"
          "Madras"
          "Nakkestøtte"
          "Maske"
          "Knæpude"
          "General"
          "Breast board"
          "Gatingboks"
          "Comments"
          "Photos" ]

    /// Replaces CRLF and CR with LF.
    let normalizeLineEndings (text: string) = text.Replace("\r\n", "\n").Replace("\r", "\n")

    /// Splits normalized text into lines.
    let splitLines (text: string) = text.Split('\n') |> Array.toList

    /// Trims leading and trailing whitespace from one line.
    let trimLine (line: string) = line.Trim()

    /// Collapses repeated whitespace to a single space.
    let collapseSpaces (text: string) = Regex.Replace(text, @"\s+", " ").Trim()

    /// Removes dot filler sequences from one line.
    let removeDotFillersFromLine (line: string) = Regex.Replace(line, @"\.{3,}", "")

    /// Returns true when a line is made only of separator symbols.
    let isSeparatorLine (line: string) =
        let trimmed = line.Trim()
        trimmed.Length > 0
        && trimmed
           |> Seq.forall (fun c -> c = '-' || c = '_' || c = '=' || c = '*' || c = '~' || c = '/')

    let normalizeLookupText (text: string) = text |> collapseSpaces

    let buildKnownKeyMap () =
        let map = Dictionary<string, KnownKey>(StringComparer.OrdinalIgnoreCase)

        for fullKey, compactKey in knownKeyDefinitions do
            let normalized = normalizeLookupText fullKey
            map[normalized] <- { FullKey = fullKey; CompactKey = compactKey }

        map

    let buildSectionTitleSet () =
        HashSet<string>(reservedSectionTitles |> List.map normalizeLookupText, StringComparer.OrdinalIgnoreCase)

    let knownKeyMap = buildKnownKeyMap ()

    let reservedSectionTitleSet = buildSectionTitleSet ()

    let tryGetKnownKey (key: string) =
        let normalized = normalizeLookupText key

        match knownKeyMap.TryGetValue normalized with
        | true, knownKey -> Some knownKey
        | _ -> None

    /// Splits a key-value line on the first colon.
    let splitFirstColon (line: string) =
        let idx = line.IndexOf ':'

        if idx < 0 then
            None
        else
            Some(line.Substring(0, idx), line.Substring(idx + 1))

    let isReservedSectionTitle (line: string) =
        let normalized = normalizeLookupText line
        line.Contains(':') |> not && reservedSectionTitleSet.Contains normalized

    let hasProcessingLevel (currentLevel: ProcessingLevel) (requiredLevel: ProcessingLevel) =
        int currentLevel >= int requiredLevel

    let preprocessLines (level: ProcessingLevel) (rawText: string) =
        let normalized = normalizeLineEndings rawText

        if level = ProcessingLevel.Raw then
            normalized |> splitLines
        else
            normalized
            |> splitLines
            |> List.map trimLine
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> fun lines ->
                if hasProcessingLevel level ProcessingLevel.RemoveVisualNoise then
                    lines
                    |> List.filter (isSeparatorLine >> not)
                    |> List.map removeDotFillersFromLine
                    |> List.map trimLine
                    |> List.filter (String.IsNullOrWhiteSpace >> not)
                else
                    lines

    let normalizeValueText (value: string) = value |> collapseSpaces

    let isKnownKeyLine (line: string) =
        match splitFirstColon line with
        | Some(keyRaw, _) -> tryGetKnownKey keyRaw |> Option.isSome
        | None -> false

    let canUseNextLineAsValue (line: string) =
        not (String.IsNullOrWhiteSpace line)
        && not (isReservedSectionTitle line)
        && not (isKnownKeyLine line)

    /// Parses known key-value pairs while supporting values on the immediate next non-empty line.
    let parseKnownKeyValues (lines: string list) =
        let rec loop index acc =
            if index >= lines.Length then
                List.rev acc
            else
                let line = lines[index]

                match splitFirstColon line with
                | None -> loop (index + 1) acc
                | Some(keyRaw, valueRaw) ->
                    match tryGetKnownKey keyRaw with
                    | None -> loop (index + 1) acc
                    | Some knownKey ->
                        let inlineValue = valueRaw |> removeDotFillersFromLine |> normalizeValueText

                        if inlineValue <> "" then
                            let pair =
                                { Key = knownKey.FullKey
                                  Value = inlineValue }

                            loop (index + 1) (pair :: acc)
                        elif index + 1 < lines.Length then
                            let nextLine = lines[index + 1] |> normalizeValueText

                            if canUseNextLineAsValue nextLine then
                                let pair =
                                    { Key = knownKey.FullKey
                                      Value = nextLine }

                                loop (index + 2) (pair :: acc)
                            else
                                loop (index + 1) acc
                        else
                            loop (index + 1) acc

        loop 0 []

    /// Returns shortened value text for known safe values.
    let shortenValue (value: string) =
        match value with
        | "Head First Supine" -> "HFS"
        | "Head First Prone" -> "HFP"
        | "Feet First Supine" -> "FFS"
        | "Feet First Prone" -> "FFP"
        | "Yes"
        | "yes" -> "yes"
        | "No"
        | "no" -> "no"
        | _ -> value

    let shortenPair (level: ProcessingLevel) (pair: KeyValue) =
        let key =
            if hasProcessingLevel level ProcessingLevel.ShortenSafeKeys then
                match tryGetKnownKey pair.Key with
                | Some knownKey -> knownKey.CompactKey
                | None -> pair.Key
            else
                pair.Key

        let value =
            if hasProcessingLevel level ProcessingLevel.ShortenSafeValues then
                shortenValue pair.Value
            else
                pair.Value

        { Key = key; Value = value }

    /// Renders key-value pairs in key=value format separated by newlines.
    let renderParsedOutput (level: ProcessingLevel) (pairs: KeyValue list) =
        let separator =
            if hasProcessingLevel level ProcessingLevel.CompactFinal then
                " | "
            else
                "\n"

        pairs |> List.map (fun pair -> $"{pair.Key}={pair.Value}") |> String.concat separator

    /// Builds a trim result from output and parsed pairs.
    let toTrimResult (output: string) (pairs: KeyValue list) =
        let count = output.Length

        { Output = output
          CharacterCount = count
          IsAboveWarningLimit = count > warningLimit
          IsAboveHardLimit = count > hardLimit
          KeyValues = pairs }

    /// Applies setup note processing according to the selected deterministic level.
    let trim (level: ProcessingLevel) (rawText: string) =
        let lines = preprocessLines level rawText

        if hasProcessingLevel level ProcessingLevel.ParseKnownFields then
            let parsedPairs =
                lines
                |> parseKnownKeyValues
                |> List.map (fun pair ->
                    { Key = normalizeLookupText pair.Key
                      Value = normalizeValueText pair.Value })
                |> List.filter (fun pair -> pair.Value <> "")
                |> List.map (shortenPair level)

            parsedPairs |> renderParsedOutput level |> fun output -> toTrimResult output parsedPairs
        else
            let output = String.concat "\n" lines
            toTrimResult output []
