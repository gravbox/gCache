gCache
========

This is a memory based caching system written in C#. It is windows service that will persist objects in memory for fast access across a network. Simply add the common library to your project to access the service layer with a few lines of code. Objects will be serialized and stored on the host server. The service also has options to encrypt and compress objects in cache.

The syntax is simple. Create a cache object pointing to the host machine and port, then add or retrieve objects.

	using (var cache = new CacheService<TestItem>("localhost", 7373))
	{
		//Keep the item in cache forever
		var theItem = new TestItem();
		cache.AddOrUpdate("zz", theItem);
		var newItem = cache.Get("zz");
		
		//Keep the object in cache until Jan 1, 2018
		var expireOn = new DateTime(2018, 1, 1);
		cache.AddOrUpdate("Key1", theItem, expireOn);
		
		//Keep the item in cache for 10 minutes
		var expireIn = new TimeSpan(0, 10, 0);
		cache.AddOrUpdate("Key2", theItem, expireIn);		
	}
	
	//Create a cache connection for a container named "MyContainer"
	//Only objects in this container will be returned with the Get operation.
	//You can use the same key in different containers to store multiple objects.
	//Containers keep your object in a separate virtual repository
	using (var cache = new CacheService<TestItem>(container: "MyContainer"))
	{
		//This new object will be stored with the key "zz" just like the code above 
		//but will not remove it since this is in a different container.
		var theItem = new TestItem();
		cache.AddOrUpdate("zz", theItem);
	}
	
	//Create a strongly-typed string cache
	using (var cache = new CacheService<string>())
	{
		cache.AddOrUpdate("yy", "Hello");
	}

## Installation
To install the service, simply run .NET framework "InstallUtil.exe" application with the service executable. There are two batch files in the service project named “InstallService.bat” and “UninstallService.bat” that will install and uninstall the Windows service.

## Performance
My tests on an HP Z600 running the service locally shows very good results. Creating random 500 character strings and storing it asynchronously adds about 5,500 to 6,000 objects per second for a insertion rate of about 160-180 μs.

The access statistics were virtually the same with  objects being retrieved about 5,600-5,800 objects per second for a retrieval rate of about 170-180 μs. Both tests include the full cycle of object serialization, transport, and storage. The network did not play any effect in these tests as the test app and service were on the same machine. In a distributed environment, the network latency would add some time.
