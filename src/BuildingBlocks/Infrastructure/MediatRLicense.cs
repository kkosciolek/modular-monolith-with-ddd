using Autofac;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CompanyName.MyMeetings.BuildingBlocks.Infrastructure;

public static class MediatRLicense
{
    public static void Apply(string licenseKey)
    {
        if (!string.IsNullOrWhiteSpace(licenseKey))
        {
            Mediator.LicenseKey = licenseKey;
        }
    }

    public static void RegisterAutofac(ContainerBuilder builder)
    {
        builder.Register(_ => new MediatRServiceConfiguration())
            .AsSelf()
            .SingleInstance()
            .IfNotRegistered(typeof(MediatRServiceConfiguration));

        builder.RegisterInstance(NullLoggerFactory.Instance)
            .As<ILoggerFactory>()
            .IfNotRegistered(typeof(ILoggerFactory));
    }
}