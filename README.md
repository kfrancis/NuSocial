# NuSocial
.NET MAUI Nostr Client

- [x] create basic .net lib
- [x] serialize event
- [x] deserialize event
- [x] proper disconnect
- [ ] handle profile images using blurhash
- [ ] handle tags
- [x] multi-relay support
- [ ] markdown parsing

## Thoughts

### Images
- [BlurHash](https://blurha.sh/) is how we'll store compact representations of profile images so they load immediately and we can then load them dynamically as they are seen.

![Current progress](/pic.png "Current progress")
