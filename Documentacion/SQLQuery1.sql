-- ============================================================
--  SafeWoman — Script de Creación de Base de Datos (V. Final)
--  Aplicación Móvil de Seguridad para la Mujer
--  Proyecto: SW-ANL-DOC-01 / SW-ANL-DOC-02
--  Elaborado por: Crisólogo Aguilar Flores
--  SGBD: SQL Server 2022 Express
--  * ACTUALIZACIÓN: Timestamps en UTC y blindaje legal anti-borrado.
-- ============================================================

-- ============================================================
-- 0. CREACIÓN Y SELECCIÓN DE BASE DE DATOS
-- ============================================================
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'SafeWoman')
BEGIN
    ALTER DATABASE SafeWoman SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SafeWoman;
END
GO

CREATE DATABASE SafeWoman
    COLLATE Modern_Spanish_CI_AI;  -- insensible a mayúsculas y acentos
GO

USE SafeWomane;
GO

-- ============================================================
-- 1. ADMINISTRADOR
-- ============================================================
CREATE TABLE ADMINISTRADOR (
    id_admin        INT             NOT NULL IDENTITY(1,1),
    nombre          VARCHAR(150)    NOT NULL,
    email           VARCHAR(255)    NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,   -- BCrypt cost >= 10
    activo          BIT             NOT NULL DEFAULT 1,
    ultimo_acceso   DATETIME2       NULL,
    fecha_registro  DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_ADMINISTRADOR PRIMARY KEY (id_admin),
    CONSTRAINT UQ_ADMINISTRADOR_email UNIQUE (email)
);
GO

-- ============================================================
-- 2. VICTIMA (Borrado Lógico mediante campo 'activa')
-- ============================================================
CREATE TABLE VICTIMA (
    id_victima      INT             NOT NULL IDENTITY(1,1),
    nombre_completo VARCHAR(200)    NOT NULL,
    dni             CHAR(8)         NOT NULL,
    telefono        VARCHAR(9)      NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,
    verificada      BIT             NOT NULL DEFAULT 0,
    activa          BIT             NOT NULL DEFAULT 1, -- Borrado lógico
    fecha_registro  DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_VICTIMA           PRIMARY KEY (id_victima),
    CONSTRAINT UQ_VICTIMA_dni       UNIQUE (dni),
    CONSTRAINT UQ_VICTIMA_telefono  UNIQUE (telefono),
    CONSTRAINT CK_VICTIMA_dni       CHECK (dni LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_VICTIMA_telefono  CHECK (telefono LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
);
GO

-- ============================================================
-- 3. OTP_VERIFICACION (Puede borrarse en cascada, es temporal)
-- ============================================================
CREATE TABLE OTP_VERIFICACION (
    id_otp              INT             NOT NULL IDENTITY(1,1),
    id_victima          INT             NOT NULL,
    codigo              CHAR(6)         NOT NULL,
    fecha_generacion    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    fecha_expiracion    DATETIME2       NOT NULL,
    usado               BIT             NOT NULL DEFAULT 0,

    CONSTRAINT PK_OTP               PRIMARY KEY (id_otp),
    CONSTRAINT FK_OTP_victima       FOREIGN KEY (id_victima)
        REFERENCES VICTIMA (id_victima) ON DELETE CASCADE,
    CONSTRAINT CK_OTP_codigo        CHECK (codigo LIKE '[0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_OTP_expiracion    CHECK (fecha_expiracion > fecha_generacion)
);
GO

-- ============================================================
-- 4. CONTACTO_EMERGENCIA
-- ============================================================
CREATE TABLE CONTACTO_EMERGENCIA (
    id_contacto     INT             NOT NULL IDENTITY(1,1),
    id_victima      INT             NOT NULL,
    nombre          VARCHAR(150)    NOT NULL,
    telefono        VARCHAR(9)      NOT NULL,

    CONSTRAINT PK_CONTACTO_EMERGENCIA   PRIMARY KEY (id_contacto),
    CONSTRAINT FK_CONTACTO_victima      FOREIGN KEY (id_victima)
        REFERENCES VICTIMA (id_victima) ON DELETE CASCADE,
    CONSTRAINT CK_CONTACTO_telefono     CHECK (telefono LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
);
GO

-- ============================================================
-- 5. ALERTA_SOS
--    NO ACTION: Blindaje forense, las alertas no se borran físicamente.
-- ============================================================
CREATE TABLE ALERTA_SOS (
    id_alerta               INT             NOT NULL IDENTITY(1,1),
    id_victima              INT             NOT NULL,
    latitud                 DECIMAL(10,7)   NOT NULL,
    longitud                DECIMAL(10,7)   NOT NULL,
    timestamp_activacion    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    timestamp_cancelacion   DATETIME2       NULL,
    estado                  VARCHAR(20)     NOT NULL DEFAULT 'activa',

    CONSTRAINT PK_ALERTA_SOS        PRIMARY KEY (id_alerta),
    CONSTRAINT FK_SOS_victima       FOREIGN KEY (id_victima)
        REFERENCES VICTIMA (id_victima) ON DELETE NO ACTION,
    CONSTRAINT CK_SOS_latitud       CHECK (latitud  BETWEEN -90  AND  90),
    CONSTRAINT CK_SOS_longitud      CHECK (longitud BETWEEN -180 AND 180),
    CONSTRAINT CK_SOS_estado        CHECK (estado IN ('activa','cancelada','atendida')),
    CONSTRAINT CK_SOS_cancelacion   CHECK (
        (estado = 'cancelada' AND timestamp_cancelacion IS NOT NULL)
     OR (estado <> 'cancelada' AND timestamp_cancelacion IS NULL)
    )
);
GO

-- ============================================================
-- 6. DENUNCIA
--    NO ACTION: Blindaje legal, la evidencia no se destruye.
-- ============================================================
CREATE TABLE DENUNCIA (
    id_denuncia         INT             NOT NULL IDENTITY(1,1),
    id_victima          INT             NOT NULL,
    tipo                VARCHAR(20)     NOT NULL,   -- 'formal' | 'anonima'
    estado              VARCHAR(20)     NOT NULL DEFAULT 'pendiente',
    fecha_envio         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    foto_dni_ruta       VARCHAR(500)    NULL,
    departamento        VARCHAR(100)    NULL,
    provincia           VARCHAR(100)    NULL,
    distrito            VARCHAR(100)    NULL,
    referencia_ubicacion VARCHAR(500)   NULL,
    lat_hecho           DECIMAL(10,7)   NULL,
    lng_hecho           DECIMAL(10,7)   NULL,
    fecha_hecho         DATE            NULL,
    hora_hecho          TIME            NULL,
    descripcion         TEXT            NULL,
    declaracion_jurada  BIT             NOT NULL DEFAULT 0,

    CONSTRAINT PK_DENUNCIA          PRIMARY KEY (id_denuncia),
    CONSTRAINT FK_DENUNCIA_victima  FOREIGN KEY (id_victima)
        REFERENCES VICTIMA (id_victima) ON DELETE NO ACTION,
    CONSTRAINT CK_DENUNCIA_tipo     CHECK (tipo IN ('formal','anonima')),
    CONSTRAINT CK_DENUNCIA_estado   CHECK (estado IN ('pendiente','en_proceso','atendida','archivada')),
    CONSTRAINT CK_DENUNCIA_lat      CHECK (lat_hecho IS NULL OR lat_hecho BETWEEN -90  AND  90),
    CONSTRAINT CK_DENUNCIA_lng      CHECK (lng_hecho IS NULL OR lng_hecho BETWEEN -180 AND 180),
    CONSTRAINT CK_DENUNCIA_formal   CHECK (
        tipo <> 'formal'
     OR (declaracion_jurada = 1 AND foto_dni_ruta IS NOT NULL)
    )
);
GO

-- ============================================================
-- 7. DENUNCIADO
-- ============================================================
CREATE TABLE DENUNCIADO (
    id_denunciado       INT             NOT NULL IDENTITY(1,1),
    id_denuncia         INT             NOT NULL,
    nombre_alias        VARCHAR(200)    NULL,
    relacion_victima    VARCHAR(50)     NULL,

    CONSTRAINT PK_DENUNCIADO            PRIMARY KEY (id_denunciado),
    CONSTRAINT FK_DENUNCIADO_denuncia   FOREIGN KEY (id_denuncia)
        REFERENCES DENUNCIA (id_denuncia) ON DELETE CASCADE,
    CONSTRAINT UQ_DENUNCIADO_denuncia   UNIQUE (id_denuncia),
    CONSTRAINT CK_DENUNCIADO_relacion   CHECK (
        relacion_victima IS NULL
     OR relacion_victima IN ('pareja','expareja','familiar','conocido','desconocido')
    )
);
GO

-- ============================================================
-- 8. EVIDENCIA
-- ============================================================
CREATE TABLE EVIDENCIA (
    id_evidencia    INT             NOT NULL IDENTITY(1,1),
    id_denuncia     INT             NOT NULL,
    nombre_archivo  VARCHAR(255)    NOT NULL,
    ruta_archivo    VARCHAR(500)    NOT NULL,
    tipo_archivo    VARCHAR(50)     NOT NULL DEFAULT 'imagen',
    tamanio_bytes   BIGINT          NULL,
    fecha_subida    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_EVIDENCIA             PRIMARY KEY (id_evidencia),
    CONSTRAINT FK_EVIDENCIA_denuncia    FOREIGN KEY (id_denuncia)
        REFERENCES DENUNCIA (id_denuncia) ON DELETE CASCADE,
    CONSTRAINT CK_EVIDENCIA_tipo        CHECK (tipo_archivo IN ('imagen','video','documento'))
);
GO

-- ============================================================
-- 9. NOTA_DENUNCIA
-- ============================================================
CREATE TABLE NOTA_DENUNCIA (
    id_nota         INT             NOT NULL IDENTITY(1,1),
    id_denuncia     INT             NOT NULL,
    id_admin        INT             NOT NULL,
    contenido       TEXT            NOT NULL,
    timestamp       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_NOTA_DENUNCIA             PRIMARY KEY (id_nota),
    CONSTRAINT FK_NOTA_DEN_denuncia         FOREIGN KEY (id_denuncia)
        REFERENCES DENUNCIA (id_denuncia) ON DELETE CASCADE,
    CONSTRAINT FK_NOTA_DEN_admin            FOREIGN KEY (id_admin)
        REFERENCES ADMINISTRADOR (id_admin) ON DELETE NO ACTION
);
GO

-- ============================================================
-- 10. HUELLA_DISPOSITIVO
-- ============================================================
CREATE TABLE HUELLA_DISPOSITIVO (
    id_huella           INT             NOT NULL IDENTITY(1,1),
    device_fingerprint  VARCHAR(255)    NOT NULL,
    bloqueada           BIT             NOT NULL DEFAULT 0,
    fecha_primer_uso    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    fecha_ultimo_uso    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_HUELLA_DISPOSITIVO    PRIMARY KEY (id_huella),
    CONSTRAINT UQ_HUELLA_fingerprint    UNIQUE (device_fingerprint)
);
GO

-- ============================================================
-- 11. DENUNCIA_ANONIMA
-- ============================================================
CREATE TABLE DENUNCIA_ANONIMA (
    id_denuncia_anonima     INT             NOT NULL IDENTITY(1,1),
    id_huella               INT             NOT NULL,
    estado                  VARCHAR(20)     NOT NULL DEFAULT 'pendiente',
    fecha_envio             DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    departamento            VARCHAR(100)    NULL,
    provincia               VARCHAR(100)    NULL,
    distrito                VARCHAR(100)    NULL,
    referencia_ubicacion    VARCHAR(500)    NULL,
    lat_hecho               DECIMAL(10,7)   NULL,
    lng_hecho               DECIMAL(10,7)   NULL,
    fecha_hecho             DATE            NULL,
    hora_hecho              TIME            NULL,
    descripcion             TEXT            NULL,

    CONSTRAINT PK_DENUNCIA_ANONIMA      PRIMARY KEY (id_denuncia_anonima),
    CONSTRAINT FK_DEN_AN_huella         FOREIGN KEY (id_huella)
        REFERENCES HUELLA_DISPOSITIVO (id_huella) ON DELETE NO ACTION,
    CONSTRAINT CK_DEN_AN_estado         CHECK (estado IN ('pendiente','en_proceso','atendida','archivada')),
    CONSTRAINT CK_DEN_AN_lat            CHECK (lat_hecho IS NULL OR lat_hecho BETWEEN -90  AND  90),
    CONSTRAINT CK_DEN_AN_lng            CHECK (lng_hecho IS NULL OR lng_hecho BETWEEN -180 AND 180)
);
GO

-- ============================================================
-- 12. DENUNCIADO_ANONIMA
-- ============================================================
CREATE TABLE DENUNCIADO_ANONIMA (
    id_denunciado_an        INT             NOT NULL IDENTITY(1,1),
    id_denuncia_anonima     INT             NOT NULL,
    nombre_alias            VARCHAR(200)    NULL,
    relacion                VARCHAR(50)     NULL,

    CONSTRAINT PK_DENUNCIADO_ANONIMA        PRIMARY KEY (id_denunciado_an),
    CONSTRAINT FK_DENUNCIA_AN_denuncia      FOREIGN KEY (id_denuncia_anonima)
        REFERENCES DENUNCIA_ANONIMA (id_denuncia_anonima) ON DELETE CASCADE,
    CONSTRAINT UQ_DENUNCIADO_AN_denuncia    UNIQUE (id_denuncia_anonima),
    CONSTRAINT CK_DENUNCIADO_AN_relacion    CHECK (
        relacion IS NULL
     OR relacion IN ('pareja','expareja','familiar','conocido','desconocido')
    )
);
GO

-- ============================================================
-- 13. EVIDENCIA_ANONIMA
-- ============================================================
CREATE TABLE EVIDENCIA_ANONIMA (
    id_evidencia_an         INT             NOT NULL IDENTITY(1,1),
    id_denuncia_anonima     INT             NOT NULL,
    nombre_archivo          VARCHAR(255)    NOT NULL,
    ruta_archivo            VARCHAR(500)    NOT NULL,
    tipo_archivo            VARCHAR(50)     NOT NULL DEFAULT 'imagen',
    tamanio_bytes           BIGINT          NULL,
    fecha_subida            DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_EVIDENCIA_ANONIMA         PRIMARY KEY (id_evidencia_an),
    CONSTRAINT FK_EVIDENCIA_AN_denuncia     FOREIGN KEY (id_denuncia_anonima)
        REFERENCES DENUNCIA_ANONIMA (id_denuncia_anonima) ON DELETE CASCADE,
    CONSTRAINT CK_EVIDENCIA_AN_tipo         CHECK (tipo_archivo IN ('imagen','video','documento'))
);
GO

-- ============================================================
-- 14. NOTA_DENUNCIA_ANONIMA
-- ============================================================
CREATE TABLE NOTA_DENUNCIA_ANONIMA (
    id_nota_an              INT             NOT NULL IDENTITY(1,1),
    id_denuncia_anonima     INT             NOT NULL,
    id_admin                INT             NOT NULL,
    contenido               TEXT            NOT NULL,
    timestamp               DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_NOTA_DEN_ANONIMA          PRIMARY KEY (id_nota_an),
    CONSTRAINT FK_NOTA_DEN_AN_denuncia      FOREIGN KEY (id_denuncia_anonima)
        REFERENCES DENUNCIA_ANONIMA (id_denuncia_anonima) ON DELETE CASCADE,
    CONSTRAINT FK_NOTA_DEN_AN_admin         FOREIGN KEY (id_admin)
        REFERENCES ADMINISTRADOR (id_admin) ON DELETE NO ACTION
);
GO

-- ============================================================
-- 15. LOG_AUDITORIA
-- ============================================================
CREATE TABLE LOG_AUDITORIA (
    id_log              INT             NOT NULL IDENTITY(1,1),
    id_admin            INT             NULL,
    accion              VARCHAR(100)    NOT NULL,
    entidad_afectada    VARCHAR(60)     NOT NULL,
    id_entidad_afectada INT             NULL,
    descripcion         VARCHAR(500)    NULL,
    timestamp           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_LOG_AUDITORIA     PRIMARY KEY (id_log),
    CONSTRAINT FK_LOG_admin         FOREIGN KEY (id_admin)
        REFERENCES ADMINISTRADOR (id_admin) ON DELETE SET NULL,
    CONSTRAINT CK_LOG_accion        CHECK (accion IN (
        'BLOQUEO_DISPOSITIVO',
        'DESBLOQUEO_DISPOSITIVO',
        'CAMBIO_ESTADO_DENUNCIA',
        'CAMBIO_ESTADO_DENUNCIA_ANONIMA',
        'NOTA_AGREGADA',
        'NOTA_ANONIMA_AGREGADA',
        'LOGIN_ADMIN',
        'LOGOUT_ADMIN'
    ))
);
GO

-- ============================================================
-- ÍNDICES DE RENDIMIENTO
-- ============================================================

CREATE INDEX IX_ALERTA_SOS_victima_estado
    ON ALERTA_SOS (id_victima, estado);
GO

CREATE INDEX IX_DENUNCIA_estado_tipo_fecha
    ON DENUNCIA (estado, tipo, fecha_envio DESC);
GO

CREATE INDEX IX_DENUNCIA_victima
    ON DENUNCIA (id_victima);
GO

CREATE INDEX IX_DENUNCIA_ANONIMA_estado_fecha
    ON DENUNCIA_ANONIMA (estado, fecha_envio DESC);
GO

CREATE INDEX IX_DENUNCIA_ANONIMA_huella
    ON DENUNCIA_ANONIMA (id_huella);
GO

CREATE INDEX IX_HUELLA_bloqueada
    ON HUELLA_DISPOSITIVO (bloqueada);
GO

CREATE INDEX IX_LOG_admin_timestamp
    ON LOG_AUDITORIA (id_admin, timestamp DESC);
GO

CREATE INDEX IX_OTP_victima_usado
    ON OTP_VERIFICACION (id_victima, usado, fecha_expiracion);
GO

-- ============================================================
-- DATOS SEMILLA — ADMINISTRADOR INICIAL
-- ============================================================
INSERT INTO ADMINISTRADOR (nombre, email, password_hash, activo)
VALUES (
    'Centro de Monitoreo SafeWoman',
    'admin@safewoman.pe',
    '$2b$10$PLACEHOLDER_HASH_BCRYPT_REEMPLAZAR_EN_PRODUCCION',
    1
);
GO

-- ============================================================
-- VERIFICACIÓN FINAL
-- ============================================================
SELECT
    t.name          AS tabla,
    p.rows          AS filas_estimadas
FROM
    sys.tables  t
JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
ORDER BY t.name;
GO

PRINT '=== SafeWoman BD (Versión Final) creada correctamente. ===';
GO