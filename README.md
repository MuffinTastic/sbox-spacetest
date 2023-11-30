# sbox-spacetest
Proof of concept for a "multiple dimensions" mechanic. Made using [s&box](https://sbox.facepunch.com/news)

Secret sauce:
- Uses the engine's scene system
- No cheap tricks - does **not** teleport the player to different locations. You're truly in a different dimension!
- Has custom entities that own multiple scene objects for rendering purposes
- Uses fun math to simplify detection for the player entering the portals
- Uses custom procedural sky shaders, with random values for each dimension

Seeing as it's likely broken in modern s&box versions, here's a video:

https://github.com/MuffinTastic/sbox-spacetest/assets/10884425/6106c07e-6a29-4c5f-adcb-dceb4576652c
