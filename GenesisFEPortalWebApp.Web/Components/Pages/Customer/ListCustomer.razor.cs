using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    public partial class ListCustomer
    {
        [Inject]
        public required ApiClient ApiClient { get; set; }
        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;
        public IQueryable<CustomerModel> customers { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            var res = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/Customer");

            if (res != null && res.Success)
            {
                var customerList = JsonConvert.DeserializeObject<List<CustomerModel>>(res.Data.ToString()!)!;
                customers = customerList.AsQueryable();
            }

            await base.OnInitializedAsync();
        }

        private void OnClick(string text, long idCustomer)
        {
            if (text.Equals("update"))
            {
                var url = $"/customer/update/{idCustomer}";
                NavigationManager.NavigateTo(url);
            }
            else if (text.Equals(""))
            {

            }

        }
    }
}
