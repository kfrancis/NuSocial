# NuSocial

.NET MAUI Nostr Client

- [x] create basic .net lib
- [x] serialize event
- [x] deserialize event
- [x] proper disconnect
- [x] multi-relay support
- [x] blocking relays
- [x] blocking users
- [ ] handle profile images using blurhash
- [ ] handle tags
- [ ] markdown parsing
- [ ] eula

## NostrKey

Simple console app, should be able to run anywhere .NET 7 can.

### Features

- [x] generates diff 0 keys
- [ ] generating keys with prefixes
- [ ] publish as dotnet tool
- [ ] meaningfully generating keys with varying difficulty

#### Example

Running `NostrKey 0` gives the following type of output:
```
Started mining process with a difficulty of: 0
Benchmarking a single core for 5 seconds...
A single core can mine roughly 2704.92 h/s!
Public Key: 70c847ff3522ca7a626c0ff280db2e8dfea63e6425580c3aa55530921cc40e4d
Private Key: 303298dd83b8db5d56a4e8901107a8bf88a5706e82177c0ed9d9af3eed579f3a
```

## NostrLib

Based on NNostr, but I couldn't quite understand how it was meant to be used so I've written a new one.

### Features

- [x] connect
- [x] disconnect
- [x] fetch events of varying filters
- [x] get profiles
- [x] get global posts
- [x] get individual feed
- [x] NIP-04 encrypt/decrypt
- [ ] handle the varying way markdown is packed into profiles

## Thoughts

### Images
- [BlurHash](https://blurha.sh/) is how we'll store compact representations of profile images so they load immediately and we can then load them dynamically as they are seen.

![Current progress](/pic.png "Current progress")
