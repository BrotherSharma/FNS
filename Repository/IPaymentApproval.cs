using FNS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNS.Repository
{
    public interface IPaymentApproval
    {
        Task<PaymentApproval> CreatePaymentApprovalAsync(PaymentApproval approval);
        Task<PaymentApproval> GetPaymentApprovalByOrderIdAsync(string orderId);
        Task<PaymentApproval> GetPaymentApprovalByApprovalTokenAsync(string token);
        Task<bool> UpdateApprovalStatusAsync(int id, string status, string approvedBy = null);
        Task<PaymentApproval> GetLatestApprovalByUserIdAsync(int userId);
        Task<PaymentApproval> GetLatestApprovalByEmailAsync(string email);
    }
}
