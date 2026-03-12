using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Bills
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;

        private List<Bill> pendingBills = new();
        private List<Bill> paidBills = new();
        private decimal TotalUnpaid = 0;
        private decimal LastPaidAmount = 0;
        private string MyName = "";

        private bool showPaymentModal = false;
        private Bill? selectedBill = null;

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null) user = _supabase.Auth.CurrentUser;

            if (user != null)
            {
                string email = user.Email ?? "";
                if (user.UserMetadata != null && user.UserMetadata.TryGetValue("username", out var nameObj))
                    MyName = nameObj?.ToString() ?? email.Split('@')[0];
                else
                    MyName = email.Split('@')[0];

                await LoadBills();
            }
        }

        private async Task LoadBills()
        {
            var response = await _supabase.From<Bill>()
                .Where(x => x.ConsumerName == MyName)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var allBills = response.Models;
            pendingBills = allBills.Where(b => b.Status == "Unpaid").ToList();
            paidBills = allBills.Where(b => b.Status == "Paid").ToList();
            TotalUnpaid = pendingBills.Sum(x => x.Amount);
            LastPaidAmount = paidBills.FirstOrDefault()?.Amount ?? 0;
        }

        private void OpenPaymentModal(Bill bill)
        {
            selectedBill = bill;
            showPaymentModal = true;
        }

        private void CloseModal()
        {
            showPaymentModal = false;
            selectedBill = null;
        }

        private async Task ProcessPayment(string method)
        {
            if (selectedBill == null) return;

            showPaymentModal = false;

            try
            {
                ToastService.ShowToast("Processing", $"Connecting to {method}...", "info");

                await Task.Delay(1500);

                selectedBill.Status = "Paid";
                await _supabase.From<Bill>().Update(selectedBill);

                await LoadBills();

                ToastService.ShowToast("Payment Successful", $"Paid via {method}!", "success");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Payment Error: " + ex.Message);
                ToastService.ShowToast("Transaction Failed", "Could not complete payment.", "error");
            }
        }

        private void GoBack() => Nav.NavigateTo("/Consumer");
    }
}
