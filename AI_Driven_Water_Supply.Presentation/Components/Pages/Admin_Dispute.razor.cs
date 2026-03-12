using Microsoft.AspNetCore.Components;
using AI_Driven_Water_Supply.Application.Interfaces;

namespace AI_Driven_Water_Supply.Presentation.Components.Pages
{
    public partial class Admin_Dispute
    {
        [Inject] public Supabase.Client _supabase { get; set; } = null!;
        [Inject] public IAuthService AuthService { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;

        private string activeTab = "All";
        private bool showModal = false;
        private DisputeTicket selectedTicket = null!;

        public class DisputeTicket
        {
            public string TicketId { get; set; } = null!;
            public string UserName { get; set; } = null!;
            public string VendorName { get; set; } = null!;
            public string IssueType { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string Priority { get; set; } = null!;
            public string Date { get; set; } = null!;
            public string Status { get; set; } = null!;
        }

        private List<DisputeTicket> tickets = new List<DisputeTicket>
        {
            new DisputeTicket { TicketId = "9012", UserName = "Ahmed Ali", VendorName = "Aqua Pure", IssueType = "Water Quality", Description = "Water smells weird and tastes salty.", Priority = "High", Date = "10 Feb", Status = "Pending" },
            new DisputeTicket { TicketId = "9013", UserName = "Sara Khan", VendorName = "Blue Drop", IssueType = "Late Delivery", Description = "Order was delayed by 4 hours without notice.", Priority = "Med", Date = "09 Feb", Status = "Pending" },
            new DisputeTicket { TicketId = "8840", UserName = "Bilal", VendorName = "Clean Sip", IssueType = "Rude Behavior", Description = "Delivery guy was very rude.", Priority = "Low", Date = "05 Feb", Status = "Resolved" },
            new DisputeTicket { TicketId = "8845", UserName = "Usman", VendorName = "QuickWater", IssueType = "Payment Issue", Description = "Charged double for single refill.", Priority = "High", Date = "01 Feb", Status = "Resolved" },
        };

        private IEnumerable<DisputeTicket> FilteredTickets
        {
            get
            {
                if (activeTab == "All") return tickets;
                return tickets.Where(t => t.Status == activeTab);
            }
        }

        private string GetPriorityClass(string priority)
        {
            return priority switch
            {
                "High" => "p-high",
                "Med" => "p-med",
                _ => "p-low"
            };
        }

        private void OpenTicket(DisputeTicket ticket)
        {
            selectedTicket = ticket;
            showModal = true;
        }

        private void CloseModal()
        {
            showModal = false;
        }
    }
}
