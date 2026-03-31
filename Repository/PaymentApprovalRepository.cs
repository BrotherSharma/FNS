using FNS.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNS.Repository
{
    public class PaymentApprovalRepository : IPaymentApproval
    {
        private readonly NpgsqlConnection _connection;
        private readonly ILogger<PaymentApprovalRepository> _logger;

        public PaymentApprovalRepository(NpgsqlConnection connection, ILogger<PaymentApprovalRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<PaymentApproval> CreatePaymentApprovalAsync(PaymentApproval approval)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
                    INSERT INTO payment_approvals 
                    (user_id, user_email, user_name, amount, currency, order_id, approval_token, payment_status, approval_status, created_at)
                    VALUES (@UserId, @UserEmail, @UserName, @Amount, @Currency, @OrderId, @ApprovalToken, @PaymentStatus, @ApprovalStatus, @CreatedAt)
                    RETURNING id;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", approval.UserId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserEmail", approval.UserEmail);
                    cmd.Parameters.AddWithValue("@UserName", approval.UserName);
                    cmd.Parameters.AddWithValue("@Amount", approval.Amount);
                    cmd.Parameters.AddWithValue("@Currency", approval.Currency);
                    cmd.Parameters.AddWithValue("@OrderId", approval.OrderId);
                    cmd.Parameters.AddWithValue("@ApprovalToken", approval.ApprovalToken);
                    cmd.Parameters.AddWithValue("@PaymentStatus", approval.PaymentStatus);
                    cmd.Parameters.AddWithValue("@ApprovalStatus", approval.ApprovalStatus);
                    cmd.Parameters.AddWithValue("@CreatedAt", approval.CreatedAt);

                    var result = await cmd.ExecuteScalarAsync();
                    approval.Id = (int)result;
                }

                _logger.LogInformation($"Payment approval created for order: {approval.OrderId}");
                return approval;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating payment approval: {ex.Message}");
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<PaymentApproval> GetPaymentApprovalByOrderIdAsync(string orderId)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
                    SELECT id, user_id, user_email, user_name, amount, currency, order_id, approval_token, 
                           payment_status, approval_status, created_at, approved_at, approved_by, rejection_reason
                    FROM payment_approvals
                    WHERE order_id = @OrderId;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapPaymentApproval(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving payment approval by order ID: {ex.Message}");
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<PaymentApproval> GetPaymentApprovalByApprovalTokenAsync(string token)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
                    SELECT id, user_id, user_email, user_name, amount, currency, order_id, approval_token, 
                           payment_status, approval_status, created_at, approved_at, approved_by, rejection_reason
                    FROM payment_approvals
                    WHERE approval_token = @Token;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapPaymentApproval(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving payment approval by token: {ex.Message}");
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<bool> UpdateApprovalStatusAsync(int id, string status, string approvedBy = null)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
                    UPDATE payment_approvals
                    SET approval_status = @Status, approved_at = @ApprovedAt, approved_by = @ApprovedBy
                    WHERE id = @Id;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@ApprovedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@ApprovedBy", approvedBy ?? (object)DBNull.Value);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    _logger.LogInformation($"Payment approval status updated to {status} for ID: {id}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating approval status: {ex.Message}");
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<PaymentApproval> GetLatestApprovalByUserIdAsync(int userId)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
                    SELECT id, user_id, user_email, user_name, amount, currency, order_id, approval_token, 
                           payment_status, approval_status, created_at, approved_at, approved_by, rejection_reason
                    FROM payment_approvals
                    WHERE user_id = @UserId
                    ORDER BY created_at DESC
                    LIMIT 1;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapPaymentApproval(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving latest approval by user ID: {ex.Message}");
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<PaymentApproval> GetLatestApprovalByEmailAsync(string email)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
                    SELECT id, user_id, user_email, user_name, amount, currency, order_id, approval_token, 
                           payment_status, approval_status, created_at, approved_at, approved_by, rejection_reason
                    FROM payment_approvals
                    WHERE user_email = @Email
                    ORDER BY created_at DESC
                    LIMIT 1;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapPaymentApproval(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving latest approval by email: {ex.Message}");
                throw;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        private PaymentApproval MapPaymentApproval(NpgsqlDataReader reader)
        {
            return new PaymentApproval
            {
                Id = reader.GetInt32(0),
                UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                UserEmail = reader.GetString(2),
                UserName = reader.GetString(3),
                Amount = reader.GetDecimal(4),
                Currency = reader.GetString(5),
                OrderId = reader.GetString(6),
                ApprovalToken = reader.GetString(7),
                PaymentStatus = reader.GetString(8),
                ApprovalStatus = reader.GetString(9),
                CreatedAt = reader.GetDateTime(10),
                ApprovedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                ApprovedBy = reader.IsDBNull(12) ? null : reader.GetString(12),
                RejectionReason = reader.IsDBNull(13) ? null : reader.GetString(13)
            };
        }
    }
}
