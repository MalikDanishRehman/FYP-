using Microsoft.AspNetCore.Components;
using Supabase.Postgrest.Models;
using AI_Driven_Water_Supply.Domain.Entities;
using AI_Driven_Water_Supply.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class SupplierBills
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

        private List<Bill> collections = new();
        private List<Bill> history = new();
        private float TotalEarnings = 0;
        private float PendingAmount = 0;
        private string MyName = "";
        private bool isLoading = true;
        private bool showModal = false;
        private Bill? selectedBill = null;

        protected override async Task OnInitializedAsync()
        {
            await LoadProfileAndData();
        }

        private async Task LoadProfileAndData()
        {
            try
            {
                var user = AuthService.CurrentUser;
                if (user != null)
                {
                    MyName = user.UserMetadata.ContainsKey("full_name")
                              ? (user.UserMetadata["full_name"]?.ToString() ?? "Supplier")
                              : "M.Akmal";
                }
                await LoadBills();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadBills()
        {
            var response = await _supabase.From<Bill>()
                .Where(x => x.SupplierName == MyName)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var allBills = response.Models;
            collections = allBills.Where(b => b.Status == "Unpaid").ToList();
            history = allBills.Where(b => b.Status == "Paid").ToList();
            TotalEarnings = (float)history.Sum(x => x.Amount);
            PendingAmount = (float)collections.Sum(x => x.Amount);
            StateHasChanged();
        }

        private void OpenPaymentModal(Bill bill)
        {
            selectedBill = bill;
            showModal = true;
        }

        private void CloseModal()
        {
            showModal = false;
            selectedBill = null;
        }

        private async Task ProcessPayment(string method)
        {
            if (selectedBill == null) return;
            try
            {
                await _supabase.From<Bill>()
                    .Where(x => x.Id == selectedBill.Id)
                    .Set(x => x.Status, "Paid")
                    .Set(x => x.PaymentMethod, method)
                    .Update();

                ToastService.ShowToast("Success", $"Payment received via {method}", "success");
                CloseModal();
                await LoadBills();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Payment Error: " + ex.Message);
                ToastService.ShowToast("Error", "Failed to update payment", "error");
            }
        }

        private void GoBack() => Nav.NavigateTo("/ProviderDashboard");
    }
}
