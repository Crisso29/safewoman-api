using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ADMINISTRADOR",
                columns: table => new
                {
                    id_admin = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ultimo_acceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMINISTRADOR", x => x.id_admin);
                });

            migrationBuilder.CreateTable(
                name: "HUELLA_DISPOSITIVO",
                columns: table => new
                {
                    id_huella = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    device_fingerprint = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    bloqueada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    fecha_primer_uso = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    fecha_ultimo_uso = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HUELLA_DISPOSITIVO", x => x.id_huella);
                });

            migrationBuilder.CreateTable(
                name: "VICTIMA",
                columns: table => new
                {
                    id_victima = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_completo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dni = table.Column<string>(type: "CHAR(8)", nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    verificada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VICTIMA", x => x.id_victima);
                });

            migrationBuilder.CreateTable(
                name: "LOG_AUDITORIA",
                columns: table => new
                {
                    id_log = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_admin = table.Column<int>(type: "int", nullable: true),
                    accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entidad_afectada = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    id_entidad_afectada = table.Column<int>(type: "int", nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOG_AUDITORIA", x => x.id_log);
                    table.ForeignKey(
                        name: "FK_LOG_AUDITORIA_ADMINISTRADOR_id_admin",
                        column: x => x.id_admin,
                        principalTable: "ADMINISTRADOR",
                        principalColumn: "id_admin",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DENUNCIA_ANONIMA",
                columns: table => new
                {
                    id_denuncia_anonima = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_huella = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    fecha_envio = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    departamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    distrito = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    referencia_ubicacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    lat_hecho = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: true),
                    lng_hecho = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: true),
                    fecha_hecho = table.Column<DateOnly>(type: "date", nullable: true),
                    hora_hecho = table.Column<TimeOnly>(type: "time", nullable: true),
                    descripcion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DENUNCIA_ANONIMA", x => x.id_denuncia_anonima);
                    table.ForeignKey(
                        name: "FK_DENUNCIA_ANONIMA_HUELLA_DISPOSITIVO_id_huella",
                        column: x => x.id_huella,
                        principalTable: "HUELLA_DISPOSITIVO",
                        principalColumn: "id_huella");
                });

            migrationBuilder.CreateTable(
                name: "ALERTA_SOS",
                columns: table => new
                {
                    id_alerta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_victima = table.Column<int>(type: "int", nullable: false),
                    latitud = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: false),
                    longitud = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: false),
                    timestamp_activacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    timestamp_cancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "activa")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALERTA_SOS", x => x.id_alerta);
                    table.ForeignKey(
                        name: "FK_ALERTA_SOS_VICTIMA_id_victima",
                        column: x => x.id_victima,
                        principalTable: "VICTIMA",
                        principalColumn: "id_victima");
                });

            migrationBuilder.CreateTable(
                name: "CONTACTO_EMERGENCIA",
                columns: table => new
                {
                    id_contacto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_victima = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONTACTO_EMERGENCIA", x => x.id_contacto);
                    table.ForeignKey(
                        name: "FK_CONTACTO_EMERGENCIA_VICTIMA_id_victima",
                        column: x => x.id_victima,
                        principalTable: "VICTIMA",
                        principalColumn: "id_victima",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DENUNCIA",
                columns: table => new
                {
                    id_denuncia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_victima = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    fecha_envio = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    foto_dni_ruta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    departamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    distrito = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    referencia_ubicacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    lat_hecho = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: true),
                    lng_hecho = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: true),
                    fecha_hecho = table.Column<DateOnly>(type: "date", nullable: true),
                    hora_hecho = table.Column<TimeOnly>(type: "time", nullable: true),
                    descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    declaracion_jurada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DENUNCIA", x => x.id_denuncia);
                    table.ForeignKey(
                        name: "FK_DENUNCIA_VICTIMA_id_victima",
                        column: x => x.id_victima,
                        principalTable: "VICTIMA",
                        principalColumn: "id_victima");
                });

            migrationBuilder.CreateTable(
                name: "OTP_VERIFICACION",
                columns: table => new
                {
                    id_otp = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_victima = table.Column<int>(type: "int", nullable: false),
                    codigo = table.Column<string>(type: "CHAR(6)", nullable: false),
                    fecha_generacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    fecha_expiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    usado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTP_VERIFICACION", x => x.id_otp);
                    table.ForeignKey(
                        name: "FK_OTP_VERIFICACION_VICTIMA_id_victima",
                        column: x => x.id_victima,
                        principalTable: "VICTIMA",
                        principalColumn: "id_victima",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DENUNCIADO_ANONIMA",
                columns: table => new
                {
                    id_denunciado_an = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_denuncia_anonima = table.Column<int>(type: "int", nullable: false),
                    nombre_alias = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    relacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DENUNCIADO_ANONIMA", x => x.id_denunciado_an);
                    table.ForeignKey(
                        name: "FK_DENUNCIADO_ANONIMA_DENUNCIA_ANONIMA_id_denuncia_anonima",
                        column: x => x.id_denuncia_anonima,
                        principalTable: "DENUNCIA_ANONIMA",
                        principalColumn: "id_denuncia_anonima",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EVIDENCIA_ANONIMA",
                columns: table => new
                {
                    id_evidencia_an = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_denuncia_anonima = table.Column<int>(type: "int", nullable: false),
                    nombre_archivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ruta_archivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    tipo_archivo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "imagen"),
                    tamanio_bytes = table.Column<long>(type: "bigint", nullable: true),
                    fecha_subida = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVIDENCIA_ANONIMA", x => x.id_evidencia_an);
                    table.ForeignKey(
                        name: "FK_EVIDENCIA_ANONIMA_DENUNCIA_ANONIMA_id_denuncia_anonima",
                        column: x => x.id_denuncia_anonima,
                        principalTable: "DENUNCIA_ANONIMA",
                        principalColumn: "id_denuncia_anonima",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DENUNCIADO",
                columns: table => new
                {
                    id_denunciado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_denuncia = table.Column<int>(type: "int", nullable: false),
                    nombre_alias = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    relacion_victima = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DENUNCIADO", x => x.id_denunciado);
                    table.ForeignKey(
                        name: "FK_DENUNCIADO_DENUNCIA_id_denuncia",
                        column: x => x.id_denuncia,
                        principalTable: "DENUNCIA",
                        principalColumn: "id_denuncia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EVIDENCIA",
                columns: table => new
                {
                    id_evidencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_denuncia = table.Column<int>(type: "int", nullable: false),
                    nombre_archivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ruta_archivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    tipo_archivo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "imagen"),
                    tamanio_bytes = table.Column<long>(type: "bigint", nullable: true),
                    fecha_subida = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVIDENCIA", x => x.id_evidencia);
                    table.ForeignKey(
                        name: "FK_EVIDENCIA_DENUNCIA_id_denuncia",
                        column: x => x.id_denuncia,
                        principalTable: "DENUNCIA",
                        principalColumn: "id_denuncia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_ADMINISTRADOR_email",
                table: "ADMINISTRADOR",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ALERTA_SOS_victima_estado",
                table: "ALERTA_SOS",
                columns: new[] { "id_victima", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_CONTACTO_EMERGENCIA_id_victima",
                table: "CONTACTO_EMERGENCIA",
                column: "id_victima");

            migrationBuilder.CreateIndex(
                name: "IX_DENUNCIA_estado_tipo_fecha",
                table: "DENUNCIA",
                columns: new[] { "estado", "tipo", "fecha_envio" });

            migrationBuilder.CreateIndex(
                name: "IX_DENUNCIA_victima",
                table: "DENUNCIA",
                column: "id_victima");

            migrationBuilder.CreateIndex(
                name: "IX_DENUNCIA_ANONIMA_estado_fecha",
                table: "DENUNCIA_ANONIMA",
                columns: new[] { "estado", "fecha_envio" });

            migrationBuilder.CreateIndex(
                name: "IX_DENUNCIA_ANONIMA_huella",
                table: "DENUNCIA_ANONIMA",
                column: "id_huella");

            migrationBuilder.CreateIndex(
                name: "IX_DENUNCIADO_id_denuncia",
                table: "DENUNCIADO",
                column: "id_denuncia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DENUNCIADO_ANONIMA_id_denuncia_anonima",
                table: "DENUNCIADO_ANONIMA",
                column: "id_denuncia_anonima",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EVIDENCIA_id_denuncia",
                table: "EVIDENCIA",
                column: "id_denuncia");

            migrationBuilder.CreateIndex(
                name: "IX_EVIDENCIA_ANONIMA_id_denuncia_anonima",
                table: "EVIDENCIA_ANONIMA",
                column: "id_denuncia_anonima");

            migrationBuilder.CreateIndex(
                name: "IX_HUELLA_bloqueada",
                table: "HUELLA_DISPOSITIVO",
                column: "bloqueada");

            migrationBuilder.CreateIndex(
                name: "UQ_HUELLA_fingerprint",
                table: "HUELLA_DISPOSITIVO",
                column: "device_fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LOG_admin_timestamp",
                table: "LOG_AUDITORIA",
                columns: new[] { "id_admin", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OTP_victima_usado",
                table: "OTP_VERIFICACION",
                columns: new[] { "id_victima", "usado", "fecha_expiracion" });

            migrationBuilder.CreateIndex(
                name: "UQ_VICTIMA_dni",
                table: "VICTIMA",
                column: "dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_VICTIMA_telefono",
                table: "VICTIMA",
                column: "telefono",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ALERTA_SOS");

            migrationBuilder.DropTable(
                name: "CONTACTO_EMERGENCIA");

            migrationBuilder.DropTable(
                name: "DENUNCIADO");

            migrationBuilder.DropTable(
                name: "DENUNCIADO_ANONIMA");

            migrationBuilder.DropTable(
                name: "EVIDENCIA");

            migrationBuilder.DropTable(
                name: "EVIDENCIA_ANONIMA");

            migrationBuilder.DropTable(
                name: "LOG_AUDITORIA");

            migrationBuilder.DropTable(
                name: "OTP_VERIFICACION");

            migrationBuilder.DropTable(
                name: "DENUNCIA");

            migrationBuilder.DropTable(
                name: "DENUNCIA_ANONIMA");

            migrationBuilder.DropTable(
                name: "ADMINISTRADOR");

            migrationBuilder.DropTable(
                name: "VICTIMA");

            migrationBuilder.DropTable(
                name: "HUELLA_DISPOSITIVO");
        }
    }
}
