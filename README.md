# Dungeon Generator
This is a Dungeon Generator made in Unity. You can clone the unity project if you want to play around with the variables of the dungeon generation. You can change the variables in the Room object in the Dungeon Scene. Making it bigger than 500x500 will likely take more than a few seconds to generate the dungeon. You can also (un)tick some boxes if you want to visualize the individual steps of the generation.
Relevant code can be found in the /Assets/Scripts folder
## The Algorithm
The generator uses the BFS (breadth first search) algorithm to check and edit a node graph. The node graph is made using a generic type graph class that consists of a dictionary that holds the generic type nodes as keys and the edges connected to the node as values of that key.
The algorith itself goes through some steps to generate the dungeon. First It takes a RectInt with a certain starting size and splits that RectInt randomly. It then generates the nodes for the rooms simply by taking the center of the room. Using the overlap of different rooms, it can then determine where possible locations are for doorsways inbetween the rooms and it places doorway nodes accordingly. Then it will get rid of the 10 smallest rooms as long as it does not break the graphs connection, and makes sure there are no cyclic paths to be found in the dungeon. Then as the last step it spawns the dungeons prefabs (a cube and a quad) as walls and floors in the right places.
<p align="center">
  <img height="300" alt="roomSplitting"
       src="https://github.com/user-attachments/assets/8887c030-0eec-4192-937f-04caf61394af" />
  <img height="300" alt="nodeGeneration"
       src="https://github.com/user-attachments/assets/bc7185a6-f672-44c2-91ab-0aa6b65e1884" />
  <img height="300" alt="removedCyclesAndGeneratedDungeon"
       src="https://github.com/user-attachments/assets/6f716319-a523-4041-81a5-35e7ed6ce8d3" />
</p>

<p align="center">
  <em>Left to right: room splitting, node generation, final dungeon</em>
</p>
This was a solo school project where I learned more about node graphs, algorithms used in game development, and algorithm classifications using the O notation to generate a non cyclic dungeon. Studied algorithms include the popular graph search algorithms BFS (Breadth first search) and DFS (Depth First Search). It was very satisfying to see how much faster an algorithm can get after applying small changes like changing variable types.
