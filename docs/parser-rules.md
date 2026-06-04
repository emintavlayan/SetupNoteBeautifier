# Parser Rules

## Deterministic Slider Pipeline

The UI uses one `Processing level` slider. Each level is cumulative and defines the full pipeline:

1. `Raw`
2. `Clean lines`
3. `Remove visual noise`
4. `Parse known setup fields only`
5. `Shorten safe values`
6. `Shorten safe keys`
7. `Compact final output`

This removes invalid combinations from the old checkbox model. A user can only move forward or backward along one deterministic path.

## Known-Key-Only Parsing

Parsing starts at `Parse known setup fields only`.

Only keys in the known-key map are parsed:

- `Template` -> `Tpl`
- `Patient Orientation` -> `Ori`
- `Prescription(s)` -> `Rx`
- `Course Number` -> `Course`
- `Fodstøtte` -> `Fodstøtte`
- `Vinkel` -> `Vinkel`
- `Arme` -> `Arme`
- `Madras` -> `Madras`
- `Nakkestøtter` -> `Nakkestøtte`
- `Net` -> `Net`
- `Knæpude` -> `Knæpude`
- `Contrast media used` -> `Contrast`
- `Iodine allergy` -> `IodineAllergy`
- `Hearing aids removed for CT sim?` -> `HearingAidsRemoved`
- `Dentures removed for CT sim` -> `DenturesRemoved`
- `Deep inspiration breathhold` -> `DIBH`
- `Kile` -> `Kile`
- `Bas` -> `Bas`
- `Opstilling long` -> `Long`
- `Pinde` -> `Pinde`
- `Gatingboks` -> `Gatingboks`

Unknown colon lines are ignored. A line is never parsed just because it contains one colon.

## Section Title Handling

These reserved titles document structure and prevent false positives:

- `Course number`
- `Benfiksation`
- `Arme`
- `Madras`
- `Nakkestøtte`
- `Maske`
- `Knæpude`
- `General`
- `Breast board`
- `Gatingboks`
- `Comments`
- `Photos`

Titles do not become output fields.

Colon presence still matters:

- `Knæpude` is a section title.
- `Knæpude:` is a parsable key.

The same rule applies to `Arme` and `Madras`.

## Danish Preservation Rule

Danish clinical values are preserved unless they are explicitly listed in the safe value map.

Examples that stay unchanged:

- `På bryst`
- `Kort madras`
- `Mellem`
- `B+20`
- `C`
- `L2`

## Safe Shortening Rule

Safe value shortening starts at `Shorten safe values`:

- `Head First Supine` -> `HFS`
- `Head First Prone` -> `HFP`
- `Feet First Supine` -> `FFS`
- `Feet First Prone` -> `FFP`
- `Yes` -> `yes`
- `yes` -> `yes`
- `No` -> `no`
- `no` -> `no`

Safe key shortening starts at `Shorten safe keys` and uses the compact key map shown above.

## Pair Parsing Rules

- Line endings are normalized before processing.
- At `Clean lines` and above, each line is trimmed and blank lines are removed.
- At `Remove visual noise` and above, separator lines and `//////` lines are removed.
- Dot fillers are removed before parsing.
- A known key can take its value from the same line after the colon.
- If the value after the colon is empty, the next non-empty line is used only when it is not a section title and not another colon line.
- Empty parsed values are omitted from output.

## Output Format

At `Parse known setup fields only`, `Shorten safe values`, and `Shorten safe keys`, output is rendered as one pair per line:

```text
Key=value
```

At `Compact final output`, the same pairs are rendered on one line:

```text
Key=value | Key=value | Key=value
```

## Examples

Before:

```text
Patient Orientation:
Head First Supine
Knæpude
Knæpude: På bryst
Comments:
```

After `Parse known setup fields only`:

```text
Patient Orientation=Head First Supine
Knæpude=På bryst
```

After `Compact final output`:

```text
Ori=HFS | Knæpude=På bryst
```
