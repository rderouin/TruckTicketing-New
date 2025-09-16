using Radzen;

using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Client.Configuration.Navigation;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Client.Utilities;

using Trident.IoC;
using Trident.Mapper;

namespace SE.TruckTicketing.Client;

public class PackageModule : IoCModule
{
    public override void Configure(IIoCProvider builder)
    {
        RegisterDefaultAssemblyScans(builder);
        builder.RegisterSingleton<DialogService, DialogService>();
        builder.RegisterSingleton<NotificationService, NotificationService>();
        builder.UsingTridentMapperProfiles(TargetAssemblies);
        builder.RegisterSelf();

        builder.Register<NavigationConfigurationProvider, INavigationConfigurationProvider>();
        builder.Register<TruckTicketingAuthorizationService, ITruckTicketingAuthorizationService>();
        builder.Register<CsvExportService, ICsvExportService>();
        builder.Register<TextFileExportService, ITextFileExportService>();
        builder.Register<TruckTicketExperienceViewModel, TruckTicketExperienceViewModel>();
        builder.Register<TruckTicketWorkflowManager, TruckTicketWorkflowManager>();
        builder.Register<SalesLinesWatcher, ISalesLineWatcher>();
        builder.Register<BillingConfigurationWatcher, IBillingConfigurationWatcher>();
    }
}
