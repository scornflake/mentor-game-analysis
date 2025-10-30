# TODO

# Improving
- Get job status working
- Make perplexity/travily/brave "hidden" from the user settings.
  - put in a "settings" panel, that has spaces for their API keys
- Put "date" into the query (so LLM knows to look for recent info)
- For search, are there well known "warframe" sites where we get better, more accurate info?
- Allow LLM configuration to specify which search engines to use
  - validate those engines are setup ok
- Get analysis of image as it's own output
[x] - Do tool search first, web
- Combine both the image analysis and tool search results into one prompt for the LLM
- OpenTel tracing?
- Add google as search engine option

- Evaluation
  - How to look at the outputs at each step?

# Improving - doing a plan
- Make a step that makes a plan to complete the thing (json with steps)
  - tell it the goal
  - tell it what tools it has
- Then have steps that do each part of the plan

# Agentic Flow: Error Analysis
For analysis, measure those areas that are performing poorly
  - Image analysis - maybe the description is just plain bad
  - Web search results relevance - maybe it's using wrong sites, or results are not authoritative enough
  - LLM final output relevance  
  - Make a spreadsheet to track inputs, each step output, final output, and notes on quality (what is wrong/bad)
    - columns for each stage, and comments in columns that have bad data
    - count each thing that's wrong, so we can see which stage is causing the most problems

- Columns
  - Image description
  - Web search results
  - LLM final output

# Component Level Evals
In below, it would be nice to be able to spit out a score. 
We could then put into SS to track (would need a description of what we changed)?

- Image description
   - Use known images, where we know what is in it (use some other tool to generate)
   - Compare actual description to expected description (some kind of similarity score)
     - maybe use another LLM to do the comparison?
     - maybe use embedding similarity?
-  Websearch
   - Find a "gold standard" set of queries and expected results given some search 
   - Do search, compare actual results to gold standard (F1 score)
     - Vary date rages, search, etc, you can then measure results
   - Maybe do this as a manual test

- Pass the mime type of the image through with prompt, so we can correctly tell the LLM what kind of image it is 
- How to verify that a local LLM is actually using the results of a web search [tool use]
- Probably bring configuration out to its own persisted JSON?
  - Alternative - editing in the app, just writes out the appsettings.json itself.

- MCP, and does that make "tool use" obsolete? (how to add websearch as MCP)
  - Agent framework appears to rely on MCP
  
- MS Agent framework
  - Allow say the image parsing part (describe the picture) to use a different (better, more accurate) model.