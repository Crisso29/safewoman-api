// xUnit corre tests en paralelo por defecto. Los API tests comparten un mismo
// contenedor PostgreSQL — ejecutarlos en paralelo generaría data races porque
// cada test resetea la BD en su constructor. Al desactivar el paralelismo
// dentro del ensamblado, los tests corren secuencialmente y cada uno tiene
// BD limpia sin interferencia.
//
// Es la práctica estándar para tests de integración/API con estado compartido.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
