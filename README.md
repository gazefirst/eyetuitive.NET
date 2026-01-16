<br/>
<div align="center">
<a href="https://github.com/gazefirst/eyetuitive.NET">
<img src="https://gazefirst.com/logo/logo_only.png" alt="Logo" width="80" height="80">
</a>
<h3 align="center">.NET API for eyetuitive</h3>
<p align="center">
.NET API for eyetuitive, a remote eye tracker made by GazeFirst.
<br/>
<br/>
<a href="https://github.com/gazefirst/eyetuitive.NET/issues/new?labels=bug">Report Bug .</a>
<a href="https://github.com/gazefirst/eyetuitive.NET/issues/new?labels=enhancement">Request Feature</a>
</p>
</div>

![Product](https://gazefirst.com/wp-content/uploads/2023/09/20230901_135229-Edit-2-768x162.jpg)

![NuGet Version](https://img.shields.io/nuget/v/eyetuitive.NET?style=flat-square)

Use this repo / its published nuget package to integrate eyetuitive in your next .NET project / product.

## Connection

You can connect to eyetuitive via gRPC. Since the transport is based on HTTP/2, you need support for that. Also, ensure you can access the device via the network. The service runs on host eyetracker.local, port 12340.

## Note on legacy .NET versions

This library supports .NET 8 and .NET 10 (both LTS). However, .NET Framework 4.6.2 and later are only supported on Windows 11, since some underlying legacy gRPC implementations only support WinHttpHandler with http2 in Windows 11.

## Sample

```csharp
/// <summary>
/// eyetuitive instance
/// </summary>
private eyetuitive device = new eyetuitive();

/// <summary>
/// Connect to the eye tracker
/// </summary>
private async Task Connect()
{
	bool connected = await device.ConnectAsync();
	device.Position.StartPositionTracking(posHandler);
	device.Gaze.StartGazeTracking(gazeHandler);
}

/// <summary>
/// Handle gaze data
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
private void gazeHandler(object sender, GazeEventArgs e)
{
	NormedPoint2d gazepoint = e.gazePoint; //Normed as 0-1d - multiply with screen resolution if needed
}

/// <summary>
/// Handle position data
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
private void posHandler(object sender, PositionEventArgs e)
{
	NormedPoint2d leftEyePosition = e.leftEyePos; //Normed as 0-1d - multiply with screen resolution if needed
	NormedPoint2d rightEyePosition = e.rightEyePos; //Normed as 0-1d - multiply with screen resolution if needed
}
```

## License

Distributed under our custom license. See [license file](LICENSE.md) for more information.

## About

(c) 2024 - 2026 GazeFirst GmbH. Author: Mathias Anhalt
