# Warframe Items CLI

A command-line utility to search and filter Warframe items using the [@wfcd/items](https://github.com/WFCD/warframe-items) package.

## Installation

```bash
cd src/experiment
npm install
```

## Usage

```bash
node index.js [options]
```

### Options

- `-s, --search <term>` - Search items by name (case-insensitive)
- `-u, --lookup <unique>` - Lookup item by unique name (shows item's own stats)
- `--unique <unique>` - (same as --lookup)
- `-f, --full` - Show ALL properties including components/drops/patchlogs (use with --lookup)
- `-c, --category <type>` - Filter by category (e.g., Warframes, Melee, Primary)
- `-t, --tradable` - Show only tradable items
- `--not-tradable` - Show only non-tradable items
- `-l, --limit <number>` - Limit number of results (default: 20)
- `-h, --help` - Show help message

### Available Categories

All, Arcanes, Archwing, Arch-Gun, Arch-Melee, Enemy, Fish, Gear, Glyphs, Melee, Misc, Mods, Node, Pets, Primary, Quests, Relics, Resources, Secondary, Sentinels, SentinelWeapons, Sigils, Skins, Warframes

## Examples

Search for items with "excalibur" in the name:
```bash
node index.js --search "excalibur"
```

Find all Warframes with "prime" in the name:
```bash
node index.js --search "prime" --category "Warframes"
```

Show only tradable mods:
```bash
node index.js --category "Mods" --tradable
```

Show more results:
```bash
node index.js --search "prime" --limit 50
```

Lookup a specific item by its unique name (shows item's own stats):
```bash
node index.js --lookup "/Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon"
```

Lookup with full details (includes components, drops, patchlogs):
```bash
node index.js --lookup "/Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon" --full
```

Two-step workflow (search then lookup for details):
```bash
# Step 1: Search to find the item
node index.js --search "cedo"
# Output shows: Unique Name: /Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon

# Step 2: Lookup with the exact unique name for item stats
node index.js --lookup "/Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon"

# Step 3 (optional): Add --full for complete details including components/drops
node index.js --lookup "/Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon" --full
```

## Output

### Search/Filter Mode
The CLI displays the following information for each item in search results:
- Name
- Type
- Category
- Tradable status
- Unique name (internal game identifier)
- Image URL (CDN link to item image)

### Lookup Mode
When using `--lookup`, the item's own properties are displayed, including:
- Complete attack statistics (damage, crit chance, status, etc.)
- Base item stats (armor, health, shield for Warframes; fire rate, magazine for weapons)
- Mastery rank requirements
- Polarities
- Wiki links and thumbnails
- And much more depending on the item type

By default, nested complex data (components, drops, patchlogs) shows a count only.

**With `--full` flag:**
- All nested data is expanded
- Component details with drop locations and relic chances
- Complete patchlog history (for Warframes)
- All nested properties and sub-properties

Example output:
- Default: `components: [5 items - use --full to see details]`
- With --full: Full component tree with names, descriptions, drop locations, chances, etc.

### Image URLs

All item images are available via CDN at:
```
https://cdn.warframestat.us/img/{imageName}
```

Note: Images are not included in the NPM package due to their size. They are hosted on the warframestat.us CDN for easy access.

## Technical Details

- Uses ES modules (`type: "module"` in package.json)
- Loads item data directly from the `@wfcd/items` package
- Supports loading specific categories to reduce memory usage
- Returns items with complete metadata including tradable status, unique names, and image references

