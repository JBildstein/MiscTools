# MiscTools
A collection of various more or less useful classes.

## Classes

### CR2ThumbReader
This static class can extract the embedded thumbnail from a Canon Raw file (CR2). The thumbnail should be 160x120 pixels in size.

```csharp
Bitmap thumb = CR2ThumbReader.GetThumb("file.CR2");
//Or if you want just the jpeg encoded data:
byte[] data = CR2ThumbReader.GetThumbData("file.CR2");
```
Alternatively you can also pass a `System.IO.Stream` into both methods.

### NATimer
NA stands for Non-Accumulating, which means this timer will fire the `Tick` event compared to when it started and not compared to the last `Tick` event.

Not clear what this should mean? Look at this table and understand. The timers are set to fire an event every second. The numbers listed here is the time since the start of the timer. 

|System.Threading.Timer|MiscTools.NATimer|
|:--------------------:|:---------------:|
|1.0135s|0.9950s|
|2.0215s|1.9999s|
|3.0415s|2.9918s|
|4.0525s|3.9939s|
|5.0664s|4.9958s|
|6.0785s|5.9978s|
|7.0944s|6.9928s|
|8.1044s|7.9978s|
|9.1174s|9.0008s|
|10.1394s|9.9928s|
|11.1514s|10.9937s|
|12.1654s|11.9937s|
|13.1783s|12.9957s|
|14.1943s|13.9927s|
|15.2043s|14.9947s|
|16.2163s|15.9937s|
|17.2383s|16.9967s|
|18.2482s|17.9977s|
|19.2632s|18.9926s|
|Average Error:| Average Error|
|0.1294558s|0.0044979s|

As you can see, over the course of 20 seconds, the `System.Threading.Timer` is already 0.26 seconds off track, while the `NATimer` still keeps its interval.
Now, 0.26 seconds is not much at this point, but imagine this timer running for a few hours. Extrapolated to one hour the error would be 47.376 seconds.

Now for a quick code example (as console app):
```csharp
static void Main(string[] args)
{
    using (NATimer timer = new NATimer())
    {
        timer.Interval = 1000;          //Setting the interval to one second
        timer.Tick += Timer_Tick;       //Settings the Tick event
        Console.WriteLine("Starting timer...");
        timer.Start();          //Starting the timer

        Thread.Sleep(10000);    //Wait for 10 seconds

        Console.WriteLine("Stopping timer...");
        timer.Stop();           //Stopping the timer
    }
}

private static void Timer_Tick(object sender, EventArgs e)
{
    Console.WriteLine("Timer ticked!!!");
}
```

And here's an explanation of the settings you can do:
* `Interval`: The interval for firing the `Tick` event in milliseconds.
* `FireImmediately`: If true, the `Tick` event fires immediately after calling `Start()`, otherwise it will wait for one interval to fire.
* `AsyncEvent`: States if the `Tick` event is fired asynchronous or not.

Some things to watch out:
* When using with an UI, you might have to invoke on the UI thread to update UI controls.
* Make sure that you dispose the timer after you are done using it.

## Last Words

If you find a bug or have a problem, please report it and I'll try to find a solution.