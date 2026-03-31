using System;

namespace FNS.Models
{
    public class PaymentApproval
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string ApprovalToken { get; set; }
        
        /// <summary>
        /// Payment Status: Pending, Completed, Failed
        /// </summary>
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Completed, Failed

        /// <summary>
        /// Approval Status: Pending, Approved, Rejected
        /// </summary>
        public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; }
        public string RejectionReason { get; set; }
    }
}
