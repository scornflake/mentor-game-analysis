# TODO

- Wiki content needs to be heirarchical. e.g: in reading phantasma, there is a line "Primary fire shoots six continuous beams", and under that are 5 more lines (indented). The intent is that all those five lines (indented) are related to the primary fire of the weapon.  Recommend a new game rule structure that takes this into account. Also consider how the wiki extraction should represent this, to make it easier for an LLM to parse it to the new game rule structure.

- ExtractCharacteristics_WithRealWikiContent_ExtractsExpectedCharacteristics is failing for Phantasma, because characteristic extraction is taking into account "advantages" and "disadvantages". Fix extraction to only the characteristics sections.
