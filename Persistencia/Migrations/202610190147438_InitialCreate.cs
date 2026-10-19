namespace Persistencia.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "ASISTENCIA.CARGO",
                c => new
                    {
                        ID_CARGO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        NOMBRE_CARGO = c.String(nullable: false, maxLength: 80, unicode: false),
                        NIVEL_JERARQUICO = c.String(maxLength: 30, unicode: false),
                    })
                .PrimaryKey(t => t.ID_CARGO);
            
            CreateTable(
                "ASISTENCIA.EMPLEADO",
                c => new
                    {
                        ID_EMPLEADO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        TIPO_DOCUMENTO = c.String(nullable: false, maxLength: 2, fixedLength: true, unicode: false),
                        NUMERO_DOCUMENTO = c.String(nullable: false, maxLength: 20, unicode: false),
                        NOMBRES = c.String(nullable: false, maxLength: 60, unicode: false),
                        APELLIDOS = c.String(nullable: false, maxLength: 60, unicode: false),
                        FECHA_NACIMIENTO = c.DateTime(),
                        CORREO = c.String(maxLength: 100, unicode: false),
                        TELEFONO = c.String(maxLength: 20, unicode: false),
                        FECHA_INGRESO = c.DateTime(nullable: false),
                        ESTADO = c.String(nullable: false, maxLength: 1, fixedLength: true, unicode: false),
                        ID_DEPARTAMENTO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        ID_CARGO = c.Decimal(nullable: false, precision: 10, scale: 0),
                    })
                .PrimaryKey(t => t.ID_EMPLEADO)
                .ForeignKey("ASISTENCIA.CARGO", t => t.ID_CARGO)
                .ForeignKey("ASISTENCIA.DEPARTAMENTO", t => t.ID_DEPARTAMENTO)
                .Index(t => t.NUMERO_DOCUMENTO, unique: true, name: "UQ_EMPLEADO_NUMDOC")
                .Index(t => t.CORREO, unique: true, name: "UQ_EMPLEADO_CORREO")
                .Index(t => t.ID_DEPARTAMENTO)
                .Index(t => t.ID_CARGO);
            
            CreateTable(
                "ASISTENCIA.DEPARTAMENTO",
                c => new
                    {
                        ID_DEPARTAMENTO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        NOMBRE_DEPARTAMENTO = c.String(nullable: false, maxLength: 80, unicode: false),
                        ID_SEDE = c.Decimal(nullable: false, precision: 10, scale: 0),
                    })
                .PrimaryKey(t => t.ID_DEPARTAMENTO)
                .ForeignKey("ASISTENCIA.SEDE", t => t.ID_SEDE)
                .Index(t => t.ID_SEDE);
            
            CreateTable(
                "ASISTENCIA.SEDE",
                c => new
                    {
                        ID_SEDE = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        NOMBRE_SEDE = c.String(nullable: false, maxLength: 100, unicode: false),
                        CIUDAD = c.String(nullable: false, maxLength: 50, unicode: false),
                        DIRECCION = c.String(nullable: false, maxLength: 150, unicode: false),
                        TELEFONO = c.String(maxLength: 20, unicode: false),
                    })
                .PrimaryKey(t => t.ID_SEDE);
            
            CreateTable(
                "ASISTENCIA.DISPOSITIVO_BIOMETRICO",
                c => new
                    {
                        ID_DISPOSITIVO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        ID_SEDE = c.Decimal(nullable: false, precision: 10, scale: 0),
                        NOMBRE_DISPOSITIVO = c.String(nullable: false, maxLength: 60, unicode: false),
                        TIPO_DISPOSITIVO = c.String(nullable: false, maxLength: 20, unicode: false),
                        DIRECCION_IP = c.String(maxLength: 15, unicode: false),
                        UBICACION = c.String(maxLength: 80, unicode: false),
                        ESTADO = c.String(nullable: false, maxLength: 1, fixedLength: true, unicode: false),
                    })
                .PrimaryKey(t => t.ID_DISPOSITIVO)
                .ForeignKey("ASISTENCIA.SEDE", t => t.ID_SEDE)
                .Index(t => t.ID_SEDE);
            
            CreateTable(
                "ASISTENCIA.REGISTRO_ASISTENCIA",
                c => new
                    {
                        ID_REGISTRO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        ID_EMPLEADO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        ID_DISPOSITIVO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        FECHA = c.DateTime(nullable: false),
                        HORA = c.DateTime(nullable: false),
                        TIPO_MARCACION = c.String(nullable: false, maxLength: 10, unicode: false),
                        METODO_VERIFICACION = c.String(nullable: false, maxLength: 20, unicode: false),
                    })
                .PrimaryKey(t => t.ID_REGISTRO)
                .ForeignKey("ASISTENCIA.DISPOSITIVO_BIOMETRICO", t => t.ID_DISPOSITIVO)
                .ForeignKey("ASISTENCIA.EMPLEADO", t => t.ID_EMPLEADO)
                .Index(t => t.ID_EMPLEADO)
                .Index(t => t.ID_DISPOSITIVO);
            
            CreateTable(
                "ASISTENCIA.EMPLEADO_HORARIO",
                c => new
                    {
                        ID_EMPLEADO_HORARIO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        ID_EMPLEADO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        ID_HORARIO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        FECHA_INICIO = c.DateTime(nullable: false),
                        FECHA_FIN = c.DateTime(),
                    })
                .PrimaryKey(t => t.ID_EMPLEADO_HORARIO)
                .ForeignKey("ASISTENCIA.EMPLEADO", t => t.ID_EMPLEADO)
                .ForeignKey("ASISTENCIA.HORARIO", t => t.ID_HORARIO)
                .Index(t => t.ID_EMPLEADO)
                .Index(t => t.ID_HORARIO);
            
            CreateTable(
                "ASISTENCIA.HORARIO",
                c => new
                    {
                        ID_HORARIO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        NOMBRE_HORARIO = c.String(nullable: false, maxLength: 50, unicode: false),
                        HORA_ENTRADA = c.DateTime(nullable: false),
                        HORA_SALIDA = c.DateTime(nullable: false),
                        DIAS_APLICABLES = c.String(nullable: false, maxLength: 30, unicode: false),
                    })
                .PrimaryKey(t => t.ID_HORARIO);
            
            CreateTable(
                "ASISTENCIA.NOVEDAD",
                c => new
                    {
                        ID_NOVEDAD = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        ID_EMPLEADO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        ID_TIPO_NOVEDAD = c.Decimal(nullable: false, precision: 10, scale: 0),
                        FECHA_INICIO = c.DateTime(nullable: false),
                        FECHA_FIN = c.DateTime(),
                        MOTIVO = c.String(maxLength: 200, unicode: false),
                        ESTADO_APROBACION = c.String(nullable: false, maxLength: 15, unicode: false),
                        ID_USUARIO_APRUEBA = c.Decimal(precision: 10, scale: 0),
                    })
                .PrimaryKey(t => t.ID_NOVEDAD)
                .ForeignKey("ASISTENCIA.EMPLEADO", t => t.ID_EMPLEADO)
                .ForeignKey("ASISTENCIA.TIPO_NOVEDAD", t => t.ID_TIPO_NOVEDAD)
                .ForeignKey("ASISTENCIA.USUARIO", t => t.ID_USUARIO_APRUEBA)
                .Index(t => t.ID_EMPLEADO)
                .Index(t => t.ID_TIPO_NOVEDAD)
                .Index(t => t.ID_USUARIO_APRUEBA);
            
            CreateTable(
                "ASISTENCIA.TIPO_NOVEDAD",
                c => new
                    {
                        ID_TIPO_NOVEDAD = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        NOMBRE_TIPO = c.String(nullable: false, maxLength: 40, unicode: false),
                        DESCRIPCION = c.String(maxLength: 150, unicode: false),
                    })
                .PrimaryKey(t => t.ID_TIPO_NOVEDAD);
            
            CreateTable(
                "ASISTENCIA.USUARIO",
                c => new
                    {
                        ID_USUARIO = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        ID_EMPLEADO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        ID_ROL = c.Decimal(nullable: false, precision: 10, scale: 0),
                        NOMBRE_USUARIO = c.String(nullable: false, maxLength: 30, unicode: false),
                        CONTRASENA_HASH = c.String(nullable: false, maxLength: 255, unicode: false),
                        CORREO_ACCESO = c.String(nullable: false, maxLength: 100, unicode: false),
                        ESTADO = c.String(nullable: false, maxLength: 1, fixedLength: true, unicode: false),
                    })
                .PrimaryKey(t => t.ID_USUARIO)
                .ForeignKey("ASISTENCIA.EMPLEADO", t => t.ID_EMPLEADO)
                .ForeignKey("ASISTENCIA.ROL", t => t.ID_ROL)
                .Index(t => t.ID_EMPLEADO, unique: true, name: "UQ_USUARIO_EMPLEADO")
                .Index(t => t.ID_ROL)
                .Index(t => t.NOMBRE_USUARIO, unique: true, name: "UQ_USUARIO_NOMBRE")
                .Index(t => t.CORREO_ACCESO, unique: true, name: "UQ_USUARIO_CORREO");
            
            CreateTable(
                "ASISTENCIA.ROL",
                c => new
                    {
                        ID_ROL = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        NOMBRE_ROL = c.String(nullable: false, maxLength: 30, unicode: false),
                        DESCRIPCION = c.String(maxLength: 150, unicode: false),
                    })
                .PrimaryKey(t => t.ID_ROL);
            
            CreateTable(
                "ASISTENCIA.PLANTILLA_BIOMETRICA",
                c => new
                    {
                        ID_PLANTILLA = c.Decimal(nullable: false, precision: 10, scale: 0, identity: true),
                        ID_EMPLEADO = c.Decimal(nullable: false, precision: 10, scale: 0),
                        TIPO_BIOMETRIA = c.String(nullable: false, maxLength: 20, unicode: false),
                        PLANTILLA = c.Binary(nullable: false),
                        FECHA_REGISTRO = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID_PLANTILLA)
                .ForeignKey("ASISTENCIA.EMPLEADO", t => t.ID_EMPLEADO)
                .Index(t => t.ID_EMPLEADO, unique: true, name: "UQ_PLANTILLA_EMPLEADO");
            
        }
        
        public override void Down()
        {
            DropForeignKey("ASISTENCIA.PLANTILLA_BIOMETRICA", "ID_EMPLEADO", "ASISTENCIA.EMPLEADO");
            DropForeignKey("ASISTENCIA.NOVEDAD", "ID_USUARIO_APRUEBA", "ASISTENCIA.USUARIO");
            DropForeignKey("ASISTENCIA.USUARIO", "ID_ROL", "ASISTENCIA.ROL");
            DropForeignKey("ASISTENCIA.USUARIO", "ID_EMPLEADO", "ASISTENCIA.EMPLEADO");
            DropForeignKey("ASISTENCIA.NOVEDAD", "ID_TIPO_NOVEDAD", "ASISTENCIA.TIPO_NOVEDAD");
            DropForeignKey("ASISTENCIA.NOVEDAD", "ID_EMPLEADO", "ASISTENCIA.EMPLEADO");
            DropForeignKey("ASISTENCIA.EMPLEADO_HORARIO", "ID_HORARIO", "ASISTENCIA.HORARIO");
            DropForeignKey("ASISTENCIA.EMPLEADO_HORARIO", "ID_EMPLEADO", "ASISTENCIA.EMPLEADO");
            DropForeignKey("ASISTENCIA.EMPLEADO", "ID_DEPARTAMENTO", "ASISTENCIA.DEPARTAMENTO");
            DropForeignKey("ASISTENCIA.DEPARTAMENTO", "ID_SEDE", "ASISTENCIA.SEDE");
            DropForeignKey("ASISTENCIA.DISPOSITIVO_BIOMETRICO", "ID_SEDE", "ASISTENCIA.SEDE");
            DropForeignKey("ASISTENCIA.REGISTRO_ASISTENCIA", "ID_EMPLEADO", "ASISTENCIA.EMPLEADO");
            DropForeignKey("ASISTENCIA.REGISTRO_ASISTENCIA", "ID_DISPOSITIVO", "ASISTENCIA.DISPOSITIVO_BIOMETRICO");
            DropForeignKey("ASISTENCIA.EMPLEADO", "ID_CARGO", "ASISTENCIA.CARGO");
            DropIndex("ASISTENCIA.PLANTILLA_BIOMETRICA", "UQ_PLANTILLA_EMPLEADO");
            DropIndex("ASISTENCIA.USUARIO", "UQ_USUARIO_CORREO");
            DropIndex("ASISTENCIA.USUARIO", "UQ_USUARIO_NOMBRE");
            DropIndex("ASISTENCIA.USUARIO", new[] { "ID_ROL" });
            DropIndex("ASISTENCIA.USUARIO", "UQ_USUARIO_EMPLEADO");
            DropIndex("ASISTENCIA.NOVEDAD", new[] { "ID_USUARIO_APRUEBA" });
            DropIndex("ASISTENCIA.NOVEDAD", new[] { "ID_TIPO_NOVEDAD" });
            DropIndex("ASISTENCIA.NOVEDAD", new[] { "ID_EMPLEADO" });
            DropIndex("ASISTENCIA.EMPLEADO_HORARIO", new[] { "ID_HORARIO" });
            DropIndex("ASISTENCIA.EMPLEADO_HORARIO", new[] { "ID_EMPLEADO" });
            DropIndex("ASISTENCIA.REGISTRO_ASISTENCIA", new[] { "ID_DISPOSITIVO" });
            DropIndex("ASISTENCIA.REGISTRO_ASISTENCIA", new[] { "ID_EMPLEADO" });
            DropIndex("ASISTENCIA.DISPOSITIVO_BIOMETRICO", new[] { "ID_SEDE" });
            DropIndex("ASISTENCIA.DEPARTAMENTO", new[] { "ID_SEDE" });
            DropIndex("ASISTENCIA.EMPLEADO", new[] { "ID_CARGO" });
            DropIndex("ASISTENCIA.EMPLEADO", new[] { "ID_DEPARTAMENTO" });
            DropIndex("ASISTENCIA.EMPLEADO", "UQ_EMPLEADO_CORREO");
            DropIndex("ASISTENCIA.EMPLEADO", "UQ_EMPLEADO_NUMDOC");
            DropTable("ASISTENCIA.PLANTILLA_BIOMETRICA");
            DropTable("ASISTENCIA.ROL");
            DropTable("ASISTENCIA.USUARIO");
            DropTable("ASISTENCIA.TIPO_NOVEDAD");
            DropTable("ASISTENCIA.NOVEDAD");
            DropTable("ASISTENCIA.HORARIO");
            DropTable("ASISTENCIA.EMPLEADO_HORARIO");
            DropTable("ASISTENCIA.REGISTRO_ASISTENCIA");
            DropTable("ASISTENCIA.DISPOSITIVO_BIOMETRICO");
            DropTable("ASISTENCIA.SEDE");
            DropTable("ASISTENCIA.DEPARTAMENTO");
            DropTable("ASISTENCIA.EMPLEADO");
            DropTable("ASISTENCIA.CARGO");
        }
    }
}
