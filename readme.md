# Loudenvier.Utils

This is a collection of utilities I've been using for ages in my own C# programming. Some of it is great, some of it I'm ashamed of, most of it was written in the witching hours of the night, when ghosts roam and software crashes, but all of it is very useful (to me). Whenever I started a new project I ended up referencing "my" _utils_ collection so it was only natural to turn it into a (few) nuget(s) package(s), for easier deployment and referencing, and then upload it to github for version control (_actually it's been the other way around: organize, clean up and publish it to github and then publish to nuget_). As can be seen, _this is a work in progress!_

The code does not aim to be very smart, it aims to solve common and simple problems, apease to my tastes (which will likely differ from yours), to make some repetitive tasks simple and avoid common errors and pitfalls, and ultimately help me not (re)inventing the wheel time and time again. 

It does not aim for performance unless it specifically deals with performance sensitive code (like network-related code). Many parts were taken from other sources but most of these sources are long gone from (my) memory (I'm more careful to document on the sources I merge into it on more recent code)

Originally it was a monolithic project with many dependencies, but to publish it to nuget I've done some clean-up and organizing, segregating it into projects to only add "heavy" dependencies if you really need some specific functionality. The segregation was not by **function** (e.g.: date and time functions here, string functions there) but rather by the weight of the dependencies it would add to a referencing project. If some function added lots of references it ended up (or should) in a separate project/package.

I'm profusely thankful to [PolySharp](https://github.com/Sergio0694/PolySharp) for providing generated, source-only polyfills for C# language features that allow me to easily use all runtime-agnostic features at .NET Standard 2.0 (which this project targets). I really recommend it for all .NET developers.

## Documentation

I'm in the process of documenting everything using the amazing [docfx](https://github.com/dotnet/docfx) project. It takes time to properly add the needed XML comments for documentation, but I'm activelly working on it (as an added bonus Intellisense is much more useful now). Once documentation is done, it will be served from github pages, and only then the nugets will get published.
