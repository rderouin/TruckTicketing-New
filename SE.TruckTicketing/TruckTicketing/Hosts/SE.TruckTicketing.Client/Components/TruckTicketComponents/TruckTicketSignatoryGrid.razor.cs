// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
//
// using Microsoft.AspNetCore.Components;
//
// using Radzen;
// using SE.TruckTicketing.Contracts.Models.Operations;
//
// using Trident.Api.Search;
//
// namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;
//
// public partial class TruckTicketSignatoryGrid
// {
//     private List<AccountContact> _accountContacts;
//
//         private Dictionary<Guid, Guid> _accountContactToAccountMap;
//
//     private int? _pageSize { get; set; } = 10;
//
//     [Parameter]
//     public List<Signatory> SignatoryContacts { get; set; }
//
//     [Parameter]
//     public List<AccountContact> BillingCustomerAccountContactList { get; set; }
//
//         [Parameter]
//         public List<AccountContact> GeneratorAccountContactList { get; set; }
//
//         [Parameter]
//         public Account BillingCustomerAccount { get; set; }
//
//         [Parameter]
//         public Account GeneratorAccount { get; set; }
//         //Events
//
//     [Parameter]
//     public EventCallback<Signatory> SignatoryContactDeleted { get; set; }
//
//     [Parameter]
//     public EventCallback<Signatory> SignatoryContactAddUpdate { get; set; }
//
//     [Parameter]
//     public EventCallback<Signatory> SignatoryContactAuthorizedChange { get; set; }
//
//         private bool _isSaveDisabled => !BillingCustomerAccount.Contacts.Any() || !GeneratorAccount.Contacts.Any();
//
//         protected override async Task OnParametersSetAsync()
//         {
//             await base.OnParametersSetAsync();
//             await LoadSignatoryContacts();
//             _accountContacts = new();
//             _accountContactToAccountMap = new();
//
//             if (BillingCustomerAccount.Contacts.Count > 0 || GeneratorAccount.Contacts.Count > 0)
//             {
//                 if (BillingCustomerAccount != null)
//                 {
//                     foreach (var customerContact in BillingCustomerAccount.Contacts)
//                     {
//                         if (!SignatoryContacts.Exists(x => x.AccountContactId == customerContact.Id))
//                         {
//                             _accountContacts.Add(customerContact);
//                             _accountContactToAccountMap.TryAdd(customerContact.Id, BillingCustomerAccount.Id);
//                         }
//                     }
//                 }
//
//                 if (GeneratorAccount != null)
//                 {
//                     foreach (var generatorContact in GeneratorAccount.Contacts)
//                     {
//                         if (!SignatoryContacts.Exists(x => x.AccountContactId == generatorContact.Id))
//                         {
//                             _accountContacts.Add(generatorContact);
//                             _accountContactToAccountMap.TryAdd(generatorContact.Id, GeneratorAccount.Id);
//                         }
//                     }
//                 }
//             }
//         }
//
//     private Task LoadSignatoryContacts()
//     {
//         _signatoryContacts = new(SignatoryContacts);
//         return Task.CompletedTask;
//     }
//
//     private async Task AuthorizeSignatory(Signatory signatoryApplicant)
//     {
//         await SignatoryContactAuthorizedChange.InvokeAsync(signatoryApplicant);
//     }
//
//     private async Task AddSignatoryContact()
//     {
//         await OpenEditDialog(new() { Id = Guid.NewGuid() });
//     }
//
//         private async Task EditButton_Click(Signatory singatory)
//         {
//             await OpenEditDialog(singatory, true);
//         }
//         private async Task OpenEditDialog(Signatory model, bool editMode = false)
//         {
//             await DialogService.OpenAsync<TruckTicketSignatoryEdit>("Add Signatory Contact",
//                                                                                         new Dictionary<string, object>()
//                                                                                         {
//                                                                                             { nameof(TruckTicketSignatoryEdit.signatoryModel), model },
//                                                                                             { nameof(TruckTicketSignatoryEdit.AccountContacts), _accountContacts },
//                                                                                             { nameof(TruckTicketSignatoryEdit.AccountContactToAccountMap), _accountContactToAccountMap },
//                                                                                             { nameof(TruckTicketSignatoryEdit.OnSubmit), new EventCallback(this, (Func<Signatory, Task>)(async (model) =>
//                                                                                                         {
//                                                                                                             DialogService.Close();
//                                                                                                             await SignatoryContactAddUpdate.InvokeAsync(model);
//                                                                                                         })) },
//                                                                                             { nameof(TruckTicketSignatoryEdit.OnCancel), new EventCallback(this, () => DialogService.Close())  }
//                                                                                         });
//         }
//
//
//         private async Task DeleteButton_Click(Signatory model)
//         {
//             const string msg = "Are you sure you want to delete this record?";
//             const string title = "Delete Signatory Contact Record";
//             var deleteConfirmed = await DialogService.Confirm(msg, title,
//                                                               new ConfirmOptions()
//                                                               {
//                                                                   OkButtonText = "Delete",
//                                                                   CancelButtonText = "Cancel"
//                                                               });
//
//         if (deleteConfirmed.GetValueOrDefault())
//         {
//             await SignatoryContactDeleted.InvokeAsync(model);
//         }
//     }
// }


