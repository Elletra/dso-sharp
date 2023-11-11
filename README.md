# DSO Sharp

**Do not complain about how it doesn't work or ask how to get it to work; I will not respond.** I am just sharing my code. If you don't know how to get it to work, this repo isn't for you.

This is a ***work-in-progress*** DSO decompiler for the Torque Game Engine. It currently does not work properly and is in a very rough state. It also is only for Blockland v21 at the moment.

**Please read the _whole_ readme—it's important.**


## Background

Years ago, I made [dso.js](https://github.com/Electrk/dso.js), a DSO decompiler for Blockland v21. It was very bad and used absolutely no computer science concepts whatsoever. However, it (mostly) worked, and was the only (mostly) working, publicly-available DSO decompiler for Blockland, so it was okay for the time.

I was not satisfied with its quality, and the fact that it was hardcoded to only work for Blockland v21. I sought to write it better, use actual computer science concepts, and make it so that it could work with other games/engine versions.

Thus, in 2022, I started work on _DSO Sharp_.


## Disaster

I worked on it off and on for about a year—I would work on it heavily for 1-3 months, hit a wall, get burnt out, and then take a break. I did this repeatedly.

I _agonized_ over the project until August 2023, when I thought it was _finally_ almost ready for release. As I was working on getting it ready, I found out at basically the eleventh hour that it was producing wrong output and that I probably needed to rethink my whole approach. I had been working on it pretty nonstop for several months at this point, so I was already getting burnt out. Finding this out completely deflated any motivation I had to continue.


## Now (November 2023)

I haven't touched it since August. It doesn't work properly, there's a bunch of shite debug code, it's unfinished, and it only works for Blockland v21 still. But I decided to release it as-is, because hopefully it can be helpful to the community somehow.

As much as my ego would love to be The Woman Who Wrote The Decompiler™, I've always believed sharing knowledge is important and that people who hoard it are a cancer to humanity.

Reverse engineering and decompilation are inherently about sharing and democratizing knowledge. Anyone involved in a reverse engineering or decompilation project who doesn't open source their work is everything wrong with programming. They should kill their ego and open source it. It is antithetical to the very concepts of reverse engineering and decompilation. The only valid reason for not open sourcing is fear of legal issues. Anything else is just selfish, ego-stroking nonsense or corporate-style information hoarding.

So, I am swallowing my pride and releasing this project, in all its ugly, unfinished glory. It's not finished, and its code is of varying quality (as the project went on, I just wanted it done, so code quality got ***real bad***), but maybe it can be of use to someone.

Feel free to fork it (and maybe even submit a pull request!) as long as you don't mess with the license. I have licensed this project under the BSD 3-Clause License. Please check `LICENSE` for more details.

Hopefully I or someone else can finish it someday. This project has been my white whale for almost four years now, and I would love to see it complete.

Maybe someday...


## Important Notes

The C# heads reading the code will see that almost everything is `protected` instead of `private`. This is due to the goal of making it compatible with different games and engine versions. The general idea is that there is a base class that subclasses can override for game/engine-specific functionality. I wanted to make it modular. This may be a bad way to do that, but I couldn't think of any other way—please understand. I apologize sincerely.

As mentioned in the previous section, the code quality suffered greatly as time went on. At a certain point I just wanted it _done_. It's actually, funny enough, roughly in the order that the program is supposed to run: reader, loader, and disassembler are all pretty great quality, but from the control flow stuff onward it just gets worse and worse.

Also, `Program.cs` makes some calls to debug programs and files that I didn't include in the repo. You can remove these calls.


## References

These are the papers I referenced while writing this decompiler:

- "A Simple, Fast Dominance Algorithm" by Keith Cooper, Timothy Harvey, and Ken Kennedy.
[https://www.cs.tufts.edu/comp/150FP/archive/keith-cooper/dom14.pdf](https://www.cs.tufts.edu/comp/150FP/archive/keith-cooper/dom14.pdf)

- "Native x86 Decompilation Using Semantics-Preserving Structural Analysis and Iterative Control-Flow Structuring" by Edward J. Schwartz, JongHyup Lee, Maverick Woo, and David Brumley.
[https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf](https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf)
