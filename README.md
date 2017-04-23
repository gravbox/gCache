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
