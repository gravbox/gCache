gCache
========

This is a memory based caching system written in C#. It is windows service that will persist objects in memory for fast access across a network. Simply add the common library to your project to access the service layer with a few lines of code. Objects will be serialized and stored on the host server. The service also has options to encrypt and compress objects in cache.

The syntax is simple. Create a cache object pointing to the host machine and port, then add or retrieve objects.


	using (var cache = new CacheService<TestItem>("localhost", 7373))
	{
		var theItem = new TestItem();
		cache.AddOrUpdate("zz", theItem);
		var newItem = cache.Get("zz");
	}

## Installation
To install the service, simply run .NET framework "InstallUtil.exe" application with the service executable. There are two batch files in the service project named “InstallService.bat” and “UninstallService.bat” that will install and uninstall the Windows service.

## Performance
My tests on an HP Z600 running the service locally shows very good results. Creating random 500 character strings and storing it asynchronously adds about 5,500 to 6,000 objects per second for a insertion rate of about 160-180 μs.

The access statistics were virtually the same with  objects being retrieved about 5,600-5,800 objects per second for a retrieval rate of about 170-180 μs. Both tests include the full cycle of object serialization, transport, and storage. The network did not play any effect in these tests as the test app and service were on the same machine. In a distributed environment, the network latency would add some time.
