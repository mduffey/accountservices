NOTE: if you decide to pull this down and run it, note that it's a pretty old WCF-style self-hosted service. You'll need to run it as an admin. No Swagger either. Sorry! Hadn't heard of it in 2012.

This is an application I wrote for a coding test when I applied for a Blizzard role ages ago. I found it lurking in some old data and decided to preserve it by putting it on GitHub, since I still really like the code and what it did.

This is pretty old school now (because 2012 counts as ancient history in web service design...), but I think it did a lot of clever things for a simple coding sample. Heck, if you needed a basic MUD service and didn't expect to go past a few thousand users, it'd probably work just fine! Until Blizzard sued you for using Worgens, anyways.

There are some things I'd change, WCF aside. The worst one is the use of dynamic objects in the deserialization. I think I wrote this before I'd become familiar with NuGet, so I used System.Web instead of Newtonsoft for JSON serialization/deserialization to keep third party libraries out of it. Since that was preventing this from running, I replaced it with Newtonsoft, and did the bare minimum to make it work by using dynamics. Pretty sure just having Newtonsoft deserialize into the objects instead would work just fine.

Also the deep embedding of tests into the core service project. I think I had it in mind that if this were a 'real' thing, I'd probably build out a separate, proper hosting project and simply utilize the library, which has all the real functionality anyways, and keep the Host project around as an integration test.

There's also a concern of trusting input from a client (the input is sanity-tested, but the client can claim to be whatever level they want), but I think that was an intentional oversight; the parameters of the sample application probably specifically called out that we weren't supposed to handle that particular challenge.