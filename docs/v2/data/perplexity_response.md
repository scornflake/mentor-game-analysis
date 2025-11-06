When modeling relationships in a graph database for your Warframe example, the key is to treat relationships as first-class, explicit entities with meaningful properties rather than trying to force them into simplistic or fixed categories. You don't necessarily have to fully normalize relationships like in relational databases, but you do want to model them clearly to enable the queries you want.

### Modeling Relationships in Graph Databases

- In graph databases, entities like guns, mods, and status effects are nodes.
- Relationships are directed edges with labels like "INCREASES" or "CAUSES".
- Crucially, relationships themselves can have properties—e.g., the exact increase amount (+58.2% Status Chance), condition (e.g., "On Kill"), duration, stacking behavior.
- This allows complex and context-rich connections without flattening them into simple columns or having to guess what a single relationship means.
- For example, a mod node connects to a gun node with an "INCREASES_STATUS_CHANCE" edge, carrying properties for both the base increase and the conditional on-kill damage buff with stacking rules.

### Handling Complex and Conditional Effects

- You can model each effect as a separate relationship or even as its own effect node connected to the mod, which then relates to the gun or status attribute.
- Conditional effects like "On Kill: +29.1% Damage per Status Type" can have properties for condition ("On Kill"), effect magnitude, duration, and stacking.
- This schema flexibility exploits the graph database’s strength in representing nuanced relationships naturally.

### Querying Based on Relationships

- Queries like "show me mods that increase status chance" can traverse relationships labeled with relevant effect types.
- Because effects are explicit and carry properties, you can filter exact conditions or aggregate stacking effects without ambiguity.

### Normalization in Graph Databases

- Unlike in relational databases where normalization reduces redundancy, graph databases favor modeling relationships fully and explicitly.
- Over-normalizing (splitting fine-grained relationships into too many nodes) can hurt query simplicity.
- Instead, organize around relationship semantics important for your queries.
- Properties on relationships can prevent unnecessary expansion of nodes.

### Summary

- Nodes represent entities (guns, mods, status types).
- Relationships connect entities, have types (e.g., "INCREASES_STATUS_CHANCE") and rich properties (amount, condition, stacking).
- Complex mod effects can be modeled as multiple relationships or intermediate effect nodes.
- This direct, property-rich relationship modeling is typical and effective in graph databases.
- You do not have to normalize relationships as strictly as in relational DBs but should model clearly around query goals.

This approach enables you to ask complex questions like "show mods that increase status chance," while also capturing detailed mod behaviors and stacking mechanics naturally in the graph structure.[1][2][3][4]

[1](https://milvus.io/ai-quick-reference/how-do-graph-databases-handle-relationships-between-data-points)
[2](https://memgraph.com/blog/relational-to-graph)
[3](https://memgraph.com/blog/optimizing-graph-databases-through-denormalization)
[4](https://www.linkedin.com/pulse/graph-data-modeling-normalization-ayad-aliomer)
[5](https://dataengine.co.nz/blog/the-power-of-graph-databases/)
[6](https://www.ijset.in/graph-databases-for-modeling-complex-relationships/)
[7](https://learn.microsoft.com/en-us/fabric/graph/graph-relational-databases)
[8](https://www.reddit.com/r/learnprogramming/comments/1f94m59/good_solutions_for_storing_graph_database/)
[9](https://www.puppygraph.com/blog/graph-data-modeling)
[10](https://memgraph.com/blog/memgraph-storage-modes-explained)