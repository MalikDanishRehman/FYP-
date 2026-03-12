using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class GetStartedrazor
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private bool showServices = false;
        private List<string> selectedServices = new List<string>();

        void SelectRole(string role)
        {
            Nav.NavigateTo($"/signup?role={role}");
        }

        void ShowServiceSelection()
        {
            showServices = true;
        }

        void ToggleService(string serviceName)
        {
            if (selectedServices.Contains(serviceName))
            {
                selectedServices.Remove(serviceName);
            }
            else
            {
                selectedServices.Add(serviceName);
            }
        }

        void GoBack()
        {
            showServices = false;
            selectedServices.Clear();
        }

        void FinalizeProviderSignup()
        {
            string servicesParam = string.Join(",", selectedServices);
            Nav.NavigateTo($"/signup?role=Provider&services={servicesParam}");
        }
    }
}
