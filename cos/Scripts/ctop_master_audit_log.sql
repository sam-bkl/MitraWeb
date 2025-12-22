-- Audit table for ctop_master to track all insert and update activities
-- This table stores a complete history of all changes to ctop_master records

CREATE TABLE IF NOT EXISTS ctop_master_audit_log (
    id BIGSERIAL PRIMARY KEY,
    audit_type VARCHAR(10) NOT NULL, -- 'INSERT' or 'UPDATE'
    ctopupno VARCHAR(50) NOT NULL,
    
    -- All columns from ctop_master (before and after values for UPDATE)
    username VARCHAR(255),
    name VARCHAR(255),
    dealertype VARCHAR(50),
    ssa_code VARCHAR(50),
    csccode VARCHAR(50),
    circle_code VARCHAR(50),
    attached_to VARCHAR(50),
    contact_number VARCHAR(50),
    pos_hno VARCHAR(255),
    pos_street VARCHAR(255),
    pos_landmark VARCHAR(255),
    pos_locality VARCHAR(255),
    pos_city VARCHAR(255),
    pos_district VARCHAR(255),
    pos_state VARCHAR(255),
    pos_pincode VARCHAR(20),
    created_date TIMESTAMP,
    pos_name_ss VARCHAR(255),
    pos_owner_name VARCHAR(255),
    pos_code VARCHAR(50),
    pos_ctop VARCHAR(50),
    circle_name VARCHAR(255),
    pos_unique_code VARCHAR(255),
    latitude VARCHAR(50),
    longitude VARCHAR(50),
    aadhaar_no VARCHAR(50),
    zone_code VARCHAR(50),
    ctop_type VARCHAR(50),
    dealercode VARCHAR(50),
    ref_dealer_id BIGINT,
    master_dealer_id BIGINT,
    parent_ctopno VARCHAR(50),
    dealer_status VARCHAR(50),
    
    -- Audit metadata
    account_id BIGINT NOT NULL, -- Account ID of the user who made the change
    data_source VARCHAR(50) DEFAULT 'MITRA',
    audit_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- For UPDATE operations, store old values
    old_pos_unique_code VARCHAR(255), -- Only used for UPDATE when pos_unique_code changes
    
    -- Additional context
    change_description TEXT, -- Description of what changed (e.g., "Updated pos_unique_code from ABC123 to XYZ789")
    
    CONSTRAINT chk_audit_type CHECK (audit_type IN ('INSERT', 'UPDATE'))
);

-- Index for faster queries
CREATE INDEX IF NOT EXISTS idx_ctop_master_audit_log_ctopupno ON ctop_master_audit_log(ctopupno);
CREATE INDEX IF NOT EXISTS idx_ctop_master_audit_log_audit_timestamp ON ctop_master_audit_log(audit_timestamp);
CREATE INDEX IF NOT EXISTS idx_ctop_master_audit_log_account_id ON ctop_master_audit_log(account_id);
CREATE INDEX IF NOT EXISTS idx_ctop_master_audit_log_audit_type ON ctop_master_audit_log(audit_type);

-- Comments for documentation
COMMENT ON TABLE ctop_master_audit_log IS 'Audit log table to track all insert and update activities on ctop_master table';
COMMENT ON COLUMN ctop_master_audit_log.audit_type IS 'Type of operation: INSERT or UPDATE';
COMMENT ON COLUMN ctop_master_audit_log.old_pos_unique_code IS 'Previous value of pos_unique_code (only for UPDATE operations)';
COMMENT ON COLUMN ctop_master_audit_log.change_description IS 'Human-readable description of what changed in this audit entry';

