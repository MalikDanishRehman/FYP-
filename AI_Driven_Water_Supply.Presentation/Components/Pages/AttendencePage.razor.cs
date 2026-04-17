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
        private bool isSavingWorker = false;
        private string todayDate = DateTime.Now.ToString("dd MMM yyyy");
        private Worker newWorker = new();
        private Worker selectedWorker = new();
        private Worker? editDraft;
        private int totalPresentDays = 0;
        private decimal totalEarned = 0;

        private static string WorkerInitial(string? name) =>
            string.IsNullOrWhiteSpace(name) ? "?" : char.ToUpperInvariant(name.Trim()[0]).ToString();

        private static Worker CloneWorker(Worker w) => new()
        {
            Id = w.Id,
            SupplierId = w.SupplierId,
            Name = w.Name ?? "",
            Role = w.Role ?? "",
            BaseSalary = w.BaseSalary,
            TodayStatus = w.TodayStatus
        };

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

        private async Task MarkStatusForEdit(string status)
        {
            if (editDraft == null) return;
            var w = workers.FirstOrDefault(x => x.Id == editDraft.Id);
            if (w != null)
                await MarkStatus(w, status);
        }

        private async Task MarkStatus(Worker worker, string status)
        {
            try
            {
                worker.TodayStatus = status;
                if (editDraft != null && editDraft.Id == worker.Id)
                    editDraft.TodayStatus = status;

                var today = DateTime.UtcNow.Date;

                if (status == "Pending")
                {
                    var existing = await _supabase.From<AttendanceLog>()
                        .Where(x => x.WorkerId == worker.Id && x.Date == today)
                        .Get();
                    foreach (var log in existing.Models)
                        await _supabase.From<AttendanceLog>().Delete(log);
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("MarkStatus: " + ex.Message);
                ToastService.ShowToast("Attendance", "Could not update today's status.", "error");
            }
        }

        private async Task SaveWorker()
        {
            if (string.IsNullOrWhiteSpace(newWorker.Name))
            {
                ToastService.ShowToast("Validation", "Please enter the worker's name.", "warning");
                return;
            }

            var user = AuthService.CurrentUser;
            if (user == null) return;

            try
            {
                newWorker.SupplierId = user.Id;
                if (newWorker.BaseSalary < 0) newWorker.BaseSalary = 0;
                await _supabase.From<Worker>().Insert(newWorker);
                showAddModal = false;
                newWorker = new Worker();
                ToastService.ShowToast("Added", "Worker saved.", "success");
                await LoadWorkers(user.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveWorker: " + ex.Message);
                ToastService.ShowToast("Error", "Could not add worker.", "error");
            }
        }

        private void RecalculateEarnings(decimal baseSalary)
        {
            if (baseSalary > 0)
            {
                decimal daily = baseSalary / 30;
                totalEarned = Math.Round(daily * totalPresentDays, 0);
            }
            else
                totalEarned = 0;
        }

        private async Task OpenDetailModal(Worker worker)
        {
            selectedWorker = worker;
            editDraft = CloneWorker(worker);

            var response = await _supabase.From<AttendanceLog>()
                .Where(x => x.WorkerId == worker.Id && x.Status == "Present")
                .Get();

            totalPresentDays = response.Models.Count;
            RecalculateEarnings(editDraft.BaseSalary);

            showDetailModal = true;
        }

        private void CloseDetailModal()
        {
            showDetailModal = false;
            editDraft = null;
        }

        private async Task SaveWorkerEdits()
        {
            if (editDraft == null) return;
            if (string.IsNullOrWhiteSpace(editDraft.Name))
            {
                ToastService.ShowToast("Validation", "Name is required.", "warning");
                return;
            }

            isSavingWorker = true;
            try
            {
                if (editDraft.BaseSalary < 0) editDraft.BaseSalary = 0;
                await _supabase.From<Worker>().Update(editDraft);

                var w = workers.FirstOrDefault(x => x.Id == editDraft.Id);
                if (w != null)
                {
                    w.Name = editDraft.Name;
                    w.Role = editDraft.Role;
                    w.BaseSalary = editDraft.BaseSalary;
                    selectedWorker = w;
                }

                RecalculateEarnings(editDraft.BaseSalary);
                ToastService.ShowToast("Saved", "Worker details updated.", "success");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveWorkerEdits: " + ex.Message);
                ToastService.ShowToast("Error", "Could not save changes.", "error");
            }
            finally
            {
                isSavingWorker = false;
            }
        }

        private async Task DeleteWorker()
        {
            if (editDraft == null) return;
            try
            {
                await _supabase.From<Worker>().Delete(editDraft);
                CloseDetailModal();
                var user = AuthService.CurrentUser;
                if (user != null)
                {
                    ToastService.ShowToast("Removed", "Worker record deleted.", "success");
                    await LoadWorkers(user.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteWorker: " + ex.Message);
                ToastService.ShowToast("Error", "Could not delete worker.", "error");
            }
        }
    }
}
