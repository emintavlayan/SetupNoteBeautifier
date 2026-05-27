namespace Shared

open System
open System.Text.RegularExpressions

type TrimOptions =
    { DeleteEmptyLines: bool
      RemoveSeparatorLines: bool
      RemoveDotFillers: bool
      NormalizeSpaces: bool
      KeepKeyValuePairsOnly: bool
      ShortenKnownKeys: bool
      ShortenKnownValues: bool }

type KeyValue = { Key: string; Value: string }

type TrimResult =
    { Output: string
      CharacterCount: int
      IsAboveWarningLimit: bool
      IsAboveHardLimit: bool
      KeyValues: KeyValue list }

module SetupNoteBeautifier =
    /// Default trimming options for setup note cleanup.
    let defaultOptions =
        { DeleteEmptyLines = true
          RemoveSeparatorLines = true
          RemoveDotFillers = true
          NormalizeSpaces = true
          KeepKeyValuePairsOnly = true
          ShortenKnownKeys = false
          ShortenKnownValues = false }

    /// Warning character limit for output length.
    let warningLimit = 230

    /// Hard character limit for output length.
    let hardLimit = 250

    /// Replaces CRLF and CR with LF.
    let normalizeLineEndings (text: string) = text.Replace("\r\n", "\n").Replace("\r", "\n")

    /// Splits normalized text into lines.
    let splitLines (text: string) = text.Split('\n') |> Array.toList

    /// Trims leading and trailing whitespace from one line.
    let trimLine (line: string) = line.Trim()

    /// Trims leading and trailing whitespace from all lines.
    let trimLines (lines: string list) = lines |> List.map trimLine

    /// Returns true when a line is made only of separator symbols.
    let isSeparatorLine (line: string) =
        let trimmed = line.Trim()
        trimmed.Length > 0
        && trimmed
           |> Seq.forall (fun c -> c = '-' || c = '_' || c = '=' || c = '*' || c = '~')

    /// Returns true when a line is empty or whitespace only.
    let isEmptyLine (line: string) = String.IsNullOrWhiteSpace line

    /// Removes empty lines.
    let removeEmptyLines (lines: string list) = lines |> List.filter (isEmptyLine >> not)

    /// Removes separator lines.
    let removeSeparatorLines (lines: string list) = lines |> List.filter (isSeparatorLine >> not)

    /// Removes dot filler sequences from one line.
    let removeDotFillersFromLine (line: string) = Regex.Replace(line, @"\.{3,}", "").Trim()

    /// Removes dot fillers from all lines.
    let removeDotFillersFromLines (lines: string list) = lines |> List.map removeDotFillersFromLine

    /// Collapses repeated whitespace to a single space.
    let collapseSpaces (text: string) = Regex.Replace(text, @"\s+", " ").Trim()

    /// Splits a key-value line on the first colon.
    let splitFirstColon (line: string) =
        let idx = line.IndexOf ':'
        if idx < 0 then
            None
        else
            Some(line.Substring(0, idx), line.Substring(idx + 1))

    /// Finds the next non-empty non-separator line index from a start index.
    let findNextValueIndex (lines: string list) (startIndex: int) =
        lines
        |> List.mapi (fun i line -> i, line)
        |> List.tryFind (fun (i, line) -> i >= startIndex && not (isEmptyLine line) && not (isSeparatorLine line))
        |> Option.map fst

    /// Parses key-value pairs from lines while supporting values on the following line.
    let parseKeyValues (lines: string list) =
        let rec loop i acc =
            if i >= lines.Length then
                List.rev acc
            else
                let line = lines[i]
                match splitFirstColon line with
                | None -> loop (i + 1) acc
                | Some(keyRaw, valueRaw) ->
                    let key = keyRaw.Trim()
                    let value = valueRaw.Trim()
                    if value <> "" then
                        loop (i + 1) ({ Key = key; Value = value } :: acc)
                    else
                        match findNextValueIndex lines (i + 1) with
                        | Some nextIndex ->
                            let nextValue = lines[nextIndex].Trim()
                            loop (nextIndex + 1) ({ Key = key; Value = nextValue } :: acc)
                        | None -> loop (i + 1) ({ Key = key; Value = "" } :: acc)

        loop 0 []

    /// Normalizes key and value text for one pair.
    let normalizeKeyValue (normalizeSpaces: bool) (pair: KeyValue) =
        let normalize = if normalizeSpaces then collapseSpaces else trimLine
        { Key = normalize pair.Key; Value = normalize pair.Value }

    /// Returns shortened key text for known key names.
    let shortenKey (key: string) =
        match key with
        | "Template" -> "Tpl"
        | "Patient Orientation" -> "Ori"
        | "Deep inspiration breathhold" -> "DIBH"
        | "Head turned" -> "Head"
        | "Right arm cup" -> "RArm"
        | "Left arm cup" -> "LArm"
        | "Opstilling long" -> "Long"
        | "Kn\u00e6pude" -> "Knee"
        | "Comments" -> "Comment"
        | _ -> key

    /// Returns shortened value text for known value names.
    let shortenValue (value: string) =
        match value with
        | "Head First Supine" -> "HFS"
        | "Head First Prone" -> "HFP"
        | "Feet First Supine" -> "FFS"
        | "Feet First Prone" -> "FFP"
        | "left" -> "L"
        | "right" -> "R"
        | "yes" -> "yes"
        | "Yes" -> "yes"
        | "no" -> "no"
        | "No" -> "no"
        | _ -> value

    /// Applies optional key and value shortening.
    let shortenKnown (options: TrimOptions) (pair: KeyValue) =
        { Key = if options.ShortenKnownKeys then shortenKey pair.Key else pair.Key
          Value = if options.ShortenKnownValues then shortenValue pair.Value else pair.Value }

    /// Renders key-value pairs in key=value format separated by new lines.
    let renderKeyValues (pairs: KeyValue list) = pairs |> List.map (fun p -> $"{p.Key}={p.Value}") |> String.concat "\n"

    /// Builds a trim result from output and parsed pairs.
    let toTrimResult (output: string) (pairs: KeyValue list) =
        let count = output.Length
        { Output = output
          CharacterCount = count
          IsAboveWarningLimit = count > warningLimit
          IsAboveHardLimit = count > hardLimit
          KeyValues = pairs }

    /// Applies setup note trimming and parsing with the provided options.
    let trim (options: TrimOptions) (rawText: string) =
        let lines =
            rawText
            |> normalizeLineEndings
            |> splitLines
            |> trimLines
            |> fun xs -> if options.DeleteEmptyLines then removeEmptyLines xs else xs
            |> fun xs -> if options.RemoveSeparatorLines then removeSeparatorLines xs else xs
            |> fun xs -> if options.RemoveDotFillers then removeDotFillersFromLines xs else xs

        let keyValues =
            lines
            |> parseKeyValues
            |> List.map (normalizeKeyValue options.NormalizeSpaces)
            |> List.map (shortenKnown options)

        let pairs = if options.KeepKeyValuePairsOnly then keyValues else keyValues
        pairs |> renderKeyValues |> fun output -> toTrimResult output pairs

