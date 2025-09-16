using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

namespace SE.TruckTicketing.UI.ViewModels.InvoiceConfigurations;

public class InvoiceConfigurationInvoiceInfoViewModel
{
    public InvoiceConfigurationInvoiceInfoViewModel(InvoiceConfiguration invoiceConfiguration)
    {
        InvoiceConfiguration = invoiceConfiguration;
        WellClassificationData = DataDictionary.For<WellClassifications>().Select(x =>
                                                                                      new ListOption<string>
                                                                                      {
                                                                                          Display = x.Value,
                                                                                          Value = x.Value,
                                                                                      }).ToList();
    }

    public List<ListOption<string>> WellClassificationData { get; } = new();

    public InvoiceConfiguration InvoiceConfiguration { get; }

    public IEnumerable<string> WellClassification
    {
        get => InvoiceConfiguration.WellClassifications.Select(x => x.GetEnumDescription<WellClassifications>()).ToList();
        set => InvoiceConfiguration.WellClassifications = value.Select(x => x.GetEnumValue<WellClassifications>()).ToList();
    }
}
