using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;
using AI_Driven_Water_Supply.Domain.Entities;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class AttendencePage
    {
        [Inject] private Supabase.Client _supabase { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private List<Worker> workers = new();
        private bool showAddModal = false;
        private bool showDetailModal = false;
        private bool isLoading = true;
        private string todayDate = DateTime.Now.ToString("dd MMM yyyy");
        private Worker newWorker = new();
        private Worker selectedWorker = new();
        private int totalPresentDays = 0;
        private decimal totalEarned = 0;

        protected override async Task OnInitializedAsync()
        {
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                await AuthService.TryRefreshSession();
                user = AuthService.CurrentUser;
            }

            if (user != null)
            {
                await LoadWorkers(user!.Id);
            }
            else
            {
                Nav.NavigateTo("/login");
            }
        }

        private async Task LoadWorkers(string userId)
        {
            isLoading = true;
            try
            {
                var response = await _supabase.From<Worker>().Where(x => x.SupplierId == userId).Get();
                workers = response.Models;

                var today = DateTime.UtcNow.Date;
                var attResponse = await _supabase.From<AttendanceLog>().Where(x => x.Date == today).Get();
                var logs = attResponse.Models;

                foreach (var w in workers)
                {
                    var log = logs.FirstOrDefault(l => l.WorkerId == w.Id);
                    w.TodayStatus = log != null ? log.Status : "Pending";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                ToastService.ShowToast("Error Failed", "Something went wrong!", "error");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task MarkStatus(Worker worker, string status)
        {
            worker.TodayStatus = status;

            var today = DateTime.UtcNow.Date;
            var existing = await _supabase.From<AttendanceLog>()
                .Where(x => x.WorkerId == worker.Id && x.Date == today)
                .Get();

            if (existing.Models.Any())
            {
                var log = existing.Models.First();
                log.Status = status;
                await _supabase.From<AttendanceLog>().Update(log);
            }
            else
            {
                await _supabase.From<AttendanceLog>().Insert(new AttendanceLog
                {
                    WorkerId = worker.Id,
                    Date = today,
                    Status = status
                });
            }
        }

        private async Task SaveWorker()
        {
            var user = AuthService.CurrentUser;
            if (user != null)
            {
                newWorker.SupplierId = user!.Id;
                await _supabase.From<Worker>().Insert(newWorker);
                showAddModal = false;
                newWorker = new Worker();
                await LoadWorkers(user!.Id);
            }
        }

        private async Task OpenDetailModal(Worker worker)
        {
            selectedWorker = worker;

            var response = await _supabase.From<AttendanceLog>()
                .Where(x => x.WorkerId == worker.Id && x.Status == "Present")
                .Get();

            totalPresentDays = response.Models.Count;

            if (worker.BaseSalary > 0)
            {
                decimal daily = worker.BaseSalary / 30;
                totalEarned = Math.Round(daily * totalPresentDays, 0);
            }
            else
            {
                totalEarned = 0;
            }

            showDetailModal = true;
        }

        private async Task DeleteWorker()
        {
            await _supabase.From<Worker>().Delete(selectedWorker);
            showDetailModal = false;
            var user = AuthService.CurrentUser;
            if (user != null) await LoadWorkers(user!.Id);
        }
    }
}
