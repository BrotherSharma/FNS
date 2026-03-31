CREATE TABLE IF NOT EXISTS payment_approvals (
    id SERIAL PRIMARY KEY,
    user_id INTEGER,
    user_email VARCHAR(255) NOT NULL,
    user_name VARCHAR(255) NOT NULL,
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    order_id VARCHAR(100) NOT NULL UNIQUE,
    approval_token VARCHAR(500) NOT NULL UNIQUE,
    
    -- Payment Status: Pending, Completed, Failed
    payment_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    
    -- Approval Status: Pending, Approved, Rejected
    approval_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_at TIMESTAMP,
    approved_by VARCHAR(255),
    rejection_reason TEXT,
    
    
);

