using PruebaTecnica.Application.Commands;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using PruebaTecnica.Application.Ports.Output;
using PruebaTecnica.Application.Results;
using PruebaTecnica.Domain.Entities;

namespace PruebaTecnica.Infrastructure.Oracle;

public sealed class OracleDatosRepository(string connectionString) : IDatosRepository
{
    public async Task<ResultadoCreacion> InsertarAsync(CrearDatoCommand datos, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PKG_DATOS.SP_INSERTAR";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.CommandTimeout = 30;
        command.Parameters.Add("p_codigo", OracleDbType.Varchar2, 32).Value = datos.Codigo;
        command.Parameters.Add("p_secuencia", OracleDbType.Int64).Value = datos.Secuencia;
        command.Parameters.Add("p_fecha_recepcion", OracleDbType.TimeStamp).Value = datos.FechaRecepcion;
        command.Parameters.Add("p_peso", OracleDbType.Decimal).Value = datos.Peso;
        command.Parameters.Add("p_descripcion", OracleDbType.Varchar2, 320).Value = (object?)datos.Descripcion ?? DBNull.Value;
        var idParameter = command.Parameters.Add("p_id", OracleDbType.Decimal);
        idParameter.Direction = ParameterDirection.Output;
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);

            var id = checked((long)((OracleDecimal)idParameter.Value).Value);

            transaction.Commit();

            return new ResultadoCreacion(id);
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            transaction.Rollback();
            return new(null);
        }
        // Otros errores revierten la transacción al liberar la conexión.
    }

    public async Task<ResultadoActualizacion> ActualizarAsync(Dato dato, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PKG_DATOS.SP_ACTUALIZAR";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.CommandTimeout = 30;
        command.Parameters.Add("p_id", OracleDbType.Decimal).Value = dato.Id;
        command.Parameters.Add("p_codigo", OracleDbType.Varchar2, 32).Value = dato.Codigo;
        command.Parameters.Add("p_secuencia", OracleDbType.Decimal).Value = dato.Secuencia;
        command.Parameters.Add("p_fecha_recepcion", OracleDbType.TimeStamp).Value = dato.FechaRecepcion;
        command.Parameters.Add("p_peso", OracleDbType.Decimal).Value = dato.Peso;
        command.Parameters.Add("p_descripcion", OracleDbType.Varchar2, 320).Value = (object?)dato.Descripcion ?? DBNull.Value;
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
            return ResultadoActualizacion.Actualizado;
        }
        catch (OracleException ex) when (ex.Number == 20001)
        {
            transaction.Rollback();
            return ResultadoActualizacion.NoEncontrado;
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            transaction.Rollback();
            return ResultadoActualizacion.Conflicto;
        }
        // Si ocurre otro error, Dispose revierte la transacción sin confirmar.
    }

    public async Task<Dato?> ObtenerAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "PKG_DATOS.SP_OBTENER";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.CommandTimeout = 30;
        command.Parameters.Add("p_id", OracleDbType.Int64).Value = id;
        command.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new Dato(reader.GetDecimal(0), reader.GetString(1), reader.GetDecimal(2),
            reader.GetDateTime(3), reader.GetDecimal(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    public async Task<PaginaDatos> ListarAsync(int pagina, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "PKG_DATOS.SP_LISTAR_PAGINADO";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.CommandTimeout = 30;
        command.Parameters.Add("p_pagina", OracleDbType.Int32).Value = pagina;
        command.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
        var total = command.Parameters.Add("p_total_registros", OracleDbType.Decimal);
        total.Direction = ParameterDirection.Output;

        var items = new List<DatoListado>(20);
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var dato = new Dato(reader.GetDecimal(0), reader.GetString(1), reader.GetDecimal(2),
                    reader.GetDateTime(3), reader.GetDecimal(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5));
                items.Add(new(dato, checked((long)reader.GetDecimal(6))));
            }
        }
        return new(items, checked((long)((OracleDecimal)total.Value).Value));
    }

    public async Task<ResultadoEliminacion> EliminarAsync(long id, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = "PKG_DATOS.SP_ELIMINAR";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.CommandTimeout = 30;

        command.Parameters.Add("p_id", OracleDbType.Int64).Value = id;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);

            transaction.Commit();

            return ResultadoEliminacion.Eliminado;
        }
        catch (OracleException ex) when (ex.Number == 20001)
        {
            transaction.Rollback();

            return ResultadoEliminacion.NoEncontrado;
        }
    }
}
