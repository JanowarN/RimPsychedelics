# Trait change notification messages
Custom messages for the letter that fires when a psychedelic trip modifies a pawn's traits.
Currently all messages are null in XML, falling through to C# defaults.
Fluff has canModifyTraits=false and is not listed.

## Available tokens
- `[PAWN_nameDef]` — pawn's short name
- `[PAWN_pronoun]` — he/she/they
- `[PAWN_possessive]` — his/her/their
- `[DRUG_label]` — drug label (e.g., "LYS", "dried mindcap")
- `[TRAIT_label]` — the trait label being gained/lost/shifted to

## C# defaults (used when message is null)
- **gained:** "[PAWN_nameDef] developed the [TRAIT_label] trait after taking [DRUG_label]."
- **lost:** "[PAWN_nameDef] is no longer [TRAIT_label] after taking [DRUG_label]."
- **shifted:** "[PAWN_nameDef] shifted from {oldLabel} to {newLabel} after taking [DRUG_label]."

## LYS

### Spectrum traits
Spectrums have a higherMessage (degree shifts up) and lowerMessage (degree shifts down).

- **NaturalMood** (degree -2 to 2: depressive → pessimist → neutral → optimist → sanguine)
- - **higherMessage:**
- - **lowerMessage:**

- **Nerves** (degree -1 to 2: nervous → neutral → steadfast → iron-willed)
- - **higherMessage:**
- - **lowerMessage:**

- **Neurotic** (degree 0 to 2: neutral → neurotic → very neurotic)
- - **higherMessage:**
- - **lowerMessage:**

- **DrugDesire** (degree -1 to 2: teetotaler → neutral → chemical interest → chemical fascination)
- - **higherMessage:**
- - **lowerMessage:**

- **PsychicSensitivity** (degree -1 to 1: psychically deaf → neutral → psychically sensitive)
- - **higherMessage:**
- - **lowerMessage:**

### Standalone traits
Standalones have an addedMessage (trait gained) and removedMessage (trait lost).

- **Kind**
- - **addedMessage:**
- - **removedMessage:**

- **Abrasive**
- - **addedMessage:**
- - **removedMessage:**

- **Nudist**
- - **addedMessage:**
- - **removedMessage:**

- **Ascetic**
- - **addedMessage:**
- - **removedMessage:**

- **BodyPurist**
- - **addedMessage:**
- - **removedMessage:**

- **Transhumanist**
- - **addedMessage:**
- - **removedMessage:**

## Mindcap

### Spectrum traits

- **NaturalMood** (degree -1 to 1: pessimist → neutral → optimist)
- - **higherMessage:**
- - **lowerMessage:**

- **Nerves** (degree -1 to 2: nervous → neutral → steadfast → iron-willed)
- - **higherMessage:**
- - **lowerMessage:**

- **Neurotic** (degree 0 to 2: neutral → neurotic → very neurotic)
- - **higherMessage:**
- - **lowerMessage:**

- **DrugDesire** (degree -1 to 2: teetotaler → neutral → chemical interest → chemical fascination)
- - **higherMessage:**
- - **lowerMessage:**

- **PsychicSensitivity** (degree -1 to 1: psychically deaf → neutral → psychically sensitive)
- - **higherMessage:**
- - **lowerMessage:**

### Standalone traits

- **Kind**
- - **addedMessage:**
- - **removedMessage:**

- **Abrasive**
- - **addedMessage:**
- - **removedMessage:**

- **Ascetic**
- - **addedMessage:**
- - **removedMessage:**
