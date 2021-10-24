[![Discord](https://img.shields.io/discord/809535064551456888.svg)](https://discordapp.com/invite/DTBPBYvexy)
[![release](https://img.shields.io/github/release/Miragenet/ObjectPooler.svg)](https://github.com/MirageNet/ObjectPooler/releases/latest)
[![Build](https://github.com/MirageNet/ObjectPooler/workflows/CI/badge.svg)](https://github.com/MirageNet/ObjectPooler/actions?query=workflow%3ACI)
[![GitHub issues](https://img.shields.io/github/issues/MirageNet/ObjectPooler.svg)](https://github.com/MirageNet/ObjectPooler/issues)
![GitHub last commit](https://img.shields.io/github/last-commit/MirageNet/ObjectPooler.svg) ![MIT Licensed](https://img.shields.io/badge/license-MIT-green.svg)
[![openupm](https://img.shields.io/npm/v/com.miragenet.objectpooler?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.miragenet.objectpooler/)

## Installation

If you are using unity 2019.3 or later: 

1) Open your project in unity
2) Install [Mirage](https://github.com/MirageNet/Mirage) | [Mirror](https://assetstore.unity.com/packages/tools/network/mirror-129321)
3) Click on Windows -> Package Manager
4) Click on the plus sign on the left and click on "Add package from git URL..."
5) enter https://github.com/MirageNet/ObjectPooler.git?path=/Assets/Object%20Pooler
6) Unity will download and install ObjectPooler
7) Make sure .NET 4.X set for compiler level.

## Usages

### [Standard Usage]

Attach ObjectPoolingManager script to network object where all other mirage components are. Once attached nothing else will need to be done. 
Client side will now start object pooling for anything that is called using ServerObjectManager.Spawn();

### [Advance Usage]

#### [Server]

if you want to pool on server you can call specific commands to do so. Command below will instiante and auto spawn the object for you and return back to you
the object if you want to do further things with it. You need to pass in a position and rotation and the mirage assetid from the prefab.

`
ObjectPoolingManager.NetworkSpawnPool(Vector3.zero, Quaternion.identity, prefab.AssetId);
`

if you want to unspawn from server you can call this command. You need to pass in the object you want to unspawn. Also whether this is being called on server
or not on client. If you are inside of a networkbehaviour simply using isServer there will work best.

`
ObjectPoolingManager.NetworkSpawnPool(objectSpawned, true);
`

#### [Client]

if you need to do any client side rpc and also want to be able to still pool while say calling an rpc and show off a particle effect you can
call these commands to spawn and pool or unspawn back to pool using local methods of pooling manager. There are few overloads to use.

The reason I designed this using T is because this will allow pooler to dump back to pooling system regardless of type. The only gotcha with local
spawning of objects you must specific the correct T type or else things will break.

`
ObjectPoolingManager.LocalSpawnPool<Sprite>(objectSpawned, Vector3.zero, Quaternion.identity);
ObjectPoolingManager.LocalSpawnPool(objectToSpawn, transform, true);
`

`
ObjectPoolingManager.LocalUnSpawnObject(spawnedObject);
`

## Contributing

There are several ways to contribute to this project:

* Pull requests for bug fixes and features are always appreciated.
* Pull requests to improve the documentation is also welcome
* Make tutorials on how to use this
* Test it and open issues
* Review existing pull requests
* Donations

When contributing code, please keep these things in mind:

* [KISS](https://en.wikipedia.org/wiki/KISS_principle) principle. Everything needs to be **as simple as possible**. 
* An API is like a joke,  if you have to explain it is not a good one.  Do not require people to read the documentation if you can avoid it.
* Follow [C# code conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions).
* Follow [SOLID principles](https://en.wikipedia.org/wiki/SOLID) as much as possible. 
* Keep your pull requests small and obvious,  if a PR can be split into several small ones, do so.
