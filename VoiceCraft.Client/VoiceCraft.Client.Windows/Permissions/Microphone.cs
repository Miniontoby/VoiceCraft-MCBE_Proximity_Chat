﻿using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace VoiceCraft.Client.Windows.Permissions;

public class Microphone : Microsoft.Maui.ApplicationModel.Permissions.Microphone
{
    public override void EnsureDeclared()
    { } //Legit do nothing

    public override Task<PermissionStatus> CheckStatusAsync()
    {
        EnsureDeclared();
        return Task.FromResult(PermissionStatus.Granted);
    }

    public override Task<PermissionStatus> RequestAsync() => CheckStatusAsync();

    public override bool ShouldShowRationale() => false;
}
