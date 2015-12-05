# JustEat.StatsD

## The point

### TL;DR

We use this within our components to publish [statsd](http://github.com/etsy/statsd) metrics from .NET code. We've been using this in most of our things, since around 2013.

### Features

* statsd metrics formatter
* UDP client handling

#### Publishing statistics

`IStatsDPublisher` is the interface that you will use in most circumstances. With this you can `Increment` or `Decrement` an event, and send values for a `Gauge` or `Timing`.

The concrete class that implements `IStatsDPublisher` is `StatsDImmediatePublisher`. For the constructor parameters, you will need the statsd server host name. You can change the standard port (8125). You can also prepend a prefix to all stats. These values often come from configuration as the host name and/or prefix may vary between test and production environments.

#### Example of setting up a StatsDPublisher

An example of Ioc in NInject for statsd publisher with values from configuration:
```csharp
	string statsdHostName =  ConfigurationManager.AppSettings["statsd.hostname"];
	int statsdPort = int.Parse(ConfigurationManager.AppSettings["statsd.port"]);
	string statsdPrefix =  ConfigurationManager.AppSettings["statsd.prefix"];
		
	Bind<IStatsDPublisher>().To<StatsDImmediatePublisher>()
        .WithConstructorArgument("cultureInfo", CultureInfo.InvariantCulture)
		.WithConstructorArgument("hostNameOrAddress",statsdHostName)
        .WithConstructorArgument("port", statsdPort)
        .WithConstructorArgument("prefix", statsdPrefix);

```

#### Example of using the interface

Given an existing instance of `IStatsDPublisher` called `stats` you can do for e.g.:

```csharp
		stats.Increment("DoSomething.Attempt");
		var stopWatch = Stopwatch.StartNew();
        var success = DoSomething();

		stopWatch.Stop();
		if (success)
        {
			stats.Timing("DoSomething.Success", stopWatch.Elapsed);
		}
```

#### Simple timers

This syntax for timers less typing in simple cases, where you always want to time the operation, and always with the same stat name. Given an existing instance of `IStatsDPublisher` you can do:

```csharp
    //  timing a block of code in a using statement:
   using (stats.StartTimer("someStat"))
   {
      DoSomething();
   }

   // also works with async
    using (stats.StartTimer("someStat"))
    {
        await DoSomethingAsync();
    }
```
 
The `StartTimer` returns an `IDisposable` that wraps a stopwatch. The stopwatch is automatically stopped and the metric sent when it falls out of scope on the closing `}` of the `using` statement.

##### Functional style

```csharp
   //  timing a lambda without a return value:
   stats.Time("someStat", () => DoSomething());

    //  timing a lambda with a return value:
    var result = stats.Time("someStat", () => GetSomething());

    // and correctly times async lambdas using the usual syntax:
    await stats.Time("someStat", async () => await DoSomethingAsync());
    var result = await stats.Time("someStat", async () => await GetSomethingAsync());
    
```

##### Advanced simple timers

Sometimes the descisions about sending the stat can't be taken before the operation completes. e.g. When you are timing http operations and want different status codes to be logged under different stats.

The timer has a `StatName` property to set or change the name of the stat, and a method `Cancel` to not send the timer at all. To use these you need a reference to the timer, e.g. `using (var timer = stats.StartTimer("statName"))` instead of `using (stats.StartTimer("statName"))`

The stat name can be blank initially since it can be set later. The stat will only be sent if the stat name is not blank at the end of the `using` block.

```csharp
   using (var timer = stats.StartTimer())
   {
      var response = DoSomeHttpOperation();
	  timer.StatName = "HttpOperation." + response.StatusCode;
   }
   
   using (var timer = stats.StartTimer("DoSomething.Success"))
   {
		var success = DoSomething();
		if (! success)
		{
			timer.Cancel();
		}		
   }
```

The idea of "disposable timers" for using statements comes from [this StatsD client](https://github.com/Pereingo/statsd-csharp-client).


### How to contribute

See [CONTRIBUTING.md](CONTRIBUTING.md).

### How to release
See [CONTRIBUTING.md](CONTRIBUTING.md).

