using System;
using System.Runtime.InteropServices;

namespace MyGame;

internal sealed class ExternalMp3Player : IDisposable
{
    private dynamic? _player;

    public bool Load(string path, int volume, bool repeat)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        var playerType = Type.GetTypeFromProgID("WMPlayer.OCX");
        if (playerType == null)
        {
            return false;
        }

        try
        {
            _player = Activator.CreateInstance(playerType);
            var adjustedVolume = repeat ? volume : volume + 20;
            _player.settings.volume = Math.Clamp(adjustedVolume, 0, 100);
            _player.settings.setMode("loop", repeat);
            _player.URL = path;
            _player.controls.stop();
            return true;
        }
        catch
        {
            Dispose();
            return false;
        }
    }

    public void PlayFromStart()
    {
        if (_player == null)
        {
            return;
        }

        try
        {
            _player.controls.stop();
            _player.controls.currentPosition = 0;
            _player.controls.play();
        }
        catch
        {
        }
    }

    public void Stop()
    {
        if (_player == null)
        {
            return;
        }

        try
        {
            _player.controls.stop();
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_player == null)
        {
            return;
        }

        try
        {
            _player.controls.stop();
            _player.close();
        }
        catch
        {
        }
        finally
        {
            _player = null;
        }
    }
}
