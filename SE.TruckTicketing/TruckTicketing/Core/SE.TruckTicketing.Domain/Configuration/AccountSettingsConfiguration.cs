using System;

using Trident.Configuration;
using Trident.Contracts.Configuration;

namespace SE.TruckTicketing.Domain.Configuration;

public interface IAccountSettingsConfiguration : ICoreConfiguration
{
    public bool RunAccountCustomerNoActivityProcessor { get; }

    public bool RunMaterialApprovalLoadSummaryReportProcessor { get; }
}

public class AccountSettingsConfiguration : IAccountSettingsConfiguration
{
    private const string Run_Account_Customer_NoActivity_Processor = "RunAccountCustomerNoActivityProcessor";

    private const string Run_Material_Approval_LoadSummary_Report_Processor = "RunMaterialApprovalLoadSummaryReportProcessor";

    private readonly IAppSettings _appSettings;

    public AccountSettingsConfiguration(IAppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public bool RunAccountCustomerNoActivityProcessor => Convert.ToBoolean(_appSettings[Run_Account_Customer_NoActivity_Processor]);

    public bool RunMaterialApprovalLoadSummaryReportProcessor => Convert.ToBoolean(_appSettings[Run_Material_Approval_LoadSummary_Report_Processor]);
}
