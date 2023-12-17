# SeamlessClientPlugin (Re-write)
The seamless client plugin lets you switch between SE servers without a loading screen. This currently *only* works with Nexus compatible servers as all data is shared and synced between them.

The main load time is the time it takes for your client to ping the destination server, and the client to recieve the go-ahead. Any extra time is contributed to entities syncing to the client similar to if you were respawning at the grid. (sometimes it takes forever). I will be looking into pre-loading synced entities to the client in the near future, but atm this was the easier solution.

This has taken countless hours of testing and debugging to get right. Not to mention the countless hours implementing the server plugin Nexus. If you enjoy this kind of work, please donate [here](https://se-nexus.net/en/Contribute) to help keep this project alive.



## How it works
With Nexus servers, all data is shared between servers. (Factions, Identities, Players, Econ etc) This is a huge benefit as we dont have to go in and reload all identities and factions etc. The next thing that happens is that the server tells the client to switch to the proper server. It then goes in and just re-applies the MyMultiplayerClient to the target server. Of course there is a few other things that must happen to fix any errors or bugs, but that is the main rundown.



## How to install
Simply install the plguin loader, and check this plugins box to be added to the plugin loaders' active plugin list. (SE will need to be restarted afterwards)



## Known issues
Obviously this is not an issue free-system. Currently since im doing no mod unloading or loading there could be issues if your servers dont have the exact same mods, or the mods dont properly work right. Please do not swarm mod authors with faults if seamless doesnt play nice with it. ***Its not their fault*** its ***mine***. I will be trying to implement mod unloading and loading switching between servers, just no ETA.
