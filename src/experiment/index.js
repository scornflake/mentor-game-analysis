import Items from '@wfcd/items';
import fs from 'fs';

// Parse command line arguments
function parseArgs() {
  const args = process.argv.slice(2);
  const options = {
    search: null,
    category: null,
    tradable: null,
    limit: null,
    lookup: null,
    full: false,
    write: false,
  };

  for (let i = 0; i < args.length; i++) {
    switch (args[i]) {
      case '--search':
      case '-s':
        options.search = args[++i];
        break;
      case '--category':
      case '-c':
        options.category = args[++i];
        break;
      case '--tradable':
      case '-t':
        options.tradable = true;
        break;
      case '--not-tradable':
        options.tradable = false;
        break;
      case '--limit':
      case '-l':
        options.limit = parseInt(args[++i]);
        break;
      case '--lookup':
      case '-u':
        options.lookup = args[++i];
        break;
      case '--full':
      case '-f':
        options.full = true;
        break;
      case '--write':
      case '-w':
        options.write = true;
        break;
      case '--help':
      case '-h':
        showHelp();
        process.exit(0);
        break;
    }
  }

  return options;
}

// Show help message
function showHelp() {
  console.log(`
Warframe Items CLI - Search and filter Warframe items

Usage: node index.js [options]

Options:
  -s, --search <term>      Search items by name (case-insensitive)
  -u, --lookup <unique>    Lookup item by unique name (shows item's own stats)
      --unique <unique>    (same as --lookup)
  -f, --full               Show ALL properties including components/drops/patchlogs (use with --lookup)
  -c, --category <type>    Filter by category (e.g., Warframes, Melee, Primary)
  -t, --tradable           Show only tradable items
  --not-tradable           Show only non-tradable items
  -l, --limit <number>     Limit number of results (default: show all)
  -w, --write              Write item names to a file (category.txt or all.txt)
  -h, --help               Show this help message

Categories:
  All, Arcanes, Archwing, Arch-Gun, Arch-Melee, Enemy, Fish, Gear, 
  Glyphs, Melee, Misc, Mods, Node, Pets, Primary, Quests, Relics, 
  Resources, Secondary, Sentinels, SentinelWeapons, Sigils, Skins, Warframes

Examples:
  node index.js --search "excalibur" --category "Warframes"
  node index.js --search "prime" --tradable
  node index.js --category "Mods" --limit 10
  node index.js --lookup "/Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon"
  node index.js --lookup "/Lotus/Weapons/Tenno/LongGuns/PrimeCedo/PrimeCedoWeapon" --full
  `);
}

// Filter items based on search criteria
function filterItems(items, options) {
  let results = [...items];

  // Filter by search term
  if (options.search) {
    const searchLower = options.search.toLowerCase();
    results = results.filter(item => 
      item.name && item.name.toLowerCase().includes(searchLower)
    );
  }

  // Filter by category
  if (options.category) {
    const categoryLower = options.category.toLowerCase();
    results = results.filter(item => 
      item.category && item.category.toLowerCase() === categoryLower
    );
  }

  // Filter by tradable status
  if (options.tradable !== null) {
    results = results.filter(item => item.tradable === options.tradable);
  }

  return results;
}

// Format and display an item
function displayItem(item, index) {
  console.log(`\n${index + 1}. ${item.name || 'Unknown'}`);
  console.log(`   Type: ${item.type || 'N/A'}`);
  console.log(`   Category: ${item.category || 'N/A'}`);
  console.log(`   Tradable: ${item.tradable ? 'Yes' : 'No'}`);
  
  if (item.uniqueName) {
    console.log(`   Unique Name: ${item.uniqueName}`);
  }
  
  if (item.imageName) {
    const imageUrl = `https://cdn.warframestat.us/img/${item.imageName}`;
    console.log(`   Image: ${imageUrl}`);
  }
}

// Display all properties of an item in detail
function displayItemDetail(item, showFull = false) {
  console.log('\n' + '='.repeat(80));
  console.log(`Item: ${item.name || 'Unknown'}`);
  console.log('='.repeat(80));
  
  // Properties to skip in non-full mode (nested complex data)
  const skipInNonFull = ['components', 'patchlogs', 'drops'];
  
  // Format and display all properties
  const displayObject = (obj, indent = 0, parentKey = '') => {
    const indentStr = '  '.repeat(indent);
    
    for (const [key, value] of Object.entries(obj)) {
      if (value === null || value === undefined) {
        continue; // Skip null/undefined values
      }
      
      // Skip complex nested properties in non-full mode
      if (!showFull && indent === 0 && skipInNonFull.includes(key)) {
        console.log(`${indentStr}${key}: [${Array.isArray(value) ? value.length : 'nested'} ${Array.isArray(value) ? 'items' : 'data'} - use --full to see details]`);
        continue;
      }
      
      if (Array.isArray(value)) {
        if (value.length === 0) continue; // Skip empty arrays
        
        // In non-full mode, limit nested array displays
        if (!showFull && indent > 0 && value.length > 0 && typeof value[0] === 'object') {
          console.log(`${indentStr}${key}: [${value.length} items - use --full to see details]`);
          continue;
        }
        
        console.log(`${indentStr}${key}:`);
        value.forEach((item, index) => {
          if (typeof item === 'object' && item !== null) {
            console.log(`${indentStr}  [${index}]:`);
            displayObject(item, indent + 2, key);
          } else {
            console.log(`${indentStr}  [${index}] ${item}`);
          }
        });
      } else if (typeof value === 'object' && value !== null) {
        console.log(`${indentStr}${key}:`);
        displayObject(value, indent + 1, key);
      } else {
        // Special formatting for image names
        if (key === 'imageName') {
          const imageUrl = `https://cdn.warframestat.us/img/${value}`;
          console.log(`${indentStr}${key}: ${value}`);
          console.log(`${indentStr}imageUrl: ${imageUrl}`);
        } else {
          console.log(`${indentStr}${key}: ${value}`);
        }
      }
    }
  };
  
  displayObject(item);
  console.log('\n' + '='.repeat(80));
  
  if (!showFull) {
    console.log('Tip: Use --full flag to see all nested details (components, drops, patchlogs, etc.)');
  }
}

// Write items to a file
function writeItemsToFile(items, category) {
  const filename = category ? `${category.toLowerCase()}.txt` : 'all.txt';
  const names = items.map(item => item.name || 'Unknown').join('\n');
  
  try {
    fs.writeFileSync(filename, names + '\n', 'utf8');
    console.log(`\nWrote ${items.length} item name(s) to ${filename}`);
  } catch (error) {
    console.error(`\nError writing to file: ${error.message}`);
  }
}

// Main function
function main() {
  try {
    const options = parseArgs();
    
    console.log('Loading Warframe items...\n');
    
    // Initialize Items - load specific category if specified, otherwise load all
    const itemsConfig = options.category 
      ? { category: [options.category] }
      : { category: ['All'] };
    
    const items = new Items(itemsConfig);
    
    console.log(`Loaded ${items.length} items\n`);
    
    // Handle lookup mode - find by unique name and show all details
    if (options.lookup) {
      const item = items.find(item => item.uniqueName === options.lookup);
      
      if (!item) {
        console.log(`No item found with unique name: ${options.lookup}`);
        console.log('\nTip: Use --search to find items by name first, then use --lookup with the exact uniqueName.');
        process.exit(1);
      }
      
      displayItemDetail(item, options.full);
      return;
    }
    
    // Filter items based on criteria
    let results = filterItems(items, options);
    
    // Limit results
    const totalResults = results.length;
    if (options.limit !== null) {
      results = results.slice(0, options.limit);
    }
    
    // Display results
    if (results.length === 0) {
      console.log('No items found matching your criteria.');
    } else {
      console.log(`Found ${totalResults} item(s) (showing ${results.length}):\n`);
      console.log('='.repeat(80));
      
      results.forEach((item, index) => {
        displayItem(item, index);
      });
      
      console.log('\n' + '='.repeat(80));
      
      // Write to file if --write flag is set
      if (options.write) {
        writeItemsToFile(results, options.category);
      }
      
      if (options.limit !== null && totalResults > options.limit) {
        console.log(`\n(${totalResults - options.limit} more results not shown. Use --limit to see more)`);
      }
    }
    
  } catch (error) {
    console.error('Error:', error.message);
    process.exit(1);
  }
}

main();

