using System.Linq;
using System.Threading.Tasks;
using static SE.TruckTicketing.Domain.Entities.SalesLine.SalesLineManagerHelper;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public record PreviewApprovedSentToFoRefreshPricingStrategy(PriceRefreshContext PriceRefreshContext) : IShouldRefreshPricingStrategy
{
    private const bool YesRefreshThePrice = true;
    private const bool DoNotRefreshThePrice = false;

    public async Task<bool> ShouldRefreshPricing()
    {
        if (HasPriceBeenChangedManually(PriceRefreshContext.CurrentSalesLine))
        {
            return DoNotRefreshThePrice;
        }

        var loadConfirmation = await PriceRefreshContext.LoadConfirmationProvider.GetById(PriceRefreshContext.CurrentSalesLine.LoadConfirmationId);

        if (loadConfirmation == null)
        {
            return YesRefreshThePrice;
        }

        var billingConfig = await PriceRefreshContext.BillingConfigurationProvider.GetById(loadConfirmation.BillingConfigurationId);

        if (billingConfig == null)
        {
            return YesRefreshThePrice;
        }

        //FYI - FieldTicketsUploadEnabled == "this is a field ticket" == field ticket upload set to On on billing config page.
        //IF IT'S A FIELD TICKET, SHORT CIRCUIT IMMEDIATELY. Don't continue with other checks.
        if (IsFieldTicketOfTypeTicketByTicketOrLcBatch(billingConfig))
        {
            if (string.IsNullOrEmpty(PriceRefreshContext.CurrentSalesLine.ProductNumber))
            {
                return DoNotRefreshThePrice;
            }

            if (PriceRefreshContext.CurrentSalesLine.IsProductAnythingButOilCredit())//important business rule here. 
            {
                return YesRefreshThePrice;
            }

            return DoNotRefreshThePrice;

        }

        var serviceType = await PriceRefreshContext.ServiceTypeManager.GetById(PriceRefreshContext.CurrentSalesLine.ServiceTypeId);
        
        var hasPassedCutTypeRules = GetCutTypeRules(PriceRefreshContext.CurrentSalesLine, serviceType);

        if (!hasPassedCutTypeRules)
        {
            return DoNotRefreshThePrice;
        }

        //Condition 5 - Bug #13196
        var isSalesLineCutTypeTotalAndHasZeroRate = IsSalesLineCutTypeTotalAndHasZeroRate(PriceRefreshContext.CurrentSalesLine);

        if (isSalesLineCutTypeTotalAndHasZeroRate)
        {
            bool hasAdditionalServicesOnTicket = false;

            if (PriceRefreshContext.SalesLines != null)
            {
                hasAdditionalServicesOnTicket = PriceRefreshContext.SalesLines.Any(x => x.IsAdditionalService);
            }

            if (hasAdditionalServicesOnTicket)
            {
                return DoNotRefreshThePrice;
            }
        }


        //otherwise read the saved value from the entity.
        return GetSavedEntityValue(PriceRefreshContext.CurrentSalesLine);
    }
}
