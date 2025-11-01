How to:

1. store static data about the game
This is fixed data about items in the game.
Likely linked to a wiki article that fully describes the item (probably more than required)

- weapons
    - damage amounts
    - status applied
    - fire modes 
        - what they do, capabilities
        - e.g: cedo prime 100% status chance
    - unique traits
    - special relation to specific frames
- mods
    - element types
    - buffs
    - multipliers
    - how do you know if it applies as base damage, or scales after?
    - type (shotgun, pistol, rifle, melee, pet)
    - how to handle things like Razor: can pull mobs that you run through in to a ball, and focus fire them for X seconds
- exilus
- arcanes
- archwing
- arch-gun
- pets
    - special abilities
    - mods
- enemy
    - type
    - abilities
- warframes
    - abilities
        - duration
        - damage
        - cost
        - (Etc)?
    - unique traits

2. store rules about how things operate

- a high based status chance weapon: best to stack status on it
- cedo prime: can shoot secondary gauranteed status, so with that in mind...
- grineer susceptible to corrosive (weapons do 1.5x dmg), etc
- mods that group enemies together have value (Razor...), so if with that frame, this mod has more value.
    - then, assuming you're applying status, statsu has more value
    - but none of the above matters if you're not running that frame 

3. how would this be run as a complete system?
Asumming we have the information stored in relevant systems/dbs, how would the system run?
"plan modes" seem excellent for code, might they work here?
- first step is to build a plan to answer the users question
- then work step by step executing that plan, using available tools
- somehow generating a prompt that is used to finally answer the users q
- then perform some kind of analysis at the end


4. How to scrape this data, and build the underlying DB?
- warframe-items: https://github.com/WFCD/warframe-items - Fetches all items available on Warframe's mobile API endpoints while also adding images, drop rates, patch logs and related rivens.
    - this can get detailed static data on weapons, warframes

Questions: 
- how to know what damage is done by abilities? e.g: Razor Gyre: "...making them more vulnerable to all other status effects". more? how much more?
- Sentient Wrath: "Smash the ground sending out a radial wave of destruction" - how much dmg? how far?
