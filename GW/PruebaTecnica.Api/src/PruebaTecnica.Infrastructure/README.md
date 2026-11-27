# Infraestructura

`OracleDatosRepository` implementa `IDatosRepository` y ejecuta `PKG_DATOS.SP_LISTAR_PAGINADO` mediante ODP.NET. Lee el cursor y el total sin cargar la tabla completa en memoria.
