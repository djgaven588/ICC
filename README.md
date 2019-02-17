# ICC
ICC, short for Internet Chat Communication, was built from the ground up to allow for easy implementation, extreme flexibility, and a simple protocol.

ICC is a text chat protocol which can be used to create anything from simple client -> server -> client communication, to making the next Discord.

This repo contains guides on what you need to implement to make your own client or server, and an example made in C# that you can use as a start.
Here are some things that you can create using ICC:
-Discord -> ICC -> Discord Communication, using a Discord bot and a ICC server, you can integrate ICC into Discord. If your one stubborn friend won't use ICC, they can still comfortably use Discord while you enjoy ICC. Have Discord open and want to chat with your friend on ICC? Easy, you can make it work both ways!
-Your own version of Discord, using a customized client and server, you can create channels, roles, guilds, and all of that kind of fun stuff. The server can manage users, messages, channels, roles, and such, while the client can automatically show links, upload files, look through channels and their history, and all of that fun stuff.

ICC is a foundation for you to build amazing things with. With your imagination being the skies limit, this is where the fun begins. All clients and servers will be able to work with each other as long as you implement ICC. Creating your own Discord and someone with a console joins your servers? Not to worry, they should work completely normally. Vise versa when someone is using your client and joins a random server, the client will work completely normally.

Obviously there are some limitations with different clients and servers, like if you tried to make a Discord like UI and you joined a server which mainly operated with console clients, you may not have a friends list, channel lists, or chat history. You can however implement ways for your client or server to handle those cases better. Your Discord like client could actually simplify the UI in order to match the server.

Let's create something great.
