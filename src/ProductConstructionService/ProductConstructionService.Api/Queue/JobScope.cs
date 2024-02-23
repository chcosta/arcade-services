﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ProductConstructionService.Api.Queue.JobRunners;
using ProductConstructionService.Api.Queue.Jobs;
using ProductConstructionService.Api.Telemetry;

namespace ProductConstructionService.Api.Queue;

public class JobScope(
    Action finalizer,
    IServiceScope serviceScope,
    ITelemetryRecorder telemetryRecorder) : IDisposable
{
    private readonly IServiceScope _serviceScope = serviceScope;
    private readonly ITelemetryRecorder _telemetryRecorder = telemetryRecorder;
    private Job? _job = null;

    public void Dispose()
    {
        finalizer.Invoke();
        _serviceScope.Dispose();
    }

    public void InitializeScope(Job job)
    {
        _job = job;
    }

    public async Task RunJobAsync(CancellationToken cancellationToken)
    {
        if (_job is null)
        {
            throw new Exception($"{nameof(JobScope)} not initialized! Call InitializeScope before calling {nameof(RunJobAsync)}");
        }

        var jobRunner = _serviceScope.ServiceProvider.GetRequiredKeyedService<IJobRunner>(_job.Type);

        using (ITelemetryScope telemetryScope  = _telemetryRecorder.RecordJob(_job))
        {
            await jobRunner.RunAsync(_job, cancellationToken);
            telemetryScope.SetSuccess();
        }
    }
}