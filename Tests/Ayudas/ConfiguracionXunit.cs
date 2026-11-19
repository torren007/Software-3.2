using Xunit;

// Los tests comparten el puerto 5050 (la "cocina" falsa) y arrancan la API completa:
// se ejecutan de a uno para que no se pisen entre sí.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
