-- ╔══════════════════════════════════════════════════════════════════════════════╗
-- ║  DATABASE SCHEMA — Pregnancy Tracking Application (MomCare)               ║
-- ║  Updated: 2026-02-22                                                       ║
-- ║  Engine: MySQL 8.0+ / InnoDB                                              ║
-- ╚══════════════════════════════════════════════════════════════════════════════╝
--
-- CONVENTIONS:
--   • CHAR(36) for all GUIDs — EF Core Guid ↔ MySQL compatibility
--   • snake_case for ALL table & column names
--   • utf8mb4 charset — full Unicode (Vietnamese, emoji)
--   • Enums → VARCHAR strings (EF Core HasConversion<string>())
--   • Soft delete via deleted_at column (NULL = active, global query filter)
--   • BaseEntity pattern: { id, created_at, updated_at, deleted_at }
--   • Composite PK tables (join/translation): NO BaseEntity, have created_at only
--   • Index naming: idx_{table}_{columns}
--   • Unique key naming: uk_{table}_{columns}
--   • FK naming: fk_{table}_{ref_table}
--
-- FEATURE GROUPS:
--   ┌─ Section 1:  Lookup — languages                          (Foundation)
--   ├─ Section 2:  Users / Auth / RBAC                         (Week 1-2) ✅
--   ├─ Section 3:  Pregnancy Core                              (Week 3)   ✅
--   ├─ Section 4:  Shared File Storage                         (Week 4)   ✅
--   ├─ Section 5:  Medical Documents + OCR                     (Week 4)   ✅
--   ├─ Section 6:  AI Infrastructure                           (Week 5)   ✅
--   ├─ Section 7:  Weight Tracking + Motivational              (Week 6)   ✅
--   ├─ Section 8:  Nutrition + Meal Planning                   (Week 7)   ⬜
--   ├─ Section 9:  Doctor Profiles + Specialties + Scheduling  (Week 8+)  ⬜
--   ├─ Section 10: Consult + Chat + Call                       (Week 8+)  ⬜
--   └─ Section 11: Reminders                                   (Future)   ⬜
--
-- NOTE: ✅ = Implemented (has EF Config + Entity)  ⬜ = Future (schema only)

-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 1: LOOKUP — languages
-- Foundation table. Referenced by all translation tables.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE languages (
    code         VARCHAR(10)  NOT NULL,          -- 'vi', 'en', 'ja'...
    name         VARCHAR(50)  NOT NULL,
    is_active    TINYINT(1)   NOT NULL DEFAULT 1,
    created_at   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

    PRIMARY KEY (code),
    UNIQUE KEY uk_languages_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 2: USERS / AUTH / RBAC (Week 1-2) ✅
-- User accounts, JWT refresh tokens, role-based access control, audit log.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE users (
    id                  CHAR(36)       NOT NULL,
    email               VARCHAR(320)   NULL,
    email_normalized    VARCHAR(320)   NULL,       -- LOWER(email), indexed for login
    phone               VARCHAR(20)    NULL,       -- E.164 format: +84901234567
    password_hash       VARBINARY(255) NOT NULL,   -- bcrypt/argon2 hash
    status              VARCHAR(20)    NOT NULL DEFAULT 'Pending',
                                                   -- Enum: Pending | Active | Suspended | Deleted
    is_email_verified   TINYINT(1)     NOT NULL DEFAULT 0,
    is_phone_verified   TINYINT(1)     NOT NULL DEFAULT 0,
    last_login_at       DATETIME(6)    NULL,

    created_at          DATETIME(6)    NOT NULL,
    updated_at          DATETIME(6)    NOT NULL,
    deleted_at          DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_users_email (email_normalized),
    UNIQUE KEY uk_users_phone (phone),
    INDEX idx_users_status (status),
    INDEX idx_users_deleted_at (deleted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE user_profiles (
    id              CHAR(36)      NOT NULL,
    user_id         CHAR(36)      NOT NULL,
    full_name       VARCHAR(150)  NULL,
    date_of_birth   DATE          NULL,
    avatar_url      VARCHAR(500)  NULL,       -- URL ảnh đại diện (có thể Supabase URL)
    preferred_lang  VARCHAR(10)   NOT NULL DEFAULT 'vi',

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_user_profiles_user (user_id),
    CONSTRAINT fk_user_profiles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_profiles_lang FOREIGN KEY (preferred_lang) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE roles (
    id           CHAR(36)      NOT NULL,
    code         VARCHAR(50)   NOT NULL,      -- ADMIN, USER, DOCTOR
    name         VARCHAR(100)  NOT NULL,
    description  VARCHAR(255)  NULL,

    created_at   DATETIME(6)   NOT NULL,
    updated_at   DATETIME(6)   NOT NULL,
    deleted_at   DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_roles_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE permissions (
    id           CHAR(36)      NOT NULL,
    code         VARCHAR(80)   NOT NULL,      -- document.create, ocr.trigger, tag.view...
    name         VARCHAR(120)  NOT NULL,
    description  VARCHAR(255)  NULL,

    created_at   DATETIME(6)   NOT NULL,
    updated_at   DATETIME(6)   NOT NULL,
    deleted_at   DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_permissions_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Join: role ↔ permission (N:N)
CREATE TABLE role_permissions (
    role_id       CHAR(36)     NOT NULL,
    permission_id CHAR(36)     NOT NULL,
    created_at    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (role_id, permission_id),
    CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissions_perm FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Join: user ↔ role (N:N)
CREATE TABLE user_roles (
    user_id     CHAR(36)     NOT NULL,
    role_id     CHAR(36)     NOT NULL,
    created_at  DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (user_id, role_id),
    INDEX idx_user_roles_role (role_id),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- JWT Refresh tokens — hash only (security), JTI for revocation
CREATE TABLE auth_refresh_tokens (
    id                CHAR(36)      NOT NULL,
    user_id           CHAR(36)      NOT NULL,
    jti               CHAR(36)      NOT NULL,       -- JWT ID for token identification
    token_hash        BINARY(32)    NOT NULL,        -- SHA-256 of raw refresh token
    issued_at         DATETIME(6)   NOT NULL,
    expires_at        DATETIME(6)   NOT NULL,
    revoked_at        DATETIME(6)   NULL,
    rotated_from_id   CHAR(36)      NULL,            -- Previous token in rotation chain

    device_info       JSON          NULL,            -- { "os": "iOS", "app_version": "1.0" }
    ip_address        VARCHAR(45)   NULL,            -- IPv4/IPv6
    user_agent        VARCHAR(512)  NULL,

    created_at        DATETIME(6)   NOT NULL,
    updated_at        DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_refresh_jti (jti),
    UNIQUE KEY uk_refresh_token_hash (token_hash),
    INDEX idx_refresh_user_expires (user_id, expires_at),
    CONSTRAINT fk_refresh_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_refresh_rotated FOREIGN KEY (rotated_from_id) REFERENCES auth_refresh_tokens(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Audit trail — immutable log of data changes
CREATE TABLE audit_events (
    id              CHAR(36)      NOT NULL,
    actor_user_id   CHAR(36)      NULL,
    action          VARCHAR(50)   NOT NULL,       -- CREATE, UPDATE, DELETE, LOGIN...
    entity_table    VARCHAR(80)   NOT NULL,       -- Target table name
    entity_id       CHAR(36)      NULL,           -- Target row ID
    before_json     JSON          NULL,           -- Snapshot before change
    after_json      JSON          NULL,           -- Snapshot after change
    ip_address      VARCHAR(45)   NULL,
    user_agent      VARCHAR(512)  NULL,
    created_at      DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    INDEX idx_audit_entity (entity_table, entity_id, created_at),
    INDEX idx_audit_actor (actor_user_id, created_at),
    CONSTRAINT fk_audit_actor FOREIGN KEY (actor_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 3: PREGNANCY CORE (Week 3) ✅
-- Pregnancy tracking, conditions, prenatal visits, lab tests.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE pregnancies (
    id                       CHAR(36)      NOT NULL,
    user_id                  CHAR(36)      NOT NULL,
    pregnancy_no             INT           NOT NULL DEFAULT 1,
    status                   VARCHAR(20)   NOT NULL DEFAULT 'Active',
                                                    -- Enum: Active | Ended | Miscarriage | Delivered

    -- Dates
    lmp_date                 DATE          NULL,     -- Last Menstrual Period
    edd_date                 DATE          NULL,     -- Expected Delivery Date
    conception_date          DATE          NULL,     -- Estimated conception
    current_week             INT           NULL,     -- Cached gestational week (compute in app)
    notes                    TEXT          NULL,

    -- Baby info
    baby_nickname            VARCHAR(100)  NULL,
    baby_gender              VARCHAR(20)   NOT NULL DEFAULT 'Unknown',
                                                    -- Enum: Unknown | Male | Female
    pregnancy_type           VARCHAR(20)   NOT NULL DEFAULT 'Singleton',
                                                    -- Enum: Singleton | Twins | Triplets | Other

    -- Mother medical baseline (used by Weight + Nutrition modules)
    mother_blood_type        VARCHAR(10)   NULL,
    pre_pregnancy_weight_kg  DECIMAL(5,2)  NULL,
    height_cm                DECIMAL(5,2)  NULL,

    -- Obstetric details
    due_date_source          VARCHAR(20)   NOT NULL DEFAULT 'LMP',
                                                    -- Enum: LMP | Ultrasound | IVF | Manual
    gravida                  INT           NULL,     -- Total pregnancies (including current)
    para                     INT           NULL,     -- Previous deliveries
    actual_delivery_date     DATE          NULL,
    delivery_method          VARCHAR(20)   NULL,     -- Enum: Natural | Cesarean | Assisted
    cover_image_url          VARCHAR(500)  NULL,

    created_at               DATETIME(6)   NOT NULL,
    updated_at               DATETIME(6)   NOT NULL,
    deleted_at               DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_pregnancies_user_no (user_id, pregnancy_no),
    INDEX idx_pregnancies_user_status (user_id, status),
    INDEX idx_pregnancies_edd (edd_date),
    CONSTRAINT fk_pregnancies_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Reference: pregnancy conditions catalog (seed data)
CREATE TABLE ref_pregnancy_conditions (
    id          CHAR(36)      NOT NULL,
    code        VARCHAR(50)   NOT NULL,            -- GESTATIONAL_DIABETES, PREECLAMPSIA...
    is_active   TINYINT(1)    NOT NULL DEFAULT 1,

    created_at  DATETIME(6)   NOT NULL,
    updated_at  DATETIME(6)   NOT NULL,
    deleted_at  DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ref_preg_cond_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ref_pregnancy_condition_translations (
    condition_id   CHAR(36)      NOT NULL,
    language_code  VARCHAR(10)   NOT NULL,
    display_name   VARCHAR(120)  NOT NULL,
    description    VARCHAR(500)  NULL,

    PRIMARY KEY (condition_id, language_code),
    CONSTRAINT fk_ref_preg_cond_tr_cond FOREIGN KEY (condition_id) REFERENCES ref_pregnancy_conditions(id) ON DELETE CASCADE,
    CONSTRAINT fk_ref_preg_cond_tr_lang FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User's actual conditions for a specific pregnancy
CREATE TABLE pregnancy_conditions (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    condition_id    CHAR(36)      NOT NULL,
    diagnosed_date  DATE          NULL,
    severity        VARCHAR(20)   NULL,            -- Enum: Mild | Moderate | Severe
    notes           VARCHAR(500)  NULL,

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_pregnancy_condition (pregnancy_id, condition_id),
    CONSTRAINT fk_preg_cond_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT fk_preg_cond_ref       FOREIGN KEY (condition_id) REFERENCES ref_pregnancy_conditions(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Prenatal visits (checkups)
CREATE TABLE prenatal_visits (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    doctor_id       CHAR(36)      NULL,            -- FK → doctor_profiles (added in Section 9)
    visit_date_time DATETIME(6)   NOT NULL,
    visit_type      VARCHAR(20)   NOT NULL DEFAULT 'Routine',
                                                   -- Enum: Routine | Emergency | FollowUp | LabOnly | Other
    location        VARCHAR(255)  NULL,
    notes           TEXT          NULL,
    vitals_json     JSON          NULL,            -- { "bloodPressure": "120/80", "weightKg": 65.5, ... }

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_prenatal_visits_pregnancy (pregnancy_id, visit_date_time),
    INDEX idx_prenatal_visits_doctor (doctor_id, visit_date_time),
    CONSTRAINT fk_prenatal_visits_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
    -- FK to doctor_profiles will be added in Section 9 when doctor module is built
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Reference: test type catalog (CBC, OGTT, ULTRASOUND...)
CREATE TABLE ref_test_types (
    id          CHAR(36)      NOT NULL,
    code        VARCHAR(50)   NOT NULL,
    category    VARCHAR(50)   NULL,                -- LAB | IMAGING | OTHER
    is_active   TINYINT(1)    NOT NULL DEFAULT 1,

    created_at  DATETIME(6)   NOT NULL,
    updated_at  DATETIME(6)   NOT NULL,
    deleted_at  DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ref_test_types_code (code),
    INDEX idx_ref_test_types_category (category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ref_test_type_translations (
    test_type_id   CHAR(36)      NOT NULL,
    language_code  VARCHAR(10)   NOT NULL,
    display_name   VARCHAR(120)  NOT NULL,
    description    VARCHAR(500)  NULL,

    PRIMARY KEY (test_type_id, language_code),
    CONSTRAINT fk_ref_test_type_tr_type FOREIGN KEY (test_type_id) REFERENCES ref_test_types(id) ON DELETE CASCADE,
    CONSTRAINT fk_ref_test_type_tr_lang FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Individual test results
CREATE TABLE prenatal_tests (
    id                  CHAR(36)      NOT NULL,
    pregnancy_id        CHAR(36)      NOT NULL,
    visit_id            CHAR(36)      NULL,
    test_type_id        CHAR(36)      NOT NULL,
    test_date_time      DATETIME(6)   NOT NULL,
    result_text         TEXT          NULL,         -- Human-readable result
    result_json         JSON          NULL,         -- Structured result data
    is_abnormal_result  TINYINT(1)    NOT NULL DEFAULT 0,

    created_at          DATETIME(6)   NOT NULL,
    updated_at          DATETIME(6)   NOT NULL,
    deleted_at          DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_prenatal_tests_pregnancy (pregnancy_id, test_date_time),
    INDEX idx_prenatal_tests_type (test_type_id, test_date_time),
    CONSTRAINT fk_prenatal_tests_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT fk_prenatal_tests_visit     FOREIGN KEY (visit_id) REFERENCES prenatal_visits(id) ON DELETE SET NULL,
    CONSTRAINT fk_prenatal_tests_type      FOREIGN KEY (test_type_id) REFERENCES ref_test_types(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 4: SHARED FILE STORAGE (Week 4) ✅
-- Centralized storage for all uploaded files: documents, chat, avatars...
-- Week 4: StubFileStorageService (metadata only)
-- Week 5: SupabaseStorageService (real upload to Supabase)
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE storage_files (
    id                  CHAR(36)       NOT NULL,
    owner_user_id       CHAR(36)       NULL,        -- User who uploaded (NULL for system files)

    storage_provider    VARCHAR(32)    NOT NULL DEFAULT 'stub',
                                                    -- 'stub' (W4) → 'supabase' (W5)
    bucket_name         VARCHAR(128)   NULL,         -- Supabase bucket name
    object_key          VARCHAR(500)   NOT NULL,     -- Path in storage: "2026/02/11/{guid}.jpg"
    public_url          VARCHAR(1000)  NULL,         -- Publicly accessible URL

    original_file_name  VARCHAR(255)   NULL,         -- Original filename user uploaded
    mime_type           VARCHAR(100)   NOT NULL,     -- image/jpeg, application/pdf...
    file_size_bytes     BIGINT         NOT NULL,
    checksum_sha256     BINARY(32)     NULL,         -- SHA-256 integrity check

    uploaded_at         DATETIME(6)    NOT NULL,

    created_at          DATETIME(6)    NOT NULL,
    updated_at          DATETIME(6)    NOT NULL,
    deleted_at          DATETIME(6)    NULL,

    PRIMARY KEY (id),
    INDEX idx_storage_files_owner (owner_user_id, uploaded_at),
    INDEX idx_storage_files_object (storage_provider, object_key(191)),
    CONSTRAINT fk_storage_files_owner FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 5: MEDICAL DOCUMENTS + OCR (Week 4) ✅
-- Upload ảnh phiếu khám, OCR pipeline, IsFavorite toggle.
--
-- ⚠️ SIMPLIFIED vs old schema:
--   • REMOVED document_files (MedicalDocument → StorageFile direct FK)
--   • REMOVED medical_field_definitions + extracted_medical_fields
--     (replaced by PrenatalVisit.VitalsJson + PrenatalTest.ResultJson + OcrResult.StructuredJson)
-- ═══════════════════════════════════════════════════════════════════════════════

-- Reference: document type catalog
CREATE TABLE ref_document_types (
    id          CHAR(36)      NOT NULL,
    code        VARCHAR(50)   NOT NULL,            -- PRENATAL_CHECKUP, ULTRASOUND, BLOOD_TEST...
    is_active   TINYINT(1)    NOT NULL DEFAULT 1,

    created_at  DATETIME(6)   NOT NULL,
    updated_at  DATETIME(6)   NOT NULL,
    deleted_at  DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ref_doc_types_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ref_document_type_translations (
    document_type_id  CHAR(36)      NOT NULL,
    language_code     VARCHAR(10)   NOT NULL,
    display_name      VARCHAR(200)  NOT NULL,
    description       TEXT          NULL,

    PRIMARY KEY (document_type_id, language_code),
    CONSTRAINT fk_ref_doc_type_tr_type FOREIGN KEY (document_type_id) REFERENCES ref_document_types(id) ON DELETE CASCADE,
    CONSTRAINT fk_ref_doc_type_tr_lang FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Medical document — links uploaded file to pregnancy
-- ⚠️ Direct FK to storage_files (NO document_files intermediate table)
CREATE TABLE medical_documents (
    id                CHAR(36)      NOT NULL,
    pregnancy_id      CHAR(36)      NOT NULL,
    visit_id          CHAR(36)      NULL,          -- Populated by OCR/AI after extraction
    document_type_id  CHAR(36)      NULL,
    storage_file_id   CHAR(36)      NOT NULL,      -- Direct link to uploaded file

    title             VARCHAR(200)  NULL,
    document_date     DATE          NULL,           -- Date on the physical document
    captured_at       DATETIME(6)   NOT NULL,       -- When user uploaded/captured
    source            VARCHAR(20)   NOT NULL DEFAULT 'Upload',
                                                    -- Enum: Upload | Share | Import
    notes             TEXT          NULL,
    is_favorite       TINYINT(1)    NOT NULL DEFAULT 0,  -- Toggle yêu thích (replaced tags)

    created_at        DATETIME(6)   NOT NULL,
    updated_at        DATETIME(6)   NOT NULL,
    deleted_at        DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_medical_docs_pregnancy (pregnancy_id, captured_at),
    INDEX idx_medical_docs_visit (visit_id),
    INDEX idx_medical_docs_type (pregnancy_id, document_type_id, captured_at),
    CONSTRAINT fk_medical_docs_pregnancy    FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT fk_medical_docs_visit        FOREIGN KEY (visit_id) REFERENCES prenatal_visits(id) ON DELETE SET NULL,
    CONSTRAINT fk_medical_docs_type         FOREIGN KEY (document_type_id) REFERENCES ref_document_types(id) ON DELETE SET NULL,
    CONSTRAINT fk_medical_docs_storage_file FOREIGN KEY (storage_file_id) REFERENCES storage_files(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- OCR results — multi-run support, includes AI extraction fields (Week 5)
CREATE TABLE ocr_results (
    id                      CHAR(36)       NOT NULL,
    document_id             CHAR(36)       NOT NULL,  -- FK → medical_documents (NOT document_files)
    ocr_run_no              INT            NOT NULL DEFAULT 1,
    status                  VARCHAR(20)    NOT NULL DEFAULT 'Pending',
                                                       -- W4: Pending | Processing | Succeeded | Failed
                                                       -- W5: Pending | OcrProcessing | OcrCompleted | AiExtracting | Succeeded | Failed
    engine                  VARCHAR(80)    NULL,        -- OCR engine: "azure-document-intelligence", "stub-v1"
    language_hint           VARCHAR(10)    NULL,        -- "vi", "en"

    raw_text                LONGTEXT       NULL,        -- Raw OCR output text
    structured_json         JSON           NULL,        -- AI-extracted structured data (Gemini output)
    confidence              DECIMAL(5,2)   NULL,        -- Overall confidence 0.00–100.00
    error_message           TEXT           NULL,

    -- Week 5: AI Processing metrics
    ocr_processing_time_ms  INT            NULL,        -- Azure OCR duration
    ai_model_used           VARCHAR(50)    NULL,        -- "gemini-2.0-flash"
    ai_tokens_used          INT            NULL,        -- Total tokens (prompt + completion)
    ai_processing_time_ms   INT            NULL,        -- Gemini processing duration
    ai_prompt_template_id   CHAR(36)       NULL,        -- Which prompt template was used

    created_at              DATETIME(6)    NOT NULL,
    updated_at              DATETIME(6)    NOT NULL,
    deleted_at              DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ocr_results_doc_run (document_id, ocr_run_no),
    INDEX idx_ocr_results_status (document_id, status),
    FULLTEXT KEY ft_ocr_raw_text (raw_text),
    CONSTRAINT fk_ocr_results_document FOREIGN KEY (document_id) REFERENCES medical_documents(id) ON DELETE CASCADE
    -- FK to ai_prompt_templates added in Section 6
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NOTE: Tags (tags + medical_document_tags) were REMOVED.
-- Replaced by is_favorite boolean on medical_documents for simpler UX.


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 6: AI INFRASTRUCTURE (Week 5) ✅
-- Versioned prompt templates + general AI request logging.
-- Reusable for: Medical Record Extraction, Nutrition AI, Doctor Chat AI.
-- ═══════════════════════════════════════════════════════════════════════════════

-- Prompt templates with versioning + Rule Layer system
CREATE TABLE ai_prompt_templates (
    id                CHAR(36)       NOT NULL,
    template_key      VARCHAR(100)   NOT NULL,     -- 'medical_record.extraction', 'nutrition.meal_planning'
    version           INT            NOT NULL DEFAULT 1,
    display_name      VARCHAR(200)   NOT NULL,
    description       TEXT           NULL,

    -- Rule Layers (assembled by PromptBuilder)
    system_rules      TEXT           NOT NULL,      -- Layer 1: Language, format, safety
    domain_rules      TEXT           NULL,           -- Layer 2: Pregnancy domain knowledge (shared)
    feature_rules     TEXT           NOT NULL,       -- Layer 3: Feature-specific instructions
    output_schema     TEXT           NULL,           -- JSON schema for expected AI output

    -- Model Configuration
    model_name        VARCHAR(50)    NOT NULL DEFAULT 'gemini-2.0-flash',
    temperature       DECIMAL(3,2)   NOT NULL DEFAULT 0.10,
    max_output_tokens INT            NOT NULL DEFAULT 4096,

    is_active         TINYINT(1)     NOT NULL DEFAULT 1,

    created_at        DATETIME(6)    NOT NULL,
    updated_at        DATETIME(6)    NOT NULL,
    deleted_at        DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ai_templates_key_version (template_key, version)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add FK from ocr_results → ai_prompt_templates (deferred from Section 5)
ALTER TABLE ocr_results
    ADD CONSTRAINT fk_ocr_results_ai_template
        FOREIGN KEY (ai_prompt_template_id) REFERENCES ai_prompt_templates(id) ON DELETE SET NULL;

-- General-purpose AI request log
-- Tracks ALL AI interactions: medical extraction, nutrition, chat...
-- Replaces old nutrition_ai_requests with a generalized table.
CREATE TABLE ai_request_logs (
    id                    CHAR(36)      NOT NULL,
    feature               VARCHAR(50)   NOT NULL,   -- MEDICAL_EXTRACTION | NUTRITION_MEAL_PLAN | NUTRITION_CHAT | DOCTOR_CHAT
    pregnancy_id          CHAR(36)      NULL,
    user_id               CHAR(36)      NULL,        -- Who triggered the request
    template_id           CHAR(36)      NULL,        -- Which prompt template was used

    status                VARCHAR(20)   NOT NULL DEFAULT 'Pending',
                                                     -- Pending | Processing | Succeeded | Failed
    model                 VARCHAR(80)   NULL,         -- Actual model used
    prompt_version        VARCHAR(64)   NULL,         -- Template version info

    request_payload       JSON          NULL,         -- Full prompt sent to AI
    response_payload      JSON          NULL,         -- Full AI response

    tokens_input          INT           NULL,
    tokens_output         INT           NULL,
    processing_time_ms    INT           NULL,
    error_message         VARCHAR(500)  NULL,

    created_at            DATETIME(6)   NOT NULL,
    updated_at            DATETIME(6)   NOT NULL,
    deleted_at            DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_ai_logs_feature (feature, created_at),
    INDEX idx_ai_logs_pregnancy (pregnancy_id, created_at),
    INDEX idx_ai_logs_status (status, created_at),
    CONSTRAINT fk_ai_logs_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE SET NULL,
    CONSTRAINT fk_ai_logs_user      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_ai_logs_template  FOREIGN KEY (template_id) REFERENCES ai_prompt_templates(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 7: WEIGHT TRACKING + MOTIVATIONAL TEMPLATES (Week 6) ✅
-- Daily weight logging, goal ranges based on BMI, alerts.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE weight_logs (
    id              CHAR(36)       NOT NULL,
    pregnancy_id    CHAR(36)       NOT NULL,
    logged_on       DATE           NOT NULL,        -- One entry per day
    weight_kg       DECIMAL(5,2)   NOT NULL,
    note            VARCHAR(255)   NULL,
    source          VARCHAR(20)    NOT NULL DEFAULT 'Manual',
                                                    -- Manual | OCR

    created_at      DATETIME(6)    NOT NULL,
    updated_at      DATETIME(6)    NOT NULL,
    deleted_at      DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_weight_logs_pregnancy_date (pregnancy_id, logged_on),
    CONSTRAINT fk_weight_logs_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT chk_weight_kg CHECK (weight_kg > 0 AND weight_kg < 500)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Weight goal (one per pregnancy, based on pre-pregnancy BMI)
CREATE TABLE weight_goal_ranges (
    id                          CHAR(36)       NOT NULL,
    pregnancy_id                CHAR(36)       NOT NULL,
    height_cm                   DECIMAL(5,2)   NULL,
    pre_pregnancy_weight_kg     DECIMAL(5,2)   NULL,
    bmi                         DECIMAL(5,2)   NULL,
    recommended_total_gain_min  DECIMAL(5,2)   NULL,  -- IOM guidelines
    recommended_total_gain_max  DECIMAL(5,2)   NULL,
    notes                       VARCHAR(500)   NULL,

    created_at                  DATETIME(6)    NOT NULL,
    updated_at                  DATETIME(6)    NOT NULL,
    deleted_at                  DATETIME(6)    NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_weight_goals_pregnancy (pregnancy_id),
    CONSTRAINT fk_weight_goals_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE weight_alerts (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    alert_type      VARCHAR(64)   NOT NULL,        -- RapidGain, RapidLoss, AboveRange, BelowRange
    triggered_at    DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    details_json    JSON          NULL,             -- { "currentWeight": 70, "expectedRange": [65,68] }
    resolved_at     DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_weight_alerts_pregnancy (pregnancy_id, triggered_at),
    INDEX idx_weight_alerts_type (alert_type, triggered_at),
    CONSTRAINT fk_weight_alerts_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Motivational content — tips, baby size comparisons, milestones (per gestational week)
CREATE TABLE motivational_templates (
    id               CHAR(36)      NOT NULL,
    category         VARCHAR(30)   NOT NULL DEFAULT 'BabySize',
                                                   -- BabySize | Milestone | Tip
    week_start       INT           NOT NULL,
    week_end         INT           NOT NULL,
    is_active        TINYINT(1)    NOT NULL DEFAULT 1,
    variables_json   JSON          NULL,            -- Template variable definitions

    created_at       DATETIME(6)   NOT NULL,
    updated_at       DATETIME(6)   NOT NULL,
    deleted_at       DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_motivational_week (week_start, week_end, is_active),
    CONSTRAINT chk_motivational_week CHECK (week_start >= 0 AND week_end >= week_start AND week_end <= 45)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE motivational_template_translations (
    template_id  CHAR(36)      NOT NULL,
    language_code VARCHAR(10)  NOT NULL,
    title        VARCHAR(120)  NULL,
    message      VARCHAR(500)  NOT NULL,

    PRIMARY KEY (template_id, language_code),
    CONSTRAINT fk_motivational_tr_template FOREIGN KEY (template_id) REFERENCES motivational_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_motivational_tr_lang     FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 8: NUTRITION + MEAL PLANNING (Week 7) ⬜
-- Food preferences, AI-generated meal plans, recipes, nutrient tracking.
-- Uses IAiProvider + PromptBuilder infrastructure from Week 5.
-- ═══════════════════════════════════════════════════════════════════════════════

-- Reference: food item catalog
CREATE TABLE ref_food_items (
    id          CHAR(36)      NOT NULL,
    code        VARCHAR(80)   NOT NULL,
    is_active   TINYINT(1)    NOT NULL DEFAULT 1,

    created_at  DATETIME(6)   NOT NULL,
    updated_at  DATETIME(6)   NOT NULL,
    deleted_at  DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ref_food_items_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ref_food_item_translations (
    food_item_id   CHAR(36)      NOT NULL,
    language_code  VARCHAR(10)   NOT NULL,
    display_name   VARCHAR(120)  NOT NULL,

    PRIMARY KEY (food_item_id, language_code),
    CONSTRAINT fk_ref_food_tr_item FOREIGN KEY (food_item_id) REFERENCES ref_food_items(id) ON DELETE CASCADE,
    CONSTRAINT fk_ref_food_tr_lang FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Reference: nutrient catalog
CREATE TABLE ref_nutrients (
    id          CHAR(36)      NOT NULL,
    code        VARCHAR(50)   NOT NULL,            -- CALORIES, PROTEIN, IRON, FOLIC_ACID...
    unit        VARCHAR(20)   NOT NULL,            -- kcal, g, mg, mcg
    is_active   TINYINT(1)    NOT NULL DEFAULT 1,

    created_at  DATETIME(6)   NOT NULL,
    updated_at  DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ref_nutrients_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ref_nutrient_translations (
    nutrient_id    CHAR(36)      NOT NULL,
    language_code  VARCHAR(10)   NOT NULL,
    display_name   VARCHAR(120)  NOT NULL,

    PRIMARY KEY (nutrient_id, language_code),
    CONSTRAINT fk_ref_nutrient_tr_nutrient FOREIGN KEY (nutrient_id) REFERENCES ref_nutrients(id) ON DELETE CASCADE,
    CONSTRAINT fk_ref_nutrient_tr_lang     FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Pregnancy-scoped food preferences (allergies, dislikes)
CREATE TABLE pregnancy_food_preferences (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    food_item_id    CHAR(36)      NOT NULL,
    preference_type VARCHAR(20)   NOT NULL,        -- ALLERGY | DISLIKE
    severity        VARCHAR(20)   NULL,            -- LOW | MEDIUM | HIGH (for allergies)
    notes           VARCHAR(255)  NULL,

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_food_pref_pregnancy (pregnancy_id, food_item_id, preference_type),
    CONSTRAINT fk_food_pref_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT fk_food_pref_item      FOREIGN KEY (food_item_id) REFERENCES ref_food_items(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Free-text dietary notes per pregnancy
CREATE TABLE pregnancy_nutrition_notes (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    note_type       VARCHAR(20)   NOT NULL DEFAULT 'NOTE',
                                                   -- DIET | NOTE | OTHER
    value_text      VARCHAR(200)  NOT NULL,

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_nutrition_notes_pregnancy (pregnancy_id, created_at),
    CONSTRAINT fk_nutrition_notes_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Recipes (pregnancy-scoped: dietary needs vary per pregnancy)
CREATE TABLE recipes (
    id              CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NOT NULL,
    title           VARCHAR(200)  NOT NULL,
    instructions    LONGTEXT      NULL,
    servings        INT           NULL,
    prep_minutes    INT           NULL,
    cook_minutes    INT           NULL,

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_recipes_pregnancy (pregnancy_id, created_at),
    CONSTRAINT fk_recipes_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Meal plans (AI-generated or manual)
CREATE TABLE meal_plans (
    id                  CHAR(36)      NOT NULL,
    pregnancy_id        CHAR(36)      NOT NULL,
    ai_request_log_id   CHAR(36)      NULL,         -- FK → ai_request_logs (if AI-generated)
    start_date          DATE          NOT NULL,
    end_date            DATE          NOT NULL,
    source              VARCHAR(20)   NOT NULL DEFAULT 'AI',
                                                     -- AI | MANUAL
    title               VARCHAR(200)  NULL,
    notes               TEXT          NULL,

    created_at          DATETIME(6)   NOT NULL,
    updated_at          DATETIME(6)   NOT NULL,
    deleted_at          DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_meal_plans_pregnancy (pregnancy_id, start_date),
    CONSTRAINT fk_meal_plans_pregnancy  FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE CASCADE,
    CONSTRAINT fk_meal_plans_ai_request FOREIGN KEY (ai_request_log_id) REFERENCES ai_request_logs(id) ON DELETE SET NULL,
    CONSTRAINT chk_meal_plan_dates CHECK (end_date >= start_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE meal_plan_days (
    id              CHAR(36)      NOT NULL,
    meal_plan_id    CHAR(36)      NOT NULL,
    plan_date       DATE          NOT NULL,

    created_at      DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_meal_plan_days (meal_plan_id, plan_date),
    CONSTRAINT fk_meal_plan_days_plan FOREIGN KEY (meal_plan_id) REFERENCES meal_plans(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Individual meal items within a day
CREATE TABLE meal_items (
    id              CHAR(36)      NOT NULL,
    meal_day_id     CHAR(36)      NOT NULL,
    meal_type       VARCHAR(20)   NOT NULL,         -- BREAKFAST | LUNCH | DINNER | SNACK
    recipe_id       CHAR(36)      NULL,
    item_name       VARCHAR(200)  NULL,              -- Fallback name if no recipe
    portion_text    VARCHAR(120)  NULL,              -- "1 chén cơm", "200ml sữa"
    calories_kcal   INT           NULL,
    notes           VARCHAR(255)  NULL,

    created_at      DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    INDEX idx_meal_items_day_type (meal_day_id, meal_type),
    CONSTRAINT fk_meal_items_day    FOREIGN KEY (meal_day_id) REFERENCES meal_plan_days(id) ON DELETE CASCADE,
    CONSTRAINT fk_meal_items_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id) ON DELETE SET NULL,
    CONSTRAINT chk_meal_item_name CHECK (recipe_id IS NOT NULL OR item_name IS NOT NULL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Nutrient breakdown per meal item
CREATE TABLE meal_item_nutrients (
    meal_item_id  CHAR(36)       NOT NULL,
    nutrient_id   CHAR(36)       NOT NULL,
    amount        DECIMAL(10,3)  NOT NULL,

    PRIMARY KEY (meal_item_id, nutrient_id),
    CONSTRAINT fk_meal_nutrients_item     FOREIGN KEY (meal_item_id) REFERENCES meal_items(id) ON DELETE CASCADE,
    CONSTRAINT fk_meal_nutrients_nutrient FOREIGN KEY (nutrient_id) REFERENCES ref_nutrients(id),
    CONSTRAINT chk_nutrient_amount CHECK (amount >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User feedback on meal plans
CREATE TABLE meal_plan_feedback (
    id              CHAR(36)      NOT NULL,
    meal_plan_id    CHAR(36)      NOT NULL,
    user_id         CHAR(36)      NOT NULL,
    rating          TINYINT       NOT NULL,         -- 1-5
    comment         VARCHAR(500)  NULL,

    created_at      DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_meal_plan_feedback (meal_plan_id, user_id),
    CONSTRAINT fk_meal_plan_fb_plan FOREIGN KEY (meal_plan_id) REFERENCES meal_plans(id) ON DELETE CASCADE,
    CONSTRAINT fk_meal_plan_fb_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT chk_plan_rating CHECK (rating BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User feedback on individual meal items
CREATE TABLE meal_item_feedback (
    id              CHAR(36)      NOT NULL,
    meal_item_id    CHAR(36)      NOT NULL,
    user_id         CHAR(36)      NOT NULL,
    liked           TINYINT(1)    NOT NULL,         -- 0 = disliked, 1 = liked
    comment         VARCHAR(300)  NULL,

    created_at      DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_meal_item_feedback (meal_item_id, user_id),
    CONSTRAINT fk_meal_item_fb_item FOREIGN KEY (meal_item_id) REFERENCES meal_items(id) ON DELETE CASCADE,
    CONSTRAINT fk_meal_item_fb_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 9: DOCTOR PROFILES + SPECIALTIES + SCHEDULING (Week 8+) ⬜
-- Doctor registration, specialty management, availability rules + slots.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE doctor_profiles (
    id                CHAR(36)      NOT NULL,
    user_id           CHAR(36)      NULL,           -- Doctor = User with DOCTOR role
    full_name         VARCHAR(150)  NOT NULL,
    license_no        VARCHAR(80)   NOT NULL,
    license_country   VARCHAR(5)    NOT NULL DEFAULT 'VN',
    clinic_name       VARCHAR(200)  NULL,
    bio               TEXT          NULL,
    phone             VARCHAR(20)   NULL,
    email             VARCHAR(320)  NULL,
    status            VARCHAR(20)   NOT NULL DEFAULT 'Active',
                                                    -- Active | Inactive | Suspended

    created_at        DATETIME(6)   NOT NULL,
    updated_at        DATETIME(6)   NOT NULL,
    deleted_at        DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_doctor_license (license_no, license_country),
    UNIQUE KEY uk_doctor_user (user_id),
    INDEX idx_doctor_status (status),
    CONSTRAINT fk_doctor_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add deferred FK: prenatal_visits.doctor_id → doctor_profiles
ALTER TABLE prenatal_visits
    ADD CONSTRAINT fk_prenatal_visits_doctor
        FOREIGN KEY (doctor_id) REFERENCES doctor_profiles(id) ON DELETE SET NULL;

-- Reference: medical specialties
CREATE TABLE ref_specialties (
    id          CHAR(36)      NOT NULL,
    code        VARCHAR(50)   NOT NULL,            -- OBSTETRICS, GYNECOLOGY, NUTRITION...
    is_active   TINYINT(1)    NOT NULL DEFAULT 1,

    created_at  DATETIME(6)   NOT NULL,
    updated_at  DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_ref_specialties_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ref_specialty_translations (
    specialty_id   CHAR(36)      NOT NULL,
    language_code  VARCHAR(10)   NOT NULL,
    display_name   VARCHAR(120)  NOT NULL,
    description    VARCHAR(500)  NULL,

    PRIMARY KEY (specialty_id, language_code),
    CONSTRAINT fk_ref_spec_tr_spec FOREIGN KEY (specialty_id) REFERENCES ref_specialties(id) ON DELETE CASCADE,
    CONSTRAINT fk_ref_spec_tr_lang FOREIGN KEY (language_code) REFERENCES languages(code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Join: doctor ↔ specialty (N:N)
CREATE TABLE doctor_specialties (
    doctor_id     CHAR(36)     NOT NULL,
    specialty_id  CHAR(36)     NOT NULL,
    is_primary    TINYINT(1)   NOT NULL DEFAULT 0,

    created_at    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (doctor_id, specialty_id),
    INDEX idx_doctor_spec_specialty (specialty_id),
    CONSTRAINT fk_doctor_spec_doctor    FOREIGN KEY (doctor_id) REFERENCES doctor_profiles(id) ON DELETE CASCADE,
    CONSTRAINT fk_doctor_spec_specialty FOREIGN KEY (specialty_id) REFERENCES ref_specialties(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Availability rules (JSON-based, flexible scheduling)
CREATE TABLE doctor_availability_rules (
    id            CHAR(36)      NOT NULL,
    doctor_id     CHAR(36)      NOT NULL,
    rule_type     VARCHAR(20)   NOT NULL,          -- WEEKLY | EXCEPTION | TIME_OFF
    rule_json     JSON          NOT NULL,           -- Schedule definition
    valid_from    DATE          NULL,
    valid_to      DATE          NULL,

    created_at    DATETIME(6)   NOT NULL,
    updated_at    DATETIME(6)   NOT NULL,
    deleted_at    DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_avail_rules_doctor (doctor_id),
    INDEX idx_avail_rules_valid (valid_from, valid_to),
    CONSTRAINT fk_avail_rules_doctor FOREIGN KEY (doctor_id) REFERENCES doctor_profiles(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Materialized time slots (generated from rules, queryable for booking)
CREATE TABLE doctor_availability_slots (
    id                CHAR(36)      NOT NULL,
    doctor_id         CHAR(36)      NOT NULL,
    slot_start        DATETIME(6)   NOT NULL,
    slot_end          DATETIME(6)   NOT NULL,
    duration_minutes  INT           NOT NULL DEFAULT 15,
    status            VARCHAR(20)   NOT NULL DEFAULT 'FREE',
                                                    -- FREE | HELD | BOOKED | BLOCKED
    source_rule_id    CHAR(36)      NULL,

    created_at        DATETIME(6)   NOT NULL,
    updated_at        DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_avail_slot_doctor_time (doctor_id, slot_start, slot_end),
    INDEX idx_avail_slots_status (doctor_id, status, slot_start),
    CONSTRAINT fk_avail_slots_doctor FOREIGN KEY (doctor_id) REFERENCES doctor_profiles(id) ON DELETE CASCADE,
    CONSTRAINT fk_avail_slots_rule   FOREIGN KEY (source_rule_id) REFERENCES doctor_availability_rules(id) ON DELETE SET NULL,
    CONSTRAINT chk_slot_time CHECK (slot_end > slot_start),
    CONSTRAINT chk_slot_duration CHECK (duration_minutes IN (10, 15, 20, 30, 45, 60))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 10: CONSULT + CHAT + CALL (Week 8+) ⬜
-- Consultation requests, real-time chat, video/audio calls.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE consult_requests (
    id                      CHAR(36)      NOT NULL,
    requester_user_id       CHAR(36)      NOT NULL,
    pregnancy_id            CHAR(36)      NULL,
    request_type            VARCHAR(20)   NOT NULL DEFAULT 'CHAT',
                                                   -- CHAT | CALL | BOTH
    status                  VARCHAR(30)   NOT NULL DEFAULT 'OPEN',
                                                   -- OPEN | ASSIGNED | SCHEDULED | IN_PROGRESS | COMPLETED | CANCELLED

    requested_specialty_id  CHAR(36)      NULL,
    assigned_doctor_id      CHAR(36)      NULL,

    preferred_time_from     DATETIME(6)   NULL,
    preferred_time_to       DATETIME(6)   NULL,
    scheduled_time_from     DATETIME(6)   NULL,
    scheduled_time_to       DATETIME(6)   NULL,

    symptoms_json           JSON          NULL,     -- User-reported symptoms
    notes                   TEXT          NULL,

    created_at              DATETIME(6)   NOT NULL,
    updated_at              DATETIME(6)   NOT NULL,
    deleted_at              DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_consult_user (requester_user_id, created_at),
    INDEX idx_consult_status (status, created_at),
    INDEX idx_consult_doctor (assigned_doctor_id, status),
    CONSTRAINT fk_consult_user      FOREIGN KEY (requester_user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_consult_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE SET NULL,
    CONSTRAINT fk_consult_specialty FOREIGN KEY (requested_specialty_id) REFERENCES ref_specialties(id) ON DELETE SET NULL,
    CONSTRAINT fk_consult_doctor    FOREIGN KEY (assigned_doctor_id) REFERENCES doctor_profiles(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE chat_conversations (
    id                    CHAR(36)      NOT NULL,
    consult_request_id    CHAR(36)      NULL,
    conversation_type     VARCHAR(20)   NOT NULL DEFAULT 'CONSULT',
                                                  -- CONSULT | SUPPORT | AI_NUTRITION | OTHER
    title                 VARCHAR(200)  NULL,
    status                VARCHAR(20)   NOT NULL DEFAULT 'OPEN',
                                                  -- OPEN | CLOSED

    created_at            DATETIME(6)   NOT NULL,
    updated_at            DATETIME(6)   NOT NULL,
    deleted_at            DATETIME(6)   NULL,

    PRIMARY KEY (id),
    UNIQUE KEY uk_conversations_consult (consult_request_id),
    INDEX idx_conversations_updated (updated_at),
    CONSTRAINT fk_conversations_consult FOREIGN KEY (consult_request_id) REFERENCES consult_requests(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE chat_participants (
    conversation_id  CHAR(36)     NOT NULL,
    user_id          CHAR(36)     NOT NULL,
    role_in_chat     VARCHAR(20)  NOT NULL,        -- MOTHER | DOCTOR | SUPPORT | ADMIN
    joined_at        DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    left_at          DATETIME(6)  NULL,

    PRIMARY KEY (conversation_id, user_id),
    INDEX idx_chat_participants_user (user_id, joined_at),
    CONSTRAINT fk_chat_part_conversation FOREIGN KEY (conversation_id) REFERENCES chat_conversations(id) ON DELETE CASCADE,
    CONSTRAINT fk_chat_part_user         FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE chat_messages (
    id                CHAR(36)      NOT NULL,
    conversation_id   CHAR(36)      NOT NULL,
    sender_user_id    CHAR(36)      NULL,           -- NULL if system message
    sender_type       VARCHAR(20)   NOT NULL DEFAULT 'USER',
                                                    -- USER | SYSTEM | AI
    message_type      VARCHAR(20)   NOT NULL DEFAULT 'TEXT',
                                                    -- TEXT | IMAGE | FILE | SYSTEM
    content_text      LONGTEXT      NULL,
    content_json      JSON          NULL,            -- Structured content for non-text messages

    created_at        DATETIME(6)   NOT NULL,
    edited_at         DATETIME(6)   NULL,
    deleted_at        DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_chat_messages_conversation (conversation_id, created_at),
    INDEX idx_chat_messages_sender (sender_user_id, created_at),
    CONSTRAINT fk_chat_msg_conversation FOREIGN KEY (conversation_id) REFERENCES chat_conversations(id) ON DELETE CASCADE,
    CONSTRAINT fk_chat_msg_sender       FOREIGN KEY (sender_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- File attachments in chat messages
CREATE TABLE chat_message_attachments (
    id               CHAR(36)      NOT NULL,
    message_id       CHAR(36)      NOT NULL,
    storage_file_id  CHAR(36)      NOT NULL,
    caption          VARCHAR(255)  NULL,

    created_at       DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    INDEX idx_chat_attach_message (message_id),
    CONSTRAINT fk_chat_attach_message FOREIGN KEY (message_id) REFERENCES chat_messages(id) ON DELETE CASCADE,
    CONSTRAINT fk_chat_attach_file    FOREIGN KEY (storage_file_id) REFERENCES storage_files(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Read receipts
CREATE TABLE chat_read_receipts (
    message_id      CHAR(36)     NOT NULL,
    reader_user_id  CHAR(36)     NOT NULL,
    read_at         DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    PRIMARY KEY (message_id, reader_user_id),
    INDEX idx_read_receipts_user (reader_user_id, read_at),
    CONSTRAINT fk_read_receipt_message FOREIGN KEY (message_id) REFERENCES chat_messages(id) ON DELETE CASCADE,
    CONSTRAINT fk_read_receipt_user    FOREIGN KEY (reader_user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Video/audio call sessions
CREATE TABLE call_sessions (
    id                    CHAR(36)      NOT NULL,
    consult_request_id    CHAR(36)      NOT NULL,
    conversation_id       CHAR(36)      NULL,
    provider              VARCHAR(50)   NULL,       -- WebRTC provider name
    provider_session_id   VARCHAR(120)  NULL,
    status                VARCHAR(20)   NOT NULL DEFAULT 'INITIATED',
                                                    -- INITIATED | RINGING | CONNECTED | ENDED | FAILED | CANCELLED
    started_at            DATETIME(6)   NULL,
    ended_at              DATETIME(6)   NULL,
    duration_seconds      INT           NULL,       -- Computed in app (not GENERATED STORED — EF Core compatibility)
    recording_file_id     CHAR(36)      NULL,

    created_at            DATETIME(6)   NOT NULL,
    updated_at            DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    INDEX idx_call_sessions_consult (consult_request_id, created_at),
    INDEX idx_call_sessions_status (status, created_at),
    CONSTRAINT fk_call_consult    FOREIGN KEY (consult_request_id) REFERENCES consult_requests(id) ON DELETE CASCADE,
    CONSTRAINT fk_call_conversation FOREIGN KEY (conversation_id) REFERENCES chat_conversations(id) ON DELETE SET NULL,
    CONSTRAINT fk_call_recording  FOREIGN KEY (recording_file_id) REFERENCES storage_files(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- SECTION 11: REMINDERS (Future) ⬜
-- Configurable reminders: weight logging, medication, appointments.
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE TABLE reminder_rules (
    id              CHAR(36)      NOT NULL,
    user_id         CHAR(36)      NOT NULL,
    pregnancy_id    CHAR(36)      NULL,
    rule_type       VARCHAR(30)   NOT NULL,        -- WEIGHT_LOG | MEDICATION | APPOINTMENT | NUTRITION | CUSTOM
    rule_json       JSON          NOT NULL,         -- { "frequency": "daily", "time": "08:00", ... }
    next_run_at     DATETIME(6)   NULL,
    is_active       TINYINT(1)    NOT NULL DEFAULT 1,

    created_at      DATETIME(6)   NOT NULL,
    updated_at      DATETIME(6)   NOT NULL,
    deleted_at      DATETIME(6)   NULL,

    PRIMARY KEY (id),
    INDEX idx_reminder_rules_user (user_id, is_active, next_run_at),
    CONSTRAINT fk_reminder_rules_user      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_reminder_rules_pregnancy FOREIGN KEY (pregnancy_id) REFERENCES pregnancies(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE reminder_events (
    id              CHAR(36)      NOT NULL,
    rule_id         CHAR(36)      NOT NULL,
    scheduled_at    DATETIME(6)   NOT NULL,
    sent_at         DATETIME(6)   NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'SCHEDULED',
                                                   -- SCHEDULED | SENT | FAILED | SKIPPED
    error_message   VARCHAR(500)  NULL,

    created_at      DATETIME(6)   NOT NULL,

    PRIMARY KEY (id),
    INDEX idx_reminder_events_rule (rule_id, scheduled_at),
    INDEX idx_reminder_events_status (status, scheduled_at),
    CONSTRAINT fk_reminder_events_rule FOREIGN KEY (rule_id) REFERENCES reminder_rules(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- ═══════════════════════════════════════════════════════════════════════════════
-- TABLE SUMMARY BY FEATURE
-- ═══════════════════════════════════════════════════════════════════════════════
--
-- Total: 46 tables
--
-- Section 1  — Lookup (1 table):
--   languages
--
-- Section 2  — Users/Auth/RBAC (8 tables):
--   users, user_profiles, roles, permissions, role_permissions,
--   user_roles, auth_refresh_tokens, audit_events
--
-- Section 3  — Pregnancy Core (7 tables):
--   pregnancies, ref_pregnancy_conditions, ref_pregnancy_condition_translations,
--   pregnancy_conditions, prenatal_visits, ref_test_types, ref_test_type_translations,
--   prenatal_tests
--
-- Section 4  — File Storage (1 table):
--   storage_files
--
-- Section 5  — Medical Documents (3 tables):
--   ref_document_types, ref_document_type_translations, medical_documents,
--   ocr_results
--   (tags + medical_document_tags REMOVED → replaced by is_favorite on medical_documents)
--
-- Section 6  — AI Infrastructure (2 tables):
--   ai_prompt_templates, ai_request_logs
--
-- Section 7  — Weight + Motivational (5 tables):
--   weight_logs, weight_goal_ranges, weight_alerts,
--   motivational_templates, motivational_template_translations
--
-- Section 8  — Nutrition (10 tables):
--   ref_food_items, ref_food_item_translations, ref_nutrients, ref_nutrient_translations,
--   pregnancy_food_preferences, pregnancy_nutrition_notes, recipes,
--   meal_plans, meal_plan_days, meal_items, meal_item_nutrients,
--   meal_plan_feedback, meal_item_feedback
--
-- Section 9  — Doctors (5 tables):
--   doctor_profiles, ref_specialties, ref_specialty_translations,
--   doctor_specialties, doctor_availability_rules, doctor_availability_slots
--
-- Section 10 — Chat/Consult/Call (7 tables):
--   consult_requests, chat_conversations, chat_participants,
--   chat_messages, chat_message_attachments, chat_read_receipts, call_sessions
--
-- Section 11 — Reminders (2 tables):
--   reminder_rules, reminder_events
--
-- ═══════════════════════════════════════════════════════════════════════════════
-- CHANGES FROM ORIGINAL SCHEMA (summary):
-- ═══════════════════════════════════════════════════════════════════════════════
--
-- 🔄 GLOBAL:
--   • CHAR(36) for all GUIDs (EF Core compatibility)
--   • Enums stored as VARCHAR strings (not ENUM type) — EF HasConversion<string>()
--   • Consistent snake_case naming throughout
--
-- 🔄 SECTION 2 (Users):
--   • users: Removed password_alg, is_anonymized, anonymized_at, created_by, updated_by
--   • users: phone_e164 → phone
--   • user_profiles: avatar_file_id → avatar_url (VARCHAR, not FK)
--   • user_profiles: Removed timezone, created_by, updated_by
--
-- 🔄 SECTION 3 (Pregnancy):
--   • pregnancies: Added 12 columns: baby_nickname, baby_gender, pregnancy_type,
--     mother_blood_type, pre_pregnancy_weight_kg, height_cm, due_date_source,
--     gravida, para, actual_delivery_date, delivery_method, cover_image_url
--   • prenatal_tests: abnormal_flag → is_abnormal_result
--   • prenatal_visits: doctor_id FK deferred to Section 9
--
-- 🔄 SECTION 4-5 (Documents):
--   • storage_files: provider→storage_provider, bucket→bucket_name,
--     original_name→original_file_name, size_bytes→file_size_bytes
--   • REMOVED document_files table (MedicalDocument→StorageFile direct FK)
--   • REMOVED medical_field_definitions + extracted_medical_fields
--     (replaced by VitalsJson/ResultJson/StructuredJson)
--   • REMOVED tags + medical_document_tags (replaced by is_favorite boolean)
--   • REMOVED metadata_json from medical_documents
--   • ADDED is_favorite column to medical_documents
--   • ocr_results: document_file_id → document_id (FK to medical_documents)
--   • ocr_results: Added 5 AI processing columns (Week 5)
--
-- 🔄 SECTION 6 (AI):
--   • ADDED ai_prompt_templates (versioned prompt management)
--   • ADDED ai_request_logs (replaces nutrition_ai_requests — generalized)
--
-- 🔄 SECTION 8 (Nutrition):
--   • food_items_ref → ref_food_items (consistent ref_ prefix)
--   • food_item_translations → ref_food_item_translations
--   • ref_food_items: Added is_active, updated_at, deleted_at (BaseEntity pattern)
--   • ref_nutrients: Added is_active, updated_at
--   • pregnancy_food_preferences: pref_type → preference_type (clearer)
--   • meal_plans: ai_request_id → ai_request_log_id (FK to ai_request_logs)
--
-- 🔄 SECTION 9 (Doctors):
--   • Column renaming for clarity (status stored as VARCHAR strings)
--   • doctor_availability_slots: slot_minutes → duration_minutes
--
-- 🔄 SECTION 10 (Chat):
--   • chat_conversations: Added AI_NUTRITION to conversation_type
--   • chat_messages: sender_type: Added 'AI' option for AI-generated responses
--   • call_sessions: duration_sec (GENERATED) → duration_seconds (app-computed)
--     for EF Core compatibility
--   • chat_participants: role_in_conv → role_in_chat (clearer)
--
-- ═══════════════════════════════════════════════════════════════════════════════
